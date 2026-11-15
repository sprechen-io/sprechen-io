namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;

/// <summary>
/// Hält Fokusbezüge und Zähler des gelesenen Formularstatus zusammen.
/// </summary>
internal sealed record FormStatusResult(
    string? CurrentFieldId,
    string? CurrentFieldGroupId,
    int CurrentFieldIndex,
    int CurrentFieldGroupIndex,
    int TotalFields,
    int TotalFieldGroups,
    int OpenRequiredCount,
    int ValidationErrorCount);


