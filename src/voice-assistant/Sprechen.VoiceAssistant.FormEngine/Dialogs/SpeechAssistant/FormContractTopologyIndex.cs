using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Speichert besuchte Gruppen, aufgenommene Felder und ihre Zuordnungen während des Strukturaufbaus, um Wiederholungen zu erkennen.
/// </summary>
internal sealed class FormContractTopologyIndex
{
    private readonly List<string> fieldGroupIds = [];
    private readonly Dictionary<FieldGroupId, string?> parentByFieldGroupId = new();
    private readonly List<string> fieldIds = [];
    private readonly Dictionary<FieldId, string> fieldToFieldGroupId = new();
    private readonly Dictionary<FieldId, string> fieldToVariableId = new();
    private readonly HashSet<FieldGroupId> visitedFieldGroups = [];
    private readonly HashSet<FieldId> addedFields = [];

    public bool TryEnterFieldGroup(FieldGroupId fieldGroupId)
        => visitedFieldGroups.Add(fieldGroupId);

    public void AddFieldGroup(FieldGroupId fieldGroupId, FieldGroupId? parentFieldGroupId)
    {
        parentByFieldGroupId.TryAdd(fieldGroupId, parentFieldGroupId is { } parent ? parent.Value : null);
        fieldGroupIds.Add(fieldGroupId);
    }

    public bool TryEnterField(FieldId fieldId)
        => addedFields.Add(fieldId);

    public void AddField(FieldId fieldId, FieldGroupId fieldGroupId, VariableId variableId)
    {
        fieldIds.Add(fieldId);
        fieldToFieldGroupId[fieldId] = fieldGroupId;
        fieldToVariableId[fieldId] = variableId;
    }

    public FormContractTopology ToTopology()
        => new(
            fieldGroupIds.ToImmutableArray(),
            parentByFieldGroupId.ToImmutableDictionary(),
            fieldIds.ToImmutableArray(),
            fieldToFieldGroupId.ToImmutableDictionary(),
            fieldToVariableId.ToImmutableDictionary());
}
