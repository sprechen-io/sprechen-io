using Json.Schema;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Lädt Formularverträge mit Anwendung, Formular und Version, damit doppelte Vertragsdefinitionen beim Laden auffallen.
/// </summary>
internal sealed class FormContractReferenceArtifactLoader
{
    private readonly MdaReferenceArtifactSetLoader _setLoader;

    public FormContractReferenceArtifactLoader(MdaReferenceArtifactSetLoader setLoader)
    {
        _setLoader = setLoader ?? throw new ArgumentNullException(nameof(setLoader));
    }

    public ArtifactSet<FormContractKey, FormContractDocument> Load(JsonSchema schema)
    {
        return _setLoader.LoadCategory<FormContractKey, FormContractDocument>(
            MdaContractsArtifactPaths.FormContractsDirectoryPath,
            MdaContractsArtifactPaths.FormContractsCategoryName,
            schema,
            model => MdaArtifactKeyFactory.CreateContractKey(model.ApplicationId, model.FormId, model.ContractVersion),
            key => $"Duplicate FormContract key (applicationId='{key.ApplicationId}', formId='{key.FormId}', contractVersion='{key.ContractVersion}').");
    }
}
