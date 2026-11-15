using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormVoice;
using Sprechen.Mda.Contracts.V1.Preflight;

namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Prüft Formularverträge und ihre referenzierten Definitionen, damit nur auflösbare und im Katalog enthaltene Verträge ausgeliefert werden.
/// </summary>
internal sealed class FormContractBundleConsistencyValidator
{
    public FormContractBundleConsistencyResult Validate(
        IReadOnlySet<FormContractKey> referencedContractKeys,
        MdaReferenceArtifacts references)
    {
        ArgumentNullException.ThrowIfNull(referencedContractKeys);
        ArgumentNullException.ThrowIfNull(references);

        var referencedFieldDefinitionsKeys = new HashSet<FieldDefinitionsKey>();
        var referencedVariableDefinitionsKeys = new HashSet<VariableDefinitionsKey>();
        var referencedFormVoiceProfileKeys = new HashSet<FormVoiceProfileKey>();

        foreach (var (contractKey, contractDocument) in references.FormContracts.Models)
        {
            ValidateContractIsCatalogReferenced(referencedContractKeys, contractKey);
            var fieldCatalogKey = ResolveFieldCatalogKey(contractDocument, references);
            var variableCatalogKey = ResolveVariableCatalogKey(contractDocument, references);
            var formVoiceProfile = ResolveFormVoiceProfile(contractDocument, references, referencedFormVoiceProfileKeys);

            ValidateFormContractDocument(contractKey, contractDocument, fieldCatalogKey.Model, variableCatalogKey.Model, formVoiceProfile);
            ValidateResolvedFormContract(contractDocument, fieldCatalogKey.Model, variableCatalogKey.Model, formVoiceProfile);

            referencedFieldDefinitionsKeys.Add(fieldCatalogKey.Key);
            referencedVariableDefinitionsKeys.Add(variableCatalogKey.Key);
        }

        return new FormContractBundleConsistencyResult(
            referencedFieldDefinitionsKeys,
            referencedVariableDefinitionsKeys,
            referencedFormVoiceProfileKeys);
    }

    private static void ValidateContractIsCatalogReferenced(
        IReadOnlySet<FormContractKey> referencedContractKeys,
        FormContractKey contractKey)
    {
        if (referencedContractKeys.Contains(contractKey))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Runtime bundle contains FormContract not referenced by FormsCatalog (applicationId='{contractKey.ApplicationId}', formId='{contractKey.FormId}', contractVersion='{contractKey.ContractVersion}').");
    }

    private static (FieldDefinitionsKey Key, Sprechen.Mda.Contracts.V1.Fields.FieldDefinitions Model) ResolveFieldCatalogKey(
        FormContractDocument contractDocument,
        MdaReferenceArtifacts references)
    {
        var key = MdaArtifactKeyFactory.CreateFieldDefinitionsKey(
            contractDocument.FieldDefinitions.FieldDefinitionsId,
            contractDocument.FieldDefinitions.CatalogVersion);
        if (!references.FieldDefinitions.Models.TryGetValue(key, out var model))
        {
            throw new InvalidOperationException(
                $"FormContract references missing FieldDefinitions (fieldDefinitionsId='{key.FieldDefinitionsId}', catalogVersion='{key.CatalogVersion}').");
        }

        return (key, model);
    }

    private static (VariableDefinitionsKey Key, Sprechen.Mda.Contracts.V1.Variables.VariableDefinitions Model) ResolveVariableCatalogKey(
        FormContractDocument contractDocument,
        MdaReferenceArtifacts references)
    {
        var key = MdaArtifactKeyFactory.CreateVariableDefinitionsKey(
            contractDocument.VariableDefinitions.VariableDefinitionsId,
            contractDocument.VariableDefinitions.CatalogVersion);
        if (!references.VariableDefinitions.Models.TryGetValue(key, out var model))
        {
            throw new InvalidOperationException(
                $"FormContract references missing VariableDefinitions (variableDefinitionsId='{key.VariableDefinitionsId}', catalogVersion='{key.CatalogVersion}').");
        }

        return (key, model);
    }

    private static FormVoiceProfile? ResolveFormVoiceProfile(
        FormContractDocument contractDocument,
        MdaReferenceArtifacts references,
        ISet<FormVoiceProfileKey> referencedFormVoiceProfileKeys)
    {
        var reference = contractDocument.Projections?.Voice?.FormVoiceProfile;
        if (reference is null)
        {
            return null;
        }

        var key = MdaArtifactKeyFactory.CreateFormVoiceProfileKey(reference.FormVoiceProfileId, reference.ProfileVersion);
        if (!references.FormVoiceProfiles.Models.TryGetValue(key, out var formVoiceProfile))
        {
            throw new InvalidOperationException(
                $"FormContract references missing FormVoiceProfile (formVoiceProfileId='{key.FormVoiceProfileId}', profileVersion='{key.ProfileVersion}').");
        }

        referencedFormVoiceProfileKeys.Add(key);
        return formVoiceProfile;
    }

    private static void ValidateFormContractDocument(
        FormContractKey contractKey,
        FormContractDocument contractDocument,
        Sprechen.Mda.Contracts.V1.Fields.FieldDefinitions fieldCatalog,
        Sprechen.Mda.Contracts.V1.Variables.VariableDefinitions variableCatalog,
        FormVoiceProfile? formVoiceProfile)
    {
        var preflight = FormContractDocumentPreflight.Validate(contractDocument, fieldCatalog, variableCatalog, formVoiceProfile);
        if (!preflight.Ok)
        {
            throw new InvalidOperationException(
                $"FormContract document preflight failed ({contractKey.ApplicationId}/{contractKey.FormId}/{contractKey.ContractVersion}): {PreflightIssueFormatter.Format(preflight.Errors)}");
        }
    }

    private static void ValidateResolvedFormContract(
        FormContractDocument contractDocument,
        Sprechen.Mda.Contracts.V1.Fields.FieldDefinitions fieldCatalog,
        Sprechen.Mda.Contracts.V1.Variables.VariableDefinitions variableCatalog,
        FormVoiceProfile? formVoiceProfile)
    {
        var resolvedContract = FormContractResolver.Resolve(contractDocument, fieldCatalog, variableCatalog, formVoiceProfile);
        var preflight = FormContractPreflight.Validate(resolvedContract);
        if (!preflight.Ok)
        {
            throw new InvalidOperationException(
                $"FormContract preflight failed ({resolvedContract.ApplicationId}/{resolvedContract.FormId}/{resolvedContract.ContractVersion}): {PreflightIssueFormatter.Format(preflight.Errors)}");
        }
    }
}
