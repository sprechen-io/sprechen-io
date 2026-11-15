using System.Collections.Immutable;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Beschreibt die aktuelle Feldgruppe und die verfügbaren Ziele zur Formularorientierung.
/// </summary>
public sealed record FormDiscoverySnapshot(
    string CurrentFieldGroupId,
    string CurrentFieldGroupTitle,
    ImmutableArray<FormFieldGroupDiscoveryItem> FieldGroups,
    ImmutableArray<FormFieldDiscoveryItem> CurrentFieldGroupFields);
