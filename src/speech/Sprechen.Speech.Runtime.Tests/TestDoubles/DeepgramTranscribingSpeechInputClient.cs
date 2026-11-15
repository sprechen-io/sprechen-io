using System.Collections.Concurrent;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class DeepgramTranscribingSpeechInputClient : ISpeechInputClient, IModeAwareSpeechCaptureClient
{
    private readonly ConcurrentQueue<CapturedSpeechAudio> _recordings = new();
    private readonly Func<CapturedSpeechAudio, string?, CancellationToken, Task<SpeechTranscript>> _transcribeAsync;
    private readonly ConcurrentQueue<SpeechTranscript> _transcripts = new();
    private int _recordCallCount;
    private int _speechToTextCallCount;

    public DeepgramTranscribingSpeechInputClient(
        Func<CapturedSpeechAudio, string?, CancellationToken, Task<SpeechTranscript>> transcribeAsync)
    {
        _transcribeAsync = transcribeAsync ?? throw new ArgumentNullException(nameof(transcribeAsync));
    }

    public int RecordCallCount => Volatile.Read(ref _recordCallCount);

    public int SpeechToTextCallCount => Volatile.Read(ref _speechToTextCallCount);

    public IReadOnlyCollection<SpeechTranscript> Transcripts => _transcripts.ToArray();

    public Task RequestMicrophoneAccessAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task EnsureCaptureSessionReadyAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public ValueTask EnqueueRecordingAsync(CapturedSpeechAudio utterance)
    {
        ArgumentNullException.ThrowIfNull(utterance);

        _recordings.Enqueue(utterance);
        return ValueTask.CompletedTask;
    }

    public Task<CapturedSpeechAudio> RecordAsync(CancellationToken cancellationToken)
        => RecordCoreAsync(cancellationToken);

    public Task<CapturedSpeechAudio> RecordAsync(SpeechCaptureMode captureMode, CancellationToken cancellationToken)
        => RecordCoreAsync(cancellationToken);

    public Task<CapturedSpeechAudio> RecordAsync(
        SpeechCaptureMode captureMode,
        TimeSpan initialSilenceTimeout,
        CancellationToken cancellationToken)
        => RecordCoreAsync(cancellationToken);

    /// <summary>
    /// Liefert keinen Cursor, weil dieses Testdouble keinen Aufnahmering besitzt.
    /// </summary>
    public SampleCursor? SnapshotLiveCaptureCursor() => null;

    /// <summary>
    /// Liest die nächste vorbereitete Aufnahme. Der Startcursor bleibt unberücksichtigt,
    /// weil dieses Testdouble keinen Aufnahmering besitzt.
    /// </summary>
    public Task<CapturedSpeechAudio> RecordAsync(
        SpeechCaptureMode captureMode,
        TimeSpan initialSilenceTimeout,
        SampleCursor startCursor,
        CancellationToken cancellationToken)
        => RecordCoreAsync(cancellationToken);

    public async Task<SpeechTranscript> SpeechToTextAsync(
        CapturedSpeechAudio utterance,
        string? languageCode,
        RecognitionProfile recognitionProfile,
        CancellationToken cancellationToken,
        SpeechKeyterms? keyterms = null)
    {
        ArgumentNullException.ThrowIfNull(utterance);

        Interlocked.Increment(ref _speechToTextCallCount);
        var transcript = await _transcribeAsync(utterance, languageCode, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(transcript.Text))
        {
            throw new InvalidOperationException("Deepgram live field-entry transcription returned no speech.");
        }

        _transcripts.Enqueue(transcript);
        return transcript;
    }

    private Task<CapturedSpeechAudio> RecordCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Interlocked.Increment(ref _recordCallCount);
        if (!_recordings.TryDequeue(out var recording))
        {
            throw new InvalidOperationException("Deepgram live field-entry recording queue was exhausted.");
        }

        return Task.FromResult(recording);
    }
}
