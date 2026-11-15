using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Durchläuft erreichbare Feldgruppen in Tiefensuche und hält ihre erste Zuordnung und Reihenfolge für die Formularstruktur fest.
/// </summary>
internal sealed class FormContractTopologyTraversal
{
    private readonly ImmutableDictionary<FieldGroupId, FieldGroupDefinition> fieldGroupsById;
    private readonly ImmutableDictionary<FieldId, FieldDefinition> fieldsById;
    private readonly FormContractTopologyIndex index = new();

    public FormContractTopologyTraversal(
        ImmutableDictionary<FieldGroupId, FieldGroupDefinition> fieldGroupsById,
        ImmutableDictionary<FieldId, FieldDefinition> fieldsById)
    {
        this.fieldGroupsById = fieldGroupsById ?? throw new ArgumentNullException(nameof(fieldGroupsById));
        this.fieldsById = fieldsById ?? throw new ArgumentNullException(nameof(fieldsById));
    }

    public FormContractTopology Build(FormContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        if (contract.EntryFieldGroupId != default)
        {
            VisitFieldGroup(contract.EntryFieldGroupId, parentFieldGroupId: null);
        }

        return index.ToTopology();
    }

    private void VisitFieldGroup(FieldGroupId fieldGroupId, FieldGroupId? parentFieldGroupId)
    {
        if (!index.TryEnterFieldGroup(fieldGroupId))
        {
            return;
        }

        if (!fieldGroupsById.TryGetValue(fieldGroupId, out var fieldGroup))
        {
            throw new InvalidOperationException("A field-group identifier was not an exact canonical key.");
        }

        index.AddFieldGroup(fieldGroupId, parentFieldGroupId);

        foreach (var member in fieldGroup.Members)
        {
            VisitMember(member, fieldGroupId);
        }
    }

    private void VisitMember(FieldGroupMember? member, FieldGroupId parentFieldGroupId)
    {
        if (member is null)
        {
            return;
        }

        if (string.Equals(member.Kind, MemberKinds.Field, StringComparison.OrdinalIgnoreCase))
        {
            VisitFieldMember(member, parentFieldGroupId);
            return;
        }

        if (string.Equals(member.Kind, MemberKinds.FieldGroup, StringComparison.OrdinalIgnoreCase))
        {
            VisitFieldGroupMember(member, parentFieldGroupId);
        }
    }

    private void VisitFieldMember(FieldGroupMember member, FieldGroupId fieldGroupId)
    {
        if (string.IsNullOrWhiteSpace(member.FieldId))
        {
            return;
        }

        var fieldId = member.FieldId.Value;
        if (!index.TryEnterField(fieldId))
        {
            return;
        }

        if (!fieldsById.TryGetValue(fieldId, out var field) || field.VariableId == default)
        {
            throw new InvalidOperationException("A field identifier was not an exact canonical key.");
        }

        index.AddField(fieldId, fieldGroupId, field.VariableId);
    }

    private void VisitFieldGroupMember(FieldGroupMember member, FieldGroupId parentFieldGroupId)
    {
        if (!string.IsNullOrWhiteSpace(member.FieldGroupId))
        {
            VisitFieldGroup(member.FieldGroupId.Value, parentFieldGroupId);
        }
    }
}
