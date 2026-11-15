using Json.Schema;
using Sprechen.Mda.Contracts.V1.FormVoice;
using Sprechen.Mda.Contracts.V1.Preflight;

namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Lädt und prüft Dialogprofile von Formularen, damit ungültige oder mehrfach vorhandene Kombinationen aus Kennung und Version auffallen.
/// </summary>
internal sealed class FormVoiceProfileReferenceArtifactLoader
{
    private readonly MdaReferenceArtifactSetLoader _setLoader;

    public FormVoiceProfileReferenceArtifactLoader(MdaReferenceArtifactSetLoader setLoader)
    {
        _setLoader = setLoader ?? throw new ArgumentNullException(nameof(setLoader));
    }

    public ArtifactSet<FormVoiceProfileKey, FormVoiceProfile> Load(JsonSchema schema)
    {
        return _setLoader.LoadCategory<FormVoiceProfileKey, FormVoiceProfile>(
            MdaContractsArtifactPaths.FormVoiceProfilesDirectoryPath,
            MdaContractsArtifactPaths.FormVoiceProfilesCategoryName,
            schema,
            GetKey,
            key => $"Duplicate FormVoiceProfile key (formVoiceProfileId='{key.FormVoiceProfileId}', profileVersion='{key.ProfileVersion}').",
            allowEmpty: true);
    }

    private static FormVoiceProfileKey GetKey(FormVoiceProfile model)
    {
        var preflight = FormVoiceProfilePreflight.Validate(model);
        if (!preflight.Ok)
        {
            throw new InvalidOperationException(
                $"FormVoiceProfile preflight failed ({model.FormVoiceProfileId}/{model.ProfileVersion}): {PreflightIssueFormatter.Format(preflight.Errors)}");
        }

        return MdaArtifactKeyFactory.CreateFormVoiceProfileKey(model.FormVoiceProfileId, model.ProfileVersion);
    }
}
