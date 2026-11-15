using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Hält Variablen, Felder und Feldgruppen nach ihren modellierten Kennungen indiziert bereit,
/// damit Laufzeitdienste dieselben Definitionen gezielt auflösen können.
/// </summary>
internal sealed record FormContractDefinitionIndexes(
    ImmutableDictionary<VariableId, VariableDefinition> VariablesById,
    ImmutableDictionary<FieldId, FieldDefinition> FieldsById,
    ImmutableDictionary<FieldGroupId, FieldGroupDefinition> FieldGroupsById);


