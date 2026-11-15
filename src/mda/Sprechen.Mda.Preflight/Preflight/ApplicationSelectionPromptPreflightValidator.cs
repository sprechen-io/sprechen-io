using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft doppelte Ansagekennungen, erforderliche Ansagen und die Auflösung
/// ihrer Übersetzungen für die unterstützten Sprachen.
/// </summary>
internal sealed class ApplicationSelectionPromptPreflightValidator
{
    private readonly CatalogLocalizationPreflightValidator _localizationValidator = new();

    public void Validate(
        List<PreflightIssue> errors,
        List<PreflightIssue> localizationIssues,
        ApplicationSelectionDialog model,
        string defaultLanguage,
        string localizationCode)
    {
        var promptIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var exactPromptIds = new HashSet<string>(StringComparer.Ordinal);
        for (var promptIndex = 0; promptIndex < model.Prompts.Count; promptIndex++)
        {
            var prompt = model.Prompts[promptIndex];
            exactPromptIds.Add(prompt.PromptId);
            if (!promptIds.Add(prompt.PromptId))
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorIdDuplicate,
                    Message: $"Duplicate selection-dialog promptId '{prompt.PromptId}'.",
                    Path: $"$.prompts[{promptIndex}].promptId",
                    Id: prompt.PromptId));
            }

            _localizationValidator.Validate(
                localizationIssues,
                model.SupportedLanguages,
                defaultLanguage,
                model.StrictLocalization,
                prompt.Text,
                $"$.prompts[{promptIndex}].text",
                prompt.PromptId,
                localizationCode);
        }

        foreach (var requiredPromptId in ApplicationSelectionRequiredPromptId.All)
        {
            if (!exactPromptIds.Contains(requiredPromptId))
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorRefMissing,
                    Message: $"Required selection-dialog promptId '{requiredPromptId}' is missing.",
                    Path: "$.prompts",
                    Id: requiredPromptId));
            }
        }
    }
}
