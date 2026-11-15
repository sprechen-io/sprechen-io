using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;

/// <summary>
/// Fordert eine geordnete Zusammenfassung der Felder mit nichtleerer Bezeichnung und
/// nichtleerem Wert an.
/// </summary>
internal sealed record GetFormSummaryQuery : IQuery<FormSummaryResult>;


