using Json.Schema;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Preflight;

namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Lädt und prüft Auswahldialoge mit Kennung und Version, damit ungültige oder doppelte Dialogmodelle den Start abbrechen.
/// </summary>
internal sealed class ApplicationSelectionDialogReferenceArtifactLoader
{
    private readonly MdaReferenceArtifactSetLoader _setLoader;

    public ApplicationSelectionDialogReferenceArtifactLoader(MdaReferenceArtifactSetLoader setLoader)
    {
        _setLoader = setLoader ?? throw new ArgumentNullException(nameof(setLoader));
    }

    public ArtifactSet<SelectionDialogKey, ApplicationSelectionDialog> Load(JsonSchema schema)
    {
        return _setLoader.Load<SelectionDialogKey, ApplicationSelectionDialog>(
            MdaContractsArtifactPaths.ApplicationSelectionDialogsDirectoryPath,
            schema,
            GetKey,
            key => $"Duplicate ApplicationSelectionDialog key (selectionDialogId='{key.SelectionDialogId}', modelVersion='{key.ModelVersion}').");
    }

    private static SelectionDialogKey GetKey(ApplicationSelectionDialog model)
    {
        var preflight = ApplicationSelectionDialogPreflight.Validate(model);
        if (!preflight.Ok)
        {
            throw new InvalidOperationException(
                $"ApplicationSelectionDialog preflight failed ({model.SelectionDialogId}/{model.ModelVersion}): {PreflightIssueFormatter.Format(preflight.Errors)}");
        }

        return MdaArtifactKeyFactory.CreateSelectionDialogKey(model.SelectionDialogId, model.ModelVersion);
    }
}
