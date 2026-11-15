namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Navigation, Bearbeitungsfortschritt und Fehlerzahlen für die Formularstatusauskunft.
/// </summary>
public sealed record FormStatusSnapshot(
    FormStatusNavigationPosition Navigation,
    FormStatusCompletionProgress Progress,
    string CurrentFieldLabel,
    int CurrentFieldIndex,
    string CurrentFieldGroupTitle,
    int OpenRequiredFieldCount,
    int ValidationErrorCount);


