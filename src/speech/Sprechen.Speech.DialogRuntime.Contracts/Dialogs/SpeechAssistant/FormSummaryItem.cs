namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ein Feld in der gesprochenen Formularzusammenfassung. <see cref="Status"/> muss
/// vollständig ausgewertet werden und darf nicht auf einen Wahrheitswert reduziert werden.
/// </summary>
public sealed record FormSummaryItem(
    string FieldId,
    string FieldLabel,
    string? SpokenValue,
    FormSpokenValueProjectionStatus Status);
