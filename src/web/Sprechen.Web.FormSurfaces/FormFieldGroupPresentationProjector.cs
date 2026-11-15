using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.Web.FormSurfaces;

/// <summary>
/// Bündelt Felder, Status und Anzeigewerte für die Darstellung nach Feldgruppen.
/// </summary>
internal sealed class FormFieldGroupPresentationProjector
{
    private readonly FormDisplayValuePresenter _displayValuePresenter;

    public FormFieldGroupPresentationProjector(FormDisplayValuePresenter displayValuePresenter)
    {
        _displayValuePresenter = displayValuePresenter ?? throw new ArgumentNullException(nameof(displayValuePresenter));
    }

    public ImmutableArray<FormFieldGroupPresentation> Project(FormSurfaceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var orderedFieldGroups = new List<FormSurfaceFieldGroupInfo>();
        var groupedFields = new Dictionary<string, List<FormFieldPresentation>>(StringComparer.Ordinal);
        var seenFieldGroupIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var field in snapshot.FormProjection.Fields)
        {
            if (seenFieldGroupIds.Add(field.FieldGroupId))
            {
                orderedFieldGroups.Add(ResolveFieldGroup(snapshot.FormProjection, field.FieldGroupId));
            }

            if (!groupedFields.TryGetValue(field.FieldGroupId, out var fields))
            {
                fields = [];
                groupedFields[field.FieldGroupId] = fields;
            }

            fields.Add(ProjectField(snapshot, field));
        }

        return orderedFieldGroups
            .Select(fieldGroup => ProjectFieldGroup(snapshot, fieldGroup, groupedFields))
            .ToImmutableArray();
    }

    private FormFieldPresentation ProjectField(FormSurfaceSnapshot snapshot, FormSurfaceField field)
    {
        snapshot.FieldStatusesById.TryGetValue(field.FieldId, out var status);
        var displayValue = _displayValuePresenter.TryResolveDisplayValue(field, snapshot.FieldValuesById);

        return new FormFieldPresentation(
            field,
            displayValue,
            !string.IsNullOrWhiteSpace(displayValue),
            string.Equals(snapshot.FocusedFieldId, field.FieldId, StringComparison.Ordinal),
            status);
    }

    private static FormFieldGroupPresentation ProjectFieldGroup(
        FormSurfaceSnapshot snapshot,
        FormSurfaceFieldGroupInfo fieldGroup,
        IReadOnlyDictionary<string, List<FormFieldPresentation>> groupedFields)
    {
        snapshot.FieldGroupStatusesById.TryGetValue(fieldGroup.FieldGroupId, out var status);
        groupedFields.TryGetValue(fieldGroup.FieldGroupId, out var fields);

        return new FormFieldGroupPresentation(
            fieldGroup,
            string.Equals(snapshot.FormProjection.EntryFieldGroupId, fieldGroup.FieldGroupId, StringComparison.Ordinal),
            status,
            (fields ?? []).ToImmutableArray());
    }

    private static FormSurfaceFieldGroupInfo ResolveFieldGroup(FormSurfaceDefinition formProjection, string fieldGroupId)
    {
        if (formProjection.FieldGroupsById.TryGetValue(fieldGroupId, out var fieldGroup))
        {
            return fieldGroup;
        }

        return new FormSurfaceFieldGroupInfo(fieldGroupId, fieldGroupId, null, null);
    }
}
