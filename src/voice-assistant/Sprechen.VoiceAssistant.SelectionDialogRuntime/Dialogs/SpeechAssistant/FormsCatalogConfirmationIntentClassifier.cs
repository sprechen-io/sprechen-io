using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet Bestätigungsantworten mit fester Priorität Nein, Ja und Abbrechen zu. Der Katalogparser
/// kann diese Antworten dadurch vor den übrigen Befehlen behandeln.
/// </summary>
internal sealed class FormsCatalogConfirmationIntentClassifier
{
    private readonly IFormsCatalogIntentMatcher _matcher;

    public FormsCatalogConfirmationIntentClassifier(IFormsCatalogIntentMatcher matcher)
    {
        _matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
    }

    public FormsCatalogIntentResult? TryClassify(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        if (_matcher.IsConfirmNo(catalog, normalized, isoCode, politenessLevel))
        {
            return new FormsCatalogIntentResult(FormsCatalogIntent.ConfirmNo);
        }

        if (_matcher.IsConfirmYes(catalog, normalized, isoCode, politenessLevel))
        {
            return new FormsCatalogIntentResult(FormsCatalogIntent.ConfirmYes);
        }

        if (_matcher.IsCancel(catalog, normalized, isoCode, politenessLevel))
        {
            return new FormsCatalogIntentResult(FormsCatalogIntent.Cancel);
        }

        return null;
    }
}
