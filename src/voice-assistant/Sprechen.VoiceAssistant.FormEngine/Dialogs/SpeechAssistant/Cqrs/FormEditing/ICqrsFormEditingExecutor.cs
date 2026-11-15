using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormEditing;

/// <summary>
/// Bietet Dialogen eine gemeinsame Schnittstelle für die Übernahme freier Transkripte und bereits zusammengesetzter Werte.
/// </summary>
internal interface ICqrsFormEditingExecutor
{
    Task<FormInputResult> ApplyTranscriptAsync(string transcript);

    Task<FormInputResult> ApplyComposedValueAsync(string composedValue);
}
