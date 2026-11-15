using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Beschreibt die sprachabhängige Erkennung modellierter Katalogphrasen, Präfixwerte und Zahlen.
/// Die Priorität zwischen den Intents bleibt damit Aufgabe des Parsers.
/// </summary>
internal interface IFormsCatalogIntentMatcher
{
    bool IsConfirmNo(FormsCatalog catalog, string normalized, string isoCode, PolitenessLevel politenessLevel);

    bool IsConfirmYes(FormsCatalog catalog, string normalized, string isoCode, PolitenessLevel politenessLevel);

    bool IsCancel(FormsCatalog catalog, string normalized, string isoCode, PolitenessLevel politenessLevel);

    bool IsChangeLanguage(FormsCatalog catalog, string normalized, string isoCode, PolitenessLevel politenessLevel);

    string? TryExtractChangeLanguageTarget(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel);

    bool IsRepeat(FormsCatalog catalog, string normalized, string isoCode, PolitenessLevel politenessLevel);

    bool IsHelp(FormsCatalog catalog, string normalized, string isoCode, PolitenessLevel politenessLevel);

    bool IsList(FormsCatalog catalog, string normalized, string isoCode, PolitenessLevel politenessLevel);

    string? TryExtractSearchQuery(FormsCatalog catalog, string normalized, string isoCode, PolitenessLevel politenessLevel);

    string? TryExtractDescribeTarget(FormsCatalog catalog, string normalized, string isoCode, PolitenessLevel politenessLevel);

    string? TryExtractOpenCandidateTarget(FormsCatalog catalog, string normalized, string isoCode, PolitenessLevel politenessLevel);

    int? TryExtractNumber(FormsCatalog catalog, string normalized, string isoCode, PolitenessLevel politenessLevel);
}
