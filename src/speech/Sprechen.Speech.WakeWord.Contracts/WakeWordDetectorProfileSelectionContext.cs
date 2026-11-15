namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Bündelt Sprachcode und Aktivierungsinventar für die Detektorprofilauswahl.
/// Die Auswahlregel benötigt dadurch keine Browser- oder Umgebungsmerkmale.
/// </summary>
public sealed record WakeWordDetectorProfileSelectionContext(
    string NormalizedIsoCode,
    WakeWordInventory WakeWordInventory);
