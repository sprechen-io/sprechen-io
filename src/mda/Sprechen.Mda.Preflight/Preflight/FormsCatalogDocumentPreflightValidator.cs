using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Auswahldialogkennung und -version sowie referenzierte Profilkennungen
/// und -versionen im Anwendungskatalog.
/// </summary>
internal sealed class FormsCatalogDocumentPreflightValidator
{
    public PreflightResult Validate(
        FormsCatalogDocument document,
        ApplicationSelectionDialog selectionDialog,
        IReadOnlyDictionary<ApplicationSelectionDialogProfileKey, ApplicationSelectionDialogProfile> profilesByKey)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(selectionDialog);
        ArgumentNullException.ThrowIfNull(profilesByKey);

        var errors = new List<PreflightIssue>();

        if (!string.Equals(document.SelectionDialog.SelectionDialogId, selectionDialog.SelectionDialogId, StringComparison.Ordinal))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorRefMissing,
                Message: $"FormsCatalog references selectionDialogId '{document.SelectionDialog.SelectionDialogId}', but loaded model is '{selectionDialog.SelectionDialogId}'.",
                Path: "$.selectionDialog.selectionDialogId",
                Id: document.SelectionDialog.SelectionDialogId));
        }

        if (!string.Equals(document.SelectionDialog.ModelVersion, selectionDialog.ModelVersion, StringComparison.Ordinal))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorRefMissing,
                Message: $"FormsCatalog references selection-dialog model version '{document.SelectionDialog.ModelVersion}', but loaded model is '{selectionDialog.ModelVersion}'.",
                Path: "$.selectionDialog.modelVersion",
                Id: document.SelectionDialog.ModelVersion));
        }

        ValidateProfileReference(errors, document.SelectionDialogProfile, "$.selectionDialogProfile", profilesByKey);

        for (var appIndex = 0; appIndex < document.Applications.Count; appIndex++)
        {
            ValidateProfileReference(
                errors,
                document.Applications[appIndex].SelectionDialogProfile,
                $"$.applications[{appIndex}].selectionDialogProfile",
                profilesByKey);
        }

        return new PreflightResult(errors, []);
    }

    private static void ValidateProfileReference(
        List<PreflightIssue> errors,
        ApplicationSelectionDialogProfileReference? reference,
        string path,
        IReadOnlyDictionary<ApplicationSelectionDialogProfileKey, ApplicationSelectionDialogProfile> profilesByKey)
    {
        if (reference is null)
        {
            return;
        }

        if (profilesByKey.ContainsKey(new ApplicationSelectionDialogProfileKey(reference.ProfileId, reference.ProfileVersion)))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorRefMissing,
            Message: $"FormsCatalog references missing selection-dialog profile '{reference.ProfileId}/{reference.ProfileVersion}'.",
            Path: $"{path}.profileId",
            Id: reference.ProfileId));
    }
}
