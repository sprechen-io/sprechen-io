namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Erkannte Handlungsabsicht samt optionalen Argumenten und Hinweis auf konkurrierende Deutungen.
/// </summary>
public sealed record FormsCatalogIntentResult(
    FormsCatalogIntent Intent,
    string? TargetName = null,
    string? Query = null,
    int? Number = null,
    string? LanguageArgument = null,
    bool HasCompetingIntentEvidence = false);

