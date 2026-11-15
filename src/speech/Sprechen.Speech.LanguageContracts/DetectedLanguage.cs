namespace Sprechen.Speech.LanguageContracts;

/// <summary>
/// Überträgt Sprachergebnis und Kandidaten zur weiteren Sprachwahl. Die Auswahlentscheidung liegt beim Verbraucher.
/// </summary>
public sealed record DetectedLanguage(
    string IsoCode,
    string DisplayName,
    IReadOnlyList<DetectedLanguageCandidate> Candidates);
