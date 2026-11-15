using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Ausgewählte Felder und Gruppen für eine gemeinsame Statusprojektion.
/// </summary>
internal sealed record FormDialogProjectionScope(
    ImmutableArray<string> VisibleFieldIds,
    ImmutableArray<string> RelevantFieldGroupIds,
    ImmutableArray<string> AnswerableFieldIds);
