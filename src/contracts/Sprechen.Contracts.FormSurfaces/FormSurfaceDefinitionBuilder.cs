using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Führt Feldzuordnung, Formularstruktur und Validierungsregeln zusammen,
/// damit alle Oberflächen dieselbe Formulardefinition erhalten.
/// </summary>
public sealed class FormSurfaceDefinitionBuilder : IFormSurfaceDefinitionBuilder
{
    private readonly FormConstraintProjector _constraintProjector;
    private readonly FormSurfaceProjectionIndexBuilder _indexBuilder;
    private readonly FormSurfaceTopologyProjector _topologyProjector;

    public FormSurfaceDefinitionBuilder()
    {
        var accessibilityLabelSelector = new FormAccessibilityLabelSelector();
        var optionProjector = new FormOptionProjector();
        var inputTypeMapper = new FormInputTypeMapper();
        var fieldGroupMetadataProjector = new FormFieldGroupMetadataProjector();
        var fieldProjector = new FormFieldProjector(accessibilityLabelSelector, optionProjector, inputTypeMapper);

        _constraintProjector = new FormConstraintProjector();
        _indexBuilder = new FormSurfaceProjectionIndexBuilder();
        _topologyProjector = new FormSurfaceTopologyProjector(fieldGroupMetadataProjector, fieldProjector);
    }

    internal FormSurfaceDefinitionBuilder(
        FormConstraintProjector constraintProjector,
        FormSurfaceProjectionIndexBuilder indexBuilder,
        FormSurfaceTopologyProjector topologyProjector)
    {
        _constraintProjector = constraintProjector ?? throw new ArgumentNullException(nameof(constraintProjector));
        _indexBuilder = indexBuilder ?? throw new ArgumentNullException(nameof(indexBuilder));
        _topologyProjector = topologyProjector ?? throw new ArgumentNullException(nameof(topologyProjector));
    }

    public FormSurfaceDefinition BuildFormSurfaceDefinition(
        FormContract contract,
        string requestedLanguage,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedLanguage);

        if (!Enum.IsDefined(politenessLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(politenessLevel), politenessLevel, "Unknown politeness level.");
        }

        if (contract.Fields.Count == 0 || contract.FieldGroups.Count == 0 || contract.EntryFieldGroupId == default)
        {
            return EmptyDefinition();
        }

        var index = _indexBuilder.Build(contract);
        var entryFieldGroupId = contract.EntryFieldGroupId.Value;
        var constraintsByFieldId = _constraintProjector.ProjectByFieldId(contract, index.FieldsById, index.VariablesById);
        return _topologyProjector.Project(contract, entryFieldGroupId, requestedLanguage, politenessLevel, index, constraintsByFieldId);
    }

    private static FormSurfaceDefinition EmptyDefinition()
    {
        return new FormSurfaceDefinition(
            entryFieldGroupId: string.Empty,
            fieldGroupsById: new Dictionary<string, FormSurfaceFieldGroupInfo>(StringComparer.Ordinal),
            fields: Array.Empty<FormSurfaceField>());
    }

}
