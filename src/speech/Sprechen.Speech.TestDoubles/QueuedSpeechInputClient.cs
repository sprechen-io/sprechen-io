using System.Threading.Channels;
using System.Collections.Concurrent;
using AppRecognitionProfile = Sprechen.Speech.App.Contracts.Services.RecognitionProfile;
using AppSpeechTranscript = Sprechen.Speech.App.Contracts.Services.SpeechTranscript;

namespace Sprechen.Speech.TestDoubles;

/// <summary>
/// Liefert vorbereitete Aufnahmen und Transkripte für Dialogtests.
/// <c>RecordCallCount</c> zählt erst erfolgreich ausgelesene Aufnahmen.
/// Eine angegebene Anfangsstillefrist begrenzt das Warten auf die Warteschlange;
/// eine konfigurierte Aufnahmeverzögerung liegt davor.
/// </summary>
public sealed class QueuedSpeechInputClient : ISpeechInputClient, IModeAwareSpeechCaptureClient
{
    private readonly TimeSpan _recordDelay;
    private readonly Channel<CapturedSpeechAudio> _recordings = Channel.CreateUnbounded<CapturedSpeechAudio>(
        new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

    private readonly object _gate = new();
    private readonly Queue<Func<AppSpeechTranscript>> _transcripts = new();
    private readonly List<SpeechCaptureMode> _recordModes = [];
    private readonly List<TimeSpan?> _answerWindows = [];
    private readonly ConcurrentQueue<(string? LanguageCode, string Text)> _speechToTextCalls = new();
    private readonly List<SpeechKeyterms?> _observedKeyterms = [];

    /// <summary>
    /// Die übergebenen Schlüsselbegriffe in der Reihenfolge der Aufzeichnung.
    /// Ein null-Eintrag bedeutet, dass dieser Aufruf keine Schlüsselbegriffe übergeben hat.
    /// </summary>
    public IReadOnlyList<SpeechKeyterms?> ObservedKeyterms
    {
        get { lock (_gate) { return _observedKeyterms.ToArray(); } }
    }
    private int _recordCallCount;
    private int _speechToTextCallCount;
    private int _pauseOnNextCancellation;

    public QueuedSpeechInputClient()
        : this(TimeSpan.Zero)
    {
    }

    public QueuedSpeechInputClient(TimeSpan recordDelay)
    {
        if (recordDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(recordDelay), recordDelay, "Delay must be non-negative.");
        }

        _recordDelay = recordDelay;
    }

    public IReadOnlyCollection<(string? LanguageCode, string Text)> SpeechToTextCalls => _speechToTextCalls.ToArray();

    public int RecordCallCount => Volatile.Read(ref _recordCallCount);

    public int SpeechToTextCallCount => Volatile.Read(ref _speechToTextCallCount);

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

    /// <summary>
    /// Die angegebenen Anfangsstillefristen. Bei null setzt dieses Testdouble
    /// keine eigene Frist für das Warten auf die nächste Aufnahme.
    /// </summary>
    public IReadOnlyList<TimeSpan?> AnswerWindows
    {
        get
        {
            lock (_gate)
            {
                return _answerWindows.ToArray();
            }
        }
    }

    public Task RequestMicrophoneAccessAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task EnsureCaptureSessionReadyAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public ValueTask EnqueueRecordingAsync(CapturedSpeechAudio utterance)
        => _recordings.Writer.WriteAsync(utterance, CancellationToken.None);

    public void EnqueueTranscript(string text, int? confidencePercent = null)
    {
        lock (_gate)
        {
            _transcripts.Enqueue(() => new AppSpeechTranscript(text, confidencePercent));
        }
    }

