using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet Auflistung, Hilfe und Wiederholung mit fester Priorität zu. Eine modellierte
/// Listenanforderung hat dadurch Vorrang vor dem bloßen Wiederholen der letzten Ansage.
/// </summary>
internal sealed class FormsCatalogNavigationIntentClassifier
{
    private readonly IFormsCatalogIntentMatcher _matcher;

    public FormsCatalogNavigationIntentClassifier(IFormsCatalogIntentMatcher matcher)
    {
        _matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
    }

    public FormsCatalogIntentResult? TryClassify(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        // Eine modellierte Listenanforderung hat Vorrang vor Wiederholung, damit eine Bitte um
        // den Bestand nicht nur die letzte Rückfrage erneut ausgibt. Ohne Listen- oder
        // Hilfetreffer bleibt die Wiederholungsprüfung erreichbar.
        if (_matcher.IsList(catalog, normalized, isoCode, politenessLevel))
        {
            return new FormsCatalogIntentResult(FormsCatalogIntent.List);
        }

        if (_matcher.IsHelp(catalog, normalized, isoCode, politenessLevel))
        {
            return new FormsCatalogIntentResult(FormsCatalogIntent.Help);
        }

        if (_matcher.IsRepeat(catalog, normalized, isoCode, politenessLevel))
        {
            return new FormsCatalogIntentResult(FormsCatalogIntent.Repeat);
        }

        return null;
    }
}
