using Sprechen.Contracts.Services.Loading;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Contracts.Services;

/// <summary>
/// Sucht Artefakte über getrennte typisierte Schlüssel, damit jede Artefaktart im zugehörigen Bestand nachgeschlagen wird.
/// </summary>
internal sealed class MdaContractsStoreArtifactIndex
{
    private readonly IReadOnlyDictionary<FieldDefinitionsKey, MdaJsonArtifact> _fieldCatalogsByKey;
    private readonly IReadOnlyDictionary<VariableDefinitionsKey, MdaJsonArtifact> _variableCatalogsByKey;
    private readonly IReadOnlyDictionary<FormVoiceProfileKey, MdaJsonArtifact> _formVoiceProfilesByKey;
    private readonly IReadOnlyDictionary<SelectionDialogKey, MdaJsonArtifact> _selectionDialogsByKey;
    private readonly IReadOnlyDictionary<ApplicationSelectionDialogProfileKey, MdaJsonArtifact> _selectionDialogProfilesByKey;
    private readonly IReadOnlyDictionary<FormContractKey, MdaJsonArtifact> _formContractsByKey;

    public MdaContractsStoreArtifactIndex(MdaContractsBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        _fieldCatalogsByKey = bundle.FieldDefinitionsByKey;
        _variableCatalogsByKey = bundle.VariableDefinitionsByKey;
        _formVoiceProfilesByKey = bundle.FormVoiceProfilesByKey;
        _selectionDialogsByKey = bundle.ApplicationSelectionDialogsByKey;
        _selectionDialogProfilesByKey = bundle.ApplicationSelectionDialogProfilesByKey;
        _formContractsByKey = bundle.FormContractsByKey;
    }

    public bool TryGetFieldDefinitions(string fieldDefinitionsId, string catalogVersion, out MdaJsonArtifact artifact)
    {
        var key = MdaArtifactKeyFactory.CreateFieldDefinitionsKey(fieldDefinitionsId, catalogVersion);
        return _fieldCatalogsByKey.TryGetValue(key, out artifact!);
    }

    public bool TryGetVariableDefinitions(string variableDefinitionsId, string catalogVersion, out MdaJsonArtifact artifact)
    {
        var key = MdaArtifactKeyFactory.CreateVariableDefinitionsKey(variableDefinitionsId, catalogVersion);
        return _variableCatalogsByKey.TryGetValue(key, out artifact!);
    }

    public bool TryGetFormVoiceProfile(string formVoiceProfileId, string profileVersion, out MdaJsonArtifact artifact)
    {
        var key = MdaArtifactKeyFactory.CreateFormVoiceProfileKey(formVoiceProfileId, profileVersion);
        return _formVoiceProfilesByKey.TryGetValue(key, out artifact!);
    }

    public bool TryGetApplicationSelectionDialog(string selectionDialogId, string modelVersion, out MdaJsonArtifact artifact)
    {
        var key = MdaArtifactKeyFactory.CreateSelectionDialogKey(selectionDialogId, modelVersion);
        return _selectionDialogsByKey.TryGetValue(key, out artifact!);
    }

    public bool TryGetApplicationSelectionDialogProfile(string profileId, string profileVersion, out MdaJsonArtifact artifact)
    {
        var key = MdaArtifactKeyFactory.CreateApplicationSelectionDialogProfileKey(profileId, profileVersion);
        return _selectionDialogProfilesByKey.TryGetValue(key, out artifact!);
    }

    public bool TryGetFormContract(string applicationId, string formId, string contractVersion, out MdaJsonArtifact artifact)
    {
        var key = MdaArtifactKeyFactory.CreateContractKey(applicationId, formId, contractVersion);
        return _formContractsByKey.TryGetValue(key, out artifact!);
    }
}
