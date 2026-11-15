namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Kapselt Mikrofonfreigabe und Aufnahmelebenszyklus für Aufrufer, die aufgezeichnetes Sprachaudio benötigen.
/// </summary>
public interface ISpeechCaptureClient
{
    Task RequestMicrophoneAccessAsync(CancellationToken cancellationToken);

    Task EnsureCaptureSessionReadyAsync(CancellationToken cancellationToken);

    Task PauseAsync(CancellationToken cancellationToken) => StopAsync(cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);

    Task<CapturedSpeechAudio> RecordAsync(CancellationToken cancellationToken);
}
