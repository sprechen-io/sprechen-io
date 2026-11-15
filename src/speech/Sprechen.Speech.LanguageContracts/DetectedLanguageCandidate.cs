namespace Sprechen.Speech.LanguageContracts;

/// <summary>
/// Stellt Sprachcode, Wahrscheinlichkeit und Anzeigename eines Kandidaten für die Auswertung eines Sprachergebnisses bereit.
/// </summary>
public sealed record DetectedLanguageCandidate(
    string IsoCode,
    float Probability,
    string DisplayName);
