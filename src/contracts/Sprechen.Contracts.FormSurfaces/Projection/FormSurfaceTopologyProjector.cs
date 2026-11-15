using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Ordnet Felder und Feldgruppen vom Einstieg aus nach dem Vertrag an,
/// damit die Formularoberfläche dessen Struktur übernimmt.
/// </summary>
internal sealed class FormSurfaceTopologyProjector
{
    private readonly FormFieldGroupMetadataProjector _fieldGroupMetadataProjector;
    private readonly FormFieldProjector _fieldProjector;

    public FormSurfaceTopologyProjector(
        FormFieldGroupMetadataProjector fieldGroupMetadataProjector,
        FormFieldProjector fieldProjector)
    {
        _fieldGroupMetadataProjector = fieldGroupMetadataProjector ?? throw new ArgumentNullException(nameof(fieldGroupMetadataProjector));
        _fieldProjector = fieldProjector ?? throw new ArgumentNullException(nameof(fieldProjector));
    }

    public FormSurfaceDefinition Project(
        FormContract contract,
        string entryFieldGroupId,
        string requestedLanguage,
        PolitenessLevel politenessLevel,
        FormSurfaceProjectionIndex index,
        IReadOnlyDictionary<string, FieldConstraints> constraintsByFieldId)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryFieldGroupId);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedLanguage);
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(constraintsByFieldId);

        if (!Enum.IsDefined(politenessLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(politenessLevel), politenessLevel, "Unknown politeness level.");
        }

        var fields = new List<FormSurfaceField>();
        var fieldGroupsInfoById = new Dictionary<string, FormSurfaceFieldGroupInfo>(StringComparer.Ordinal);
        var visitedFieldGroups = new HashSet<string>(StringComparer.Ordinal);
        var addedFields = new HashSet<string>(StringComparer.Ordinal);

        VisitFieldGroup(
            contract,
            entryFieldGroupId,
            requestedLanguage,
            politenessLevel,
            index,
            constraintsByFieldId,
            visitedFieldGroups,
            addedFields,
            fieldGroupsInfoById,
            fields);

        return new FormSurfaceDefinition(entryFieldGroupId, fieldGroupsInfoById, fields);
    }

    private void VisitFieldGroup(
        FormContract contract,
        string fieldGroupId,
        string requestedLanguage,
        PolitenessLevel politenessLevel,
        FormSurfaceProjectionIndex index,
        IReadOnlyDictionary<string, FieldConstraints> constraintsByFieldId,
        HashSet<string> visitedFieldGroups,
        HashSet<string> addedFields,
        Dictionary<string, FormSurfaceFieldGroupInfo> fieldGroupsInfoById,
        List<FormSurfaceField> fields)
    {
        if (!visitedFieldGroups.Add(fieldGroupId))
        {
            return;
        }

        if (!index.FieldGroupsById.TryGetValue(fieldGroupId, out var fieldGroup))
        {
            throw new InvalidOperationException($"Form surface field group '{fieldGroupId}' was not found.");
        }

        fieldGroupsInfoById.TryAdd(
            fieldGroupId,
            _fieldGroupMetadataProjector.Project(contract, fieldGroup, requestedLanguage, politenessLevel));

        foreach (var member in fieldGroup.Members)
        {
            if (member is null)
            {
                continue;
            }

            if (string.Equals(member.Kind, MemberKinds.Field, StringComparison.OrdinalIgnoreCase))
            {
                AddField(contract, fieldGroupId, member, requestedLanguage, politenessLevel, index, constraintsByFieldId, addedFields, fields);
                continue;
            }

            if (string.Equals(member.Kind, MemberKinds.FieldGroup, StringComparison.OrdinalIgnoreCase))
            {
                VisitFieldGroup(
                    contract,
                    ResolveFieldGroupId(member),
                    requestedLanguage,
                    politenessLevel,
                    index,
                    constraintsByFieldId,
                    visitedFieldGroups,
                    addedFields,
                    fieldGroupsInfoById,
                    fields);
            }
        }
    }

    private void AddField(
        FormContract contract,
        string fieldGroupId,
        FieldGroupMember member,
        string requestedLanguage,
        PolitenessLevel politenessLevel,
        FormSurfaceProjectionIndex index,
        IReadOnlyDictionary<string, FieldConstraints> constraintsByFieldId,
        HashSet<string> addedFields,
        List<FormSurfaceField> fields)
    {
        var fieldId = ResolveFieldId(member);
        if (!addedFields.Add(fieldId))
        {
            throw new InvalidOperationException("A modeled identifier was not an exact unique canonical key.");
        }

        if (!index.FieldsById.TryGetValue(fieldId, out var field))
        {
            throw new InvalidOperationException($"Form surface field '{fieldId}' was not found.");
        }

        var fieldConstraints = constraintsByFieldId.TryGetValue(fieldId, out var constraints)
            ? constraints
            : FieldConstraints.Empty;
        fields.Add(_fieldProjector.Project(
            contract,
            field,
            fieldGroupId,
            index.VariablesById,
            fieldConstraints,
            requestedLanguage,
            politenessLevel));
    }

    private static string ResolveFieldId(FieldGroupMember member)
    {
        if (string.IsNullOrWhiteSpace(member.FieldId))
        {
            throw new InvalidOperationException("Form surface field member is missing fieldId.");
        }

        return member.FieldId.Value.Value;
    }

    private static string ResolveFieldGroupId(FieldGroupMember member)
    {
        if (string.IsNullOrWhiteSpace(member.FieldGroupId))
        {
            throw new InvalidOperationException("Form surface field-group member is missing fieldGroupId.");
        }

        return member.FieldGroupId.Value.Value;
    }
}
