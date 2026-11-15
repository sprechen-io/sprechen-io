namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Bindet den Katalogparser und die gemeinsame Intent-Abbildung an die Bestätigung einer Sitzung.
/// Die generische Bestätigungsbehandlung verwendet damit dieselben Katalog- und Sprachvorgaben
/// wie die übrige Auswahl.
/// </summary>
internal sealed class FormsCatalogRuntimeConfirmationHandlerFactory
{
    private readonly IFormsCatalogIntentParser _intentParser;

    public FormsCatalogRuntimeConfirmationHandlerFactory(IFormsCatalogIntentParser intentParser)
    {
        _intentParser = intentParser ?? throw new ArgumentNullException(nameof(intentParser));
    }

    public SelectionDialogConfirmationHandler<ApplicationInfo> Create(FormsCatalogDialogSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new SelectionDialogConfirmationHandler<ApplicationInfo>(
            parseIntent: transcript => FormsCatalogSelectionIntentMapper.Map(
                _intentParser.Parse(
                    session.Catalog,
                    transcript,
                    session.IsoCode,
                    session.PolitenessLevel)));
    }
}
