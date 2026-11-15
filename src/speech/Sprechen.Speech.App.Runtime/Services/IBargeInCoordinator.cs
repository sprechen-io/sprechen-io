namespace Sprechen.Speech.App.Runtime.Services;

/// <summary>
/// Stellt das Unterbrechungssignal für laufende Sprachausgaben bereit, damit ein Benutzereingriff deren Abbruch auslösen kann.
/// </summary>
public interface IBargeInCoordinator
{
    CancellationToken Token { get; }

    void BargeIn();
}




