using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft doppelte Kombinationen aus Kommandoabsicht und Vergleichsmodus
/// sowie die Übersetzungen der vorhandenen Auslöseformulierungen.
/// </summary>
internal sealed class ApplicationSelectionIntentPreflightValidator
{
    private readonly CatalogLocalizationPreflightValidator _localizationValidator = new();

    public void Validate(
        List<PreflightIssue> errors,
        List<PreflightIssue> localizationIssues,
        ApplicationSelectionDialog model,
        string defaultLanguage,
        string localizationCode)
    {
        var intentKeys = new HashSet<string>(StringComparer.Ordinal);
        for (var intentIndex = 0; intentIndex < model.Intents.Count; intentIndex++)
        {
            var intent = model.Intents[intentIndex];
            if (!intentKeys.Add(CreateDeduplicationKey(intent)))
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorIdDuplicate,
                    Message: $"Duplicate selection-dialog intentId/matchMode pair '{intent.IntentId}'/'{intent.MatchMode}'.",
                    Path: $"$.intents[{intentIndex}]",
                    Id: intent.IntentId));
            }

            ValidateUtteranceLocalizations(localizationIssues, model, intent, intentIndex, defaultLanguage, localizationCode);
        }
    }

    private void ValidateUtteranceLocalizations(
        List<PreflightIssue> localizationIssues,
        ApplicationSelectionDialog model,
        CatalogVoiceIntent intent,
        int intentIndex,
        string defaultLanguage,
        string localizationCode)
    {
        for (var utteranceIndex = 0; utteranceIndex < intent.Utterances.Count; utteranceIndex++)
        {
            _localizationValidator.Validate(
                localizationIssues,
                model.SupportedLanguages,
                defaultLanguage,
                model.StrictLocalization,
                intent.Utterances[utteranceIndex],
                $"$.intents[{intentIndex}].utterances[{utteranceIndex}]",
                intent.IntentId,
                localizationCode);
        }
    }

    private static string CreateDeduplicationKey(CatalogVoiceIntent intent)
    {
        return $"{intent.IntentId}\u001F{intent.MatchMode}";
    }
}
