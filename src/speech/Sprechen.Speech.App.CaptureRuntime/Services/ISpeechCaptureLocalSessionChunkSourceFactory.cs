namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Öffnet eine Audioquelle auf der lokalen Aufnahmesitzung,
/// damit Verbraucher deren gemeinsame Nutzung nicht selbst koordinieren müssen.
/// </summary>
public interface ISpeechCaptureLocalSessionChunkSourceFactory
{
    Task<ISpeechAudioChunkSource> CreateAsync(CancellationToken cancellationToken);
}
