namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Stellt den aktuellen Zustand der Aufnahmeoberfläche samt Änderungsereignis zur Beobachtung bereit.
/// </summary>
public interface ISpeechCaptureSurfaceState
{
    SpeechCaptureSurfaceStateSnapshot Current { get; }

    event Action? Changed;
}
