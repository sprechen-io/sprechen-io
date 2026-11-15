namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Stellt den Zugriff auf die Ausgabelautstärke bereit, damit die Audiowiedergabe eine gemeinsame Einstellung nutzt.
/// </summary>
public interface IAudioOutputSettings
{
    ValueTask<float> GetVolumeAsync(CancellationToken cancellationToken);

    ValueTask SetVolumeAsync(float volume, CancellationToken cancellationToken);
}
