namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Bindet den gemeinsamen Katalogparser an die Bestätigung innerhalb einer Formularsitzung. Die
/// generische Bestätigungsbehandlung erhält dadurch dieselben Sprach- und Katalogvorgaben wie die
/// Auswahl.
/// </summary>
internal sealed class FormSelectionRuntimeConfirmationHandlerFactory
{
    private readonly IFormsCatalogIntentParser _intentParser;

    public FormSelectionRuntimeConfirmationHandlerFactory(IFormsCatalogIntentParser intentParser)
    {
        _intentParser = intentParser ?? throw new ArgumentNullException(nameof(intentParser));
    }

    public SelectionDialogConfirmationHandler<FormInfo> Create(FormSelectionDialogSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new SelectionDialogConfirmationHandler<FormInfo>(
            parseIntent: transcript => FormsCatalogSelectionIntentMapper.Map(
                _intentParser.Parse(
                    session.Catalog,
                    transcript,
                    session.IsoCode,
                    session.PolitenessLevel)));
    }
}