    public void EnqueueSpeechToTextException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        lock (_gate)
        {
            _transcripts.Enqueue(() => throw exception);
        }
    }

    public void EnqueueNoSpeechDetected()
        => EnqueueSpeechToTextException(new NoSpeechDetectedException());

    public void ArmVoiceInputPauseOnNextCancellation()
        => Interlocked.Exchange(ref _pauseOnNextCancellation, 1);

    public Task<CapturedSpeechAudio> RecordAsync(CancellationToken cancellationToken)
        => RecordCoreAsync(SpeechCaptureMode.Listening, initialSilenceTimeout: null, cancellationToken);

    public Task<CapturedSpeechAudio> RecordAsync(SpeechCaptureMode captureMode, CancellationToken cancellationToken)
        => RecordCoreAsync(captureMode, initialSilenceTimeout: null, cancellationToken);

    public Task<CapturedSpeechAudio> RecordAsync(
        SpeechCaptureMode captureMode,
        TimeSpan initialSilenceTimeout,
        CancellationToken cancellationToken)
        => RecordCoreAsync(captureMode, initialSilenceTimeout, cancellationToken);

    /// <summary>
    /// Liefert keinen Cursor, weil dieses Testdouble keinen Aufnahmering besitzt.
    /// </summary>
    public SampleCursor? SnapshotLiveCaptureCursor() => null;

    /// <summary>
    /// Liest die nächste vorbereitete Aufnahme und ignoriert den Startcursor,
    /// weil dieses Testdouble keinen Aufnahmering besitzt.
    /// </summary>
    public Task<CapturedSpeechAudio> RecordAsync(
        SpeechCaptureMode captureMode,
        TimeSpan initialSilenceTimeout,
        SampleCursor startCursor,
        CancellationToken cancellationToken)
        => RecordCoreAsync(captureMode, initialSilenceTimeout, cancellationToken);

    private async Task<CapturedSpeechAudio> RecordCoreAsync(
        SpeechCaptureMode captureMode,
        TimeSpan? initialSilenceTimeout,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _recordModes.Add(captureMode);
            _answerWindows.Add(initialSilenceTimeout);
        }

        try
        {
            if (_recordDelay > TimeSpan.Zero)
            {
                await Task.Delay(_recordDelay, cancellationToken).ConfigureAwait(false);
            }

            var utterance = initialSilenceTimeout is { } window
                ? await ReadWithinWindowAsync(window, cancellationToken).ConfigureAwait(false)
                : await _recordings.Reader.ReadAsync(cancellationToken).AsTask().ConfigureAwait(false);
            Interlocked.Increment(ref _recordCallCount);
            return utterance;
        }
        catch (OperationCanceledException) when (Interlocked.Exchange(ref _pauseOnNextCancellation, 0) == 1)
        {
            throw new VoiceInputPausedException();
        }
    }

    /// <summary>
    /// Begrenzt das Warten auf eine vorbereitete Aufnahme. Bleibt sie aus,
    /// simuliert <see cref="NoSpeechDetectedException"/> fehlende Sprache, damit
    /// ein zu knapp vorbereiteter Test nicht unbegrenzt auf weitere Aufnahmen wartet.
    /// </summary>
    private async Task<CapturedSpeechAudio> ReadWithinWindowAsync(
        TimeSpan window,
        CancellationToken cancellationToken)
    {
        using var windowExpired = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        windowExpired.CancelAfter(window);

        try
        {
            return await _recordings.Reader.ReadAsync(windowExpired.Token).AsTask().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Der Fristablauf soll fehlende Sprache melden und nicht die vorgemerkte Eingabepause auslösen.
            throw new NoSpeechDetectedException();
        }
    }

    public Task<AppSpeechTranscript> SpeechToTextAsync(CapturedSpeechAudio utterance, string? languageCode, AppRecognitionProfile recognitionProfile, CancellationToken cancellationToken, SpeechKeyterms? keyterms = null)
    {
        ArgumentNullException.ThrowIfNull(utterance);

        Interlocked.Increment(ref _speechToTextCallCount);
        lock (_gate)
        {
            _observedKeyterms.Add(keyterms);

            if (_transcripts.Count == 0)
            {
                throw new InvalidOperationException("No transcript is queued.");
            }

            var transcript = _transcripts.Dequeue().Invoke();
            _speechToTextCalls.Enqueue((languageCode, transcript.Text));
            return Task.FromResult(transcript);
        }
    }

    public async Task WaitForRecordCallCountAsync(int expectedCount, TimeSpan timeout)
    {
        if (expectedCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedCount), expectedCount, "Expected count must be positive.");
        }

        using var cts = new CancellationTokenSource(timeout);

        while (!cts.IsCancellationRequested)
        {
            if (RecordCallCount >= expectedCount)
            {
                return;
            }

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10), cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                break;
            }
        }

        throw new TimeoutException($"Expected at least {expectedCount} RecordAsync call(s), but only observed {RecordCallCount}.");
    }
}
