namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Sprechtempo, Listenpausen, die Erlaubnis zum Unterbrechen einer Ansage
/// und Vertrauensgrenzen für den Auswahldialog. Der Aufrufer übergibt diese Werte.
/// Der Datensatz liest selbst kein Modell.
/// </summary>
public sealed record SelectionDialogInteractionSettings(
    double SpeechRate,
    int PauseBetweenItemsMs,
    bool AllowBargeIn,
    int SttMinConfidenceToAccept,
    int SttMinConfidenceToConfirm);
