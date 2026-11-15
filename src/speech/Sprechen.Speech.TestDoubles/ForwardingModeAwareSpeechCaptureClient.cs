namespace Sprechen.Speech.TestDoubles;

/// <summary>
/// Verwendet vorbereitete Aufnahmen eines einfachen Captureclients für Tests
/// mit dem modusabhängigen Vertrag. Modus, Anfangsstillefrist und Cursor
/// werden dabei nicht ausgewertet.
/// </summary>
public sealed class ForwardingModeAwareSpeechCaptureClient(ISpeechCaptureClient inner) : IModeAwareSpeechCaptureClient
{
    private readonly ISpeechCaptureClient _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    public Task<CapturedSpeechAudio> RecordAsync(SpeechCaptureMode captureMode, CancellationToken cancellationToken)
        => _inner.RecordAsync(cancellationToken);

    public Task<CapturedSpeechAudio> RecordAsync(
        SpeechCaptureMode captureMode,
        TimeSpan initialSilenceTimeout,
        CancellationToken cancellationToken)
        => _inner.RecordAsync(cancellationToken);

    public Task<CapturedSpeechAudio> RecordAsync(
        SpeechCaptureMode captureMode,
        TimeSpan initialSilenceTimeout,
        SampleCursor startCursor,
        CancellationToken cancellationToken)
        => _inner.RecordAsync(cancellationToken);

    public SampleCursor? SnapshotLiveCaptureCursor() => null;
}
