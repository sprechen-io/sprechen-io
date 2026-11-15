namespace Sprechen.Speech.TestDoubles;

/// <summary>
/// Verbindet vorbereitete Aufnahmen eines Capture-Testdoubles mit dem Vertrag
/// für fortlaufende Segmente, damit Dialogtests denselben Aufnahmevorrat verwenden können.
/// </summary>
public sealed class CaptureClientSegmentStreamSource(IModeAwareSpeechCaptureClient captureClient)
    : IContinuousSegmentStreamSource
{
    private readonly IModeAwareSpeechCaptureClient _captureClient = captureClient
        ?? throw new ArgumentNullException(nameof(captureClient));

    public ValueTask BeginListeningAsync(SpeechCaptureMode mode, CancellationToken cancellationToken)
    {
        if (mode != SpeechCaptureMode.ActivationListening)
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Wake segment capture requires ActivationListening.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    public Task<CapturedSpeechAudio> CaptureNextSegmentAsync(CancellationToken cancellationToken)
        => _captureClient.RecordAsync(SpeechCaptureMode.ActivationListening, cancellationToken);

    public ValueTask StopListeningAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }
}
