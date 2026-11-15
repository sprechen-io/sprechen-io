namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet das Auswahlergebnis mit einer optionalen Sprache für den Neustart.
/// Der äußere Katalogdialog kann damit zwischen Rückgabe und erneutem Laden entscheiden.
/// </summary>
internal sealed record FormsCatalogDialogRuntimeResult(
    FormsCatalogDialogResult Result,
    string? RestartIsoCode);
