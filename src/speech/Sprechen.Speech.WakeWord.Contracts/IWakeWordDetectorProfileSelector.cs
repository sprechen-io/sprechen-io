using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Wählt ein akustisches Detektorprofil aus dem Katalog. Die Auswahl bleibt
/// austauschbar, ohne den Aufrufer an eine bestimmte Auswahlregel zu binden.
/// </summary>
public interface IWakeWordDetectorProfileSelector
{
    /// <summary>
    /// Wählt ein Profil aus der angebotenen, nicht leeren Liste.
    /// </summary>
    /// <param name="profiles">Profile aus dem Katalog. Null oder eine leere Liste werden abgewiesen.</param>
    /// <param name="context">Auswahlkontext mit normalisiertem Sprachcode und Aktivierungsbestand. Darf nicht null sein.</param>
    /// <returns>Ein ausgewähltes, nicht nullwertiges Profil.</returns>
    WakeWordDetectorProfile Select(
        IReadOnlyList<WakeWordDetectorProfile> profiles,
        WakeWordDetectorProfileSelectionContext context);
}
