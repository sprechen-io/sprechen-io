namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Kapselt Mikrofonzugriff und Aufnahmestart, damit Aufnahmeabläufe von der Geräteanbindung unabhängig bleiben.
/// </summary>
public interface IMicrophoneRecorder
{
    Task<IMicrophoneRecording> StartAsync(CancellationToken cancellationToken);

    Task RequestAccessAsync(CancellationToken cancellationToken);

    Task ReleaseAsync(CancellationToken cancellationToken);
}

