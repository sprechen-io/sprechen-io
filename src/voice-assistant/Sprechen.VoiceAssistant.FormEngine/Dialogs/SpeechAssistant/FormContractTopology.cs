using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Hält die geordneten Feldgruppen- und Feldbeziehungen eines Formularvertrags bereit,
/// damit Navigation und Projektion dieselbe erschlossene Struktur verwenden.
/// </summary>
internal sealed record FormContractTopology(
    ImmutableArray<string> OrderedFieldGroupIds,
    ImmutableDictionary<FieldGroupId, string?> ParentFieldGroupById,
    ImmutableArray<string> OrderedFieldIds,
    ImmutableDictionary<FieldId, string> FieldToFieldGroupId,
    ImmutableDictionary<FieldId, string> FieldToVariableId);

