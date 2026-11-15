using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;

/// <summary>
/// Fordert den aktuellen Formularstatus ohne Zustandsänderung an.
/// </summary>
internal sealed record GetFormStatusQuery : IQuery<FormStatusResult>;


