using Json.Schema;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Preflight;

namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Lädt und prüft Auswahlprofile mit Kennung und Version, damit ungültige oder doppelte Profile den Start abbrechen.
/// </summary>
internal sealed class ApplicationSelectionDialogProfileReferenceArtifactLoader
{
    private readonly MdaReferenceArtifactSetLoader _setLoader;

    public ApplicationSelectionDialogProfileReferenceArtifactLoader(MdaReferenceArtifactSetLoader setLoader)
    {
        _setLoader = setLoader ?? throw new ArgumentNullException(nameof(setLoader));
    }

    public ArtifactSet<ApplicationSelectionDialogProfileKey, ApplicationSelectionDialogProfile> Load(JsonSchema schema)
    {
        return _setLoader.Load<ApplicationSelectionDialogProfileKey, ApplicationSelectionDialogProfile>(
            MdaContractsArtifactPaths.ApplicationSelectionDialogProfilesDirectoryPath,
            schema,
            GetKey,
            key => $"Duplicate ApplicationSelectionDialogProfile key (profileId='{key.ProfileId}', profileVersion='{key.ProfileVersion}').");
    }

    private static ApplicationSelectionDialogProfileKey GetKey(ApplicationSelectionDialogProfile model)
    {
        var preflight = ApplicationSelectionDialogProfilePreflight.Validate(model);
        if (!preflight.Ok)
        {
            throw new InvalidOperationException(
                $"ApplicationSelectionDialogProfile preflight failed ({model.ProfileId}/{model.ProfileVersion}): {PreflightIssueFormatter.Format(preflight.Errors)}");
        }

        return MdaArtifactKeyFactory.CreateApplicationSelectionDialogProfileKey(model.ProfileId, model.ProfileVersion);
    }
}
