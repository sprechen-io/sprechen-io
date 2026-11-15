using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Formularreferenzen im Katalog auf doppelte Kennungen, Versionskonsistenz,
/// vorhandene Voraussetzungen und übersetzte Namen und Beschreibungen.
/// </summary>
internal sealed class CatalogFormPreflightValidator
{
    private readonly CatalogLocalizationPreflightValidator _localizationValidator = new();

    public void Validate(List<PreflightIssue> errors, ApplicationDescriptor app, int appIndex)
    {
        var formIds = new HashSet<string>(StringComparer.Ordinal);
        var formsById = new HashSet<string>(app.Forms.Select(FormatFormId), StringComparer.Ordinal);

        for (var formIndex = 0; formIndex < app.Forms.Count; formIndex++)
        {
            var form = app.Forms[formIndex];

            ValidateFormIdentity(errors, formIds, app, form, appIndex, formIndex);
            ValidatePrerequisiteReferences(errors, formsById, app, form, appIndex, formIndex);
            ValidateFormLocalization(errors, app, form, appIndex, formIndex);
        }
    }

    private static void ValidateFormIdentity(
        List<PreflightIssue> errors,
        HashSet<string> formIds,
        ApplicationDescriptor app,
        FormDescriptor form,
        int appIndex,
        int formIndex)
    {
        var formId = FormatFormId(form);

        if (!formIds.Add(formId))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorIdDuplicate,
                Message: $"Duplicate formId '{formId}' in application '{app.ApplicationId}'.",
                Path: $"$.applications[{appIndex}].forms[{formIndex}].formId",
                Id: formId));
        }

        if (!form.AvailableContractVersions.Contains(form.LatestContractVersion, StringComparer.Ordinal))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorVersionInvariantBroken,
                Message: $"latestContractVersion '{form.LatestContractVersion}' must be contained in availableContractVersions for form '{app.ApplicationId}.{formId}'.",
                Path: $"$.applications[{appIndex}].forms[{formIndex}].latestContractVersion",
                Id: formId));
        }
    }

    private static void ValidatePrerequisiteReferences(
        List<PreflightIssue> errors,
        HashSet<string> formsById,
        ApplicationDescriptor app,
        FormDescriptor form,
        int appIndex,
        int formIndex)
    {
        if (form.PrerequisiteForms is null)
        {
            return;
        }

        for (var prereqIndex = 0; prereqIndex < form.PrerequisiteForms.Count; prereqIndex++)
        {
            var prereqFormId = form.PrerequisiteForms[prereqIndex];

            if (!formsById.Contains(prereqFormId))
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorRefMissing,
                    Message: $"prerequisiteForms references missing formId '{prereqFormId}' in application '{app.ApplicationId}'.",
                    Path: $"$.applications[{appIndex}].forms[{formIndex}].prerequisiteForms[{prereqIndex}]",
                    Id: prereqFormId));
            }
        }
    }

    private void ValidateFormLocalization(
        List<PreflightIssue> errors,
        ApplicationDescriptor app,
        FormDescriptor form,
        int appIndex,
        int formIndex)
    {
        var formId = FormatFormId(form);

        _localizationValidator.Validate(
            errors,
            app.SupportedLanguages,
            form.DisplayName,
            $"$.applications[{appIndex}].forms[{formIndex}].displayName",
            formId);

        _localizationValidator.Validate(
            errors,
            app.SupportedLanguages,
            form.Description,
            $"$.applications[{appIndex}].forms[{formIndex}].description",
            formId);
    }

    private static string FormatFormId(FormDescriptor form)
        => form.FormId == default ? string.Empty : form.FormId.Value;
}
