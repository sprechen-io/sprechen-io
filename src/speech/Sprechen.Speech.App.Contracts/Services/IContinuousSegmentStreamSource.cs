namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Trennt das Öffnen eines Hörmodus vom Abruf einzelner Segmente einer fortlaufenden Aufnahme.
/// </summary>
public interface IContinuousSegmentStreamSource
{
    ValueTask BeginListeningAsync(SpeechCaptureMode mode, CancellationToken cancellationToken);

    Task<CapturedSpeechAudio> CaptureNextSegmentAsync(CancellationToken cancellationToken);

    ValueTask StopListeningAsync(CancellationToken cancellationToken);
}
