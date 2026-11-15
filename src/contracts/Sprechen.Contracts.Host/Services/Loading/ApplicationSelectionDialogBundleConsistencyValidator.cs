using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Preflight;

namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Prüft den Katalog mit seinem Auswahldialog und den referenzierten Profilen, damit fehlende oder widersprüchliche Bestandteile vor der Nutzung auffallen.
/// </summary>
internal sealed class ApplicationSelectionDialogBundleConsistencyValidator
{
    private readonly ApplicationSelectionDialogProfileReferenceCollector _profileReferenceCollector;

    public ApplicationSelectionDialogBundleConsistencyValidator()
        : this(new ApplicationSelectionDialogProfileReferenceCollector())
    {
    }

    internal ApplicationSelectionDialogBundleConsistencyValidator(ApplicationSelectionDialogProfileReferenceCollector profileReferenceCollector)
    {
        _profileReferenceCollector = profileReferenceCollector ?? throw new ArgumentNullException(nameof(profileReferenceCollector));
    }

    public ApplicationSelectionDialogBundleConsistencyResult Validate(
        FormsCatalogDocument catalogDocument,
        MdaReferenceArtifacts references)
    {
        ArgumentNullException.ThrowIfNull(catalogDocument);
        ArgumentNullException.ThrowIfNull(references);

        var selectionDialogKey = MdaArtifactKeyFactory.CreateSelectionDialogKey(
            catalogDocument.SelectionDialog.SelectionDialogId,
            catalogDocument.SelectionDialog.ModelVersion);
        if (!references.SelectionDialogs.Models.TryGetValue(selectionDialogKey, out var selectionDialog))
        {
            throw new InvalidOperationException(
                $"FormsCatalog references missing ApplicationSelectionDialog (selectionDialogId='{selectionDialogKey.SelectionDialogId}', modelVersion='{selectionDialogKey.ModelVersion}').");
        }

        var referencedProfileKeys = _profileReferenceCollector.Collect(catalogDocument);
        var loadedProfiles = LoadProfiles(referencedProfileKeys, references.SelectionDialogProfiles.Models);
        ValidateFormsCatalogDocument(catalogDocument, selectionDialog, loadedProfiles);
        ValidateResolvedFormsCatalog(catalogDocument, selectionDialog, loadedProfiles);

        return new ApplicationSelectionDialogBundleConsistencyResult(selectionDialogKey, referencedProfileKeys);
    }

    private static Dictionary<ApplicationSelectionDialogProfileKey, ApplicationSelectionDialogProfile> LoadProfiles(
        IReadOnlySet<ApplicationSelectionDialogProfileKey> referencedProfileKeys,
        IReadOnlyDictionary<ApplicationSelectionDialogProfileKey, ApplicationSelectionDialogProfile> profilesByKey)
    {
        var loadedProfiles = new Dictionary<ApplicationSelectionDialogProfileKey, ApplicationSelectionDialogProfile>(referencedProfileKeys.Count);
        foreach (var key in referencedProfileKeys)
        {
            if (!profilesByKey.TryGetValue(key, out var profile))
            {
                throw new InvalidOperationException(
                    $"FormsCatalog references missing ApplicationSelectionDialogProfile (profileId='{key.ProfileId}', profileVersion='{key.ProfileVersion}').");
            }

            loadedProfiles.Add(key, profile);
        }

        return loadedProfiles;
    }

    private static void ValidateFormsCatalogDocument(
        FormsCatalogDocument catalogDocument,
        ApplicationSelectionDialog selectionDialog,
        IReadOnlyDictionary<ApplicationSelectionDialogProfileKey, ApplicationSelectionDialogProfile> loadedProfiles)
    {
        var preflight = FormsCatalogDocumentPreflight.Validate(catalogDocument, selectionDialog, loadedProfiles);
        if (!preflight.Ok)
        {
            throw new InvalidOperationException(
                $"FormsCatalog document preflight failed: {PreflightIssueFormatter.Format(preflight.Errors)}");
        }
    }

    private static void ValidateResolvedFormsCatalog(
        FormsCatalogDocument catalogDocument,
        ApplicationSelectionDialog selectionDialog,
        IReadOnlyDictionary<ApplicationSelectionDialogProfileKey, ApplicationSelectionDialogProfile> loadedProfiles)
    {
        var resolvedFormsCatalog = FormsCatalogResolver.Resolve(catalogDocument, selectionDialog, loadedProfiles);
        var preflight = FormsCatalogPreflight.Validate(resolvedFormsCatalog);
        if (!preflight.Ok)
        {
            throw new InvalidOperationException($"FormsCatalog preflight failed: {PreflightIssueFormatter.Format(preflight.Errors)}");
        }
    }
}
