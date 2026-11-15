using System.Threading.Channels;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class ScriptedSpeechInputClient : ISpeechInputClient, IModeAwareSpeechCaptureClient
{
    private readonly Channel<Func<CancellationToken, Task<CapturedSpeechAudio>>> _recordSteps =
        Channel.CreateUnbounded<Func<CancellationToken, Task<CapturedSpeechAudio>>>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });

    private readonly object _gate = new();
    private readonly Queue<Func<SpeechTranscript>> _transcripts = new();
    private readonly Queue<(string? LanguageCode, RecognitionProfile Profile, SpeechKeyterms? Keyterms)> _speechToTextCalls = new();

    private int _recordCallCount;
    private int _speechToTextCallCount;

    public int RecordCallCount => Volatile.Read(ref _recordCallCount);

    public int SpeechToTextCallCount => Volatile.Read(ref _speechToTextCallCount);

    /// <summary>
    /// Zeichnet Sprache, Erkennungsprofil und Schlüsselbegriffe für die Prüfung
    /// auf; sie verändern die vorbereiteten Transkripte nicht.
    /// </summary>
    public IReadOnlyCollection<(string? LanguageCode, RecognitionProfile Profile, SpeechKeyterms? Keyterms)> SpeechToTextCalls
    {
        get
        {
            lock (_gate)
            {
                return _speechToTextCalls.ToArray();
            }
        }
    }

    public IReadOnlyList<SpeechCaptureMode> RecordModes
    {
        get
        {
            lock (_gate)
            {
                return _recordModes.ToArray();
            }
        }
    }

    public IReadOnlyList<TimeSpan?> InitialSilenceTimeouts
    {
        get
        {
            lock (_gate)
            {
                return _initialSilenceTimeouts.ToArray();
            }
        }
    }

    private readonly List<SpeechCaptureMode> _recordModes = [];
    private readonly List<TimeSpan?> _initialSilenceTimeouts = [];
    private readonly List<SampleCursor?> _startCursors = [];

    public SampleCursor? LiveCaptureCursor { get; set; } = new SampleCursor(11);

    public IReadOnlyList<SampleCursor?> StartCursors
    {
        get
        {
            lock (_gate)
            {
                return _startCursors.ToArray();
            }
        }
    }

    public SampleCursor? SnapshotLiveCaptureCursor() => LiveCaptureCursor;

    public Task RequestMicrophoneAccessAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task EnsureCaptureSessionReadyAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public ValueTask EnqueueRecordStepAsync(Func<CancellationToken, Task<CapturedSpeechAudio>> step)
    {
        ArgumentNullException.ThrowIfNull(step);
        return _recordSteps.Writer.WriteAsync(step, CancellationToken.None);
    }

    public void EnqueueTranscript(string text, int? confidencePercent = null)
    {
        EnqueueTranscript(new SpeechTranscript(text, confidencePercent));
    }

    public void EnqueueTranscript(SpeechTranscript transcript)
    {
        ArgumentNullException.ThrowIfNull(transcript);

        lock (_gate)
        {
            _transcripts.Enqueue(() => transcript);
        }
    }

    /// <summary>
    /// Bereitet einen Erkennungsaufruf vor, der mit der angegebenen Ausnahme endet.
    /// </summary>
    public void EnqueueSpeechToTextFault(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        lock (_gate)
        {
            _transcripts.Enqueue(() => throw exception);
        }
    }

    public Task<CapturedSpeechAudio> RecordAsync(CancellationToken cancellationToken)
        => RecordAsync(SpeechCaptureMode.Listening, cancellationToken);

    public async Task<CapturedSpeechAudio> RecordAsync(SpeechCaptureMode captureMode, CancellationToken cancellationToken)
        => await RecordAsyncCoreAsync(captureMode, initialSilenceTimeout: null, cancellationToken).ConfigureAwait(false);

    public async Task<CapturedSpeechAudio> RecordAsync(
        SpeechCaptureMode captureMode,
        TimeSpan initialSilenceTimeout,
        CancellationToken cancellationToken)
        => await RecordAsyncCoreAsync(captureMode, initialSilenceTimeout, cancellationToken).ConfigureAwait(false);

    public async Task<CapturedSpeechAudio> RecordAsync(
        SpeechCaptureMode captureMode,
        TimeSpan initialSilenceTimeout,
        SampleCursor startCursor,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _startCursors.Add(startCursor);
        }

        return await RecordAsyncCoreAsync(captureMode, initialSilenceTimeout, cancellationToken).ConfigureAwait(false);
    }

    private async Task<CapturedSpeechAudio> RecordAsyncCoreAsync(
        SpeechCaptureMode captureMode,
        TimeSpan? initialSilenceTimeout,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _recordCallCount);
        lock (_gate)
        {
            _recordModes.Add(captureMode);
            _initialSilenceTimeouts.Add(initialSilenceTimeout);
        }

        try
        {
            // Ohne vorbereiteten Aufnahmeschritt innerhalb der Frist wird fehlende Sprache gemeldet.
            var readTask = _recordSteps.Reader.ReadAsync(cancellationToken).AsTask();
            var step = initialSilenceTimeout.HasValue
                ? await readTask.WaitAsync(initialSilenceTimeout.Value, cancellationToken).ConfigureAwait(false)
                : await readTask.ConfigureAwait(false);
            return await step(cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            throw new NoSpeechDetectedException();
        }
    }


    private Task? _hold;

    /// <summary>
    /// Hält die Transkription an, bis <paramref name="release"/> abgeschlossen ist.
    /// </summary>
    public void HoldCallsUntil(Task release)
    {
        ArgumentNullException.ThrowIfNull(release);
        Volatile.Write(ref _hold, release);
    }

    public async Task<SpeechTranscript> SpeechToTextAsync(CapturedSpeechAudio utterance, string? languageCode, RecognitionProfile recognitionProfile, CancellationToken cancellationToken, SpeechKeyterms? keyterms = null)
    {
        ArgumentNullException.ThrowIfNull(utterance);

        // Auch vorbereitete Ergebnisse sollen erst nach einer asynchronen Fortsetzung zurückkommen.
        await Task.Yield();

        // Der Test kann die Fortsetzung gezielt freigeben, statt sich auf Thread-Pool-Timing zu verlassen.
        var hold = Volatile.Read(ref _hold);
        if (hold is not null)
        {
            await hold.ConfigureAwait(false);
        }

        Interlocked.Increment(ref _speechToTextCallCount);
        lock (_gate)
        {
            _speechToTextCalls.Enqueue((languageCode, recognitionProfile, keyterms));
            if (_transcripts.Count == 0)
            {
                throw new InvalidOperationException("No transcript is queued.");
            }

            return _transcripts.Dequeue().Invoke();
        }
    }
}



