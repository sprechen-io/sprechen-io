using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Erkennt eine modellierte oder numerische Auswahlnummer und reicht übrigen Text als unbekanntes
/// Ziel weiter. Der nachgelagerte Kandidatenvergleich kann damit Namen prüfen, ohne sie hier
/// vorwegzunehmen.
/// </summary>
internal sealed class FormsCatalogSelectionIntentClassifier
{
    private readonly IFormsCatalogIntentMatcher _matcher;

    public FormsCatalogSelectionIntentClassifier(IFormsCatalogIntentMatcher matcher)
    {
        _matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
    }

    public FormsCatalogIntentResult Classify(
        FormsCatalog catalog,
        string normalized,
        string transcript,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        var number = _matcher.TryExtractNumber(catalog, normalized, isoCode, politenessLevel);
        return number.HasValue
            ? new FormsCatalogIntentResult(FormsCatalogIntent.SelectByNumber, Number: number.Value)
            : new FormsCatalogIntentResult(FormsCatalogIntent.Unknown, TargetName: transcript.Trim());
    }
}
