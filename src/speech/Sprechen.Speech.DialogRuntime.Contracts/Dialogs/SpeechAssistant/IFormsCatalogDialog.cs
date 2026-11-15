using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Wählt eine Anwendung aus dem Katalog, wahlweise anhand eines bereits erkannten
/// Transkripts, und liefert die Auswahl oder den Abbruchgrund an den Sitzungsablauf.
/// </summary>
public interface IFormsCatalogDialog
{
    Task<FormsCatalogDialogResult> ExecuteAsync(
        string languageCode,
        string correlationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Beginnt die Anwendungsauswahl mit einem bereits erkannten Transkript.
    /// </summary>
    Task<FormsCatalogDialogResult> ExecuteAsync(
        string languageCode,
        string correlationId,
        SpeechTranscript initialTranscript,
        CancellationToken cancellationToken);
}
