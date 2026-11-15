namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Bündelt das Sprachergebnis und ein optionales Transkript zur Weiterverarbeitung der Sprachbestätigung.
/// </summary>
public sealed record LanguageConfirmationStreamingResult(
    DetectedLanguage Language,
    string? Transcript);


