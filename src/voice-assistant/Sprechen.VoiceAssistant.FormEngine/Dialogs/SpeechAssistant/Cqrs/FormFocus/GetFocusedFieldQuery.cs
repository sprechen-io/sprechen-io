using Sprechen.Common.Cqrs;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormFocus;

/// <summary>
/// Fordert die Momentaufnahme des aktuell fokussierten Felds an.
/// </summary>
internal sealed record GetFocusedFieldQuery : IQuery<FormFieldSnapshot>;
