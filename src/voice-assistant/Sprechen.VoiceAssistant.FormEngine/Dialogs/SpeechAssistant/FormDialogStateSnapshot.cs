using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Hält die veränderlichen Formularwerte und das Fokusziel für eine spätere
/// Wiederherstellung zusammen. Die enthaltenen Wörterbücher bleiben veränderlich.
/// </summary>
public sealed record FormDialogStateSnapshot(
    Dictionary<string, string> ValuesByVariableId,
    Dictionary<FieldGroupId, bool> FieldGroupVisibleById,
    Dictionary<FieldId, bool> FieldVisibleById,
    Dictionary<FieldId, bool> FieldEnabledById,
    Dictionary<FieldId, bool> FieldReadOnlyById,
    Dictionary<FieldId, string> FieldInputModeOverrides,
    string? FocusedFieldId);


