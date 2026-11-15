using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;

/// <summary>
/// Hält die geordnete unveränderliche Folge der Zusammenfassungszeilen.
/// </summary>
internal sealed record FormSummaryResult(ImmutableArray<FormSummaryLine> Lines);


