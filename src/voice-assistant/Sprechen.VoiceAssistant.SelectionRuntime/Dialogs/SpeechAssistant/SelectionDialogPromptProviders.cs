namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt asynchrone Funktionen für die Ansagen der Auswahlphasen.
/// Die gemeinsame Laufzeit fordert darüber die jeweils modellabhängigen Texte an.
/// </summary>
public sealed class SelectionDialogPromptProviders<TSelection>
    where TSelection : class
{
    public SelectionDialogPromptProviders(
        Func<SelectionDialogTurnSnapshot<TSelection>, CancellationToken, Task<SelectionDialogPrompt>> getEmptyItemsPromptAsync,
        Func<SelectionDialogTurnSnapshot<TSelection>, CancellationToken, Task<SelectionDialogPrompt>> getAnnouncingPromptAsync,
        Func<SelectionDialogTurnSnapshot<TSelection>, CancellationToken, Task<SelectionDialogPrompt>> getNoInputPromptAsync,
        Func<SelectionDialogTurnSnapshot<TSelection>, CancellationToken, Task<SelectionDialogPrompt>> getReListenPromptAsync,
        Func<SelectionDialogTurnSnapshot<TSelection>, CancellationToken, Task<SelectionDialogPrompt>> getCaptureFailedTerminalPromptAsync,
        Func<SelectionDialogTurnSnapshot<TSelection>, CancellationToken, Task<SelectionDialogPrompt>> getRecoverableFailurePromptAsync,
        Func<SelectionConfirmationPromptRequest<TSelection>, CancellationToken, Task<SelectionDialogPrompt>> getConfirmationPromptAsync,
        Func<SelectionDialogTurnSnapshot<TSelection>, CancellationToken, Task<SelectionDialogPrompt>>? getRecognizerUnavailablePromptAsync = null)
    {
        GetEmptyItemsPromptAsync = getEmptyItemsPromptAsync ?? throw new ArgumentNullException(nameof(getEmptyItemsPromptAsync));
        GetAnnouncingPromptAsync = getAnnouncingPromptAsync ?? throw new ArgumentNullException(nameof(getAnnouncingPromptAsync));
        GetNoInputPromptAsync = getNoInputPromptAsync ?? throw new ArgumentNullException(nameof(getNoInputPromptAsync));
        GetReListenPromptAsync = getReListenPromptAsync ?? throw new ArgumentNullException(nameof(getReListenPromptAsync));
        GetCaptureFailedTerminalPromptAsync = getCaptureFailedTerminalPromptAsync
            ?? throw new ArgumentNullException(nameof(getCaptureFailedTerminalPromptAsync));
        GetRecoverableFailurePromptAsync = getRecoverableFailurePromptAsync ?? throw new ArgumentNullException(nameof(getRecoverableFailurePromptAsync));
        GetRecognizerUnavailablePromptAsync = getRecognizerUnavailablePromptAsync ?? getRecoverableFailurePromptAsync;
        GetConfirmationPromptAsync = getConfirmationPromptAsync ?? throw new ArgumentNullException(nameof(getConfirmationPromptAsync));
    }

    public Func<SelectionDialogTurnSnapshot<TSelection>, CancellationToken, Task<SelectionDialogPrompt>> GetEmptyItemsPromptAsync { get; }

    public Func<SelectionDialogTurnSnapshot<TSelection>, CancellationToken, Task<SelectionDialogPrompt>> GetAnnouncingPromptAsync { get; }

    public Func<SelectionDialogTurnSnapshot<TSelection>, CancellationToken, Task<SelectionDialogPrompt>> GetNoInputPromptAsync { get; }

    public Func<SelectionDialogTurnSnapshot<TSelection>, CancellationToken, Task<SelectionDialogPrompt>> GetReListenPromptAsync { get; }

    public Func<SelectionDialogTurnSnapshot<TSelection>, CancellationToken, Task<SelectionDialogPrompt>> GetCaptureFailedTerminalPromptAsync { get; }

    public Func<SelectionDialogTurnSnapshot<TSelection>, CancellationToken, Task<SelectionDialogPrompt>> GetRecoverableFailurePromptAsync { get; }

    /// <summary>
    /// Liefert die Ansage für Aufnahmefehler der technischen Infrastruktur. Ohne
    /// eigene Funktion wird GetRecoverableFailurePromptAsync verwendet. Den
    /// konkreten Wortlaut bestimmt die übergebene Funktion.
    /// </summary>
    public Func<SelectionDialogTurnSnapshot<TSelection>, CancellationToken, Task<SelectionDialogPrompt>> GetRecognizerUnavailablePromptAsync { get; }

    public Func<SelectionConfirmationPromptRequest<TSelection>, CancellationToken, Task<SelectionDialogPrompt>> GetConfirmationPromptAsync { get; }
}
