using System.Globalization;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft doppelte Zahlwerte und die Übersetzungen vorhandener Zahlwortformulierungen.
/// </summary>
internal sealed class ApplicationSelectionNumberWordPreflightValidator
{
    private readonly CatalogLocalizationPreflightValidator _localizationValidator = new();

    public void Validate(
        List<PreflightIssue> errors,
        List<PreflightIssue> localizationIssues,
        ApplicationSelectionDialog model,
        string defaultLanguage,
        string localizationCode)
    {
        var numberWordValues = new HashSet<int>();
        for (var numberWordIndex = 0; numberWordIndex < model.NumberWords.Count; numberWordIndex++)
        {
            var numberWord = model.NumberWords[numberWordIndex];
            var numberWordId = numberWord.Value.ToString(CultureInfo.InvariantCulture);
            if (!numberWordValues.Add(numberWord.Value))
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorIdDuplicate,
                    Message: $"Duplicate selection-dialog numberWords value '{numberWord.Value}'.",
                    Path: $"$.numberWords[{numberWordIndex}].value",
                    Id: numberWordId));
            }

            ValidateUtteranceLocalizations(localizationIssues, model, numberWord, numberWordIndex, numberWordId, defaultLanguage, localizationCode);
        }
    }

    private void ValidateUtteranceLocalizations(
        List<PreflightIssue> localizationIssues,
        ApplicationSelectionDialog model,
        CatalogVoiceNumberWord numberWord,
        int numberWordIndex,
        string numberWordId,
        string defaultLanguage,
        string localizationCode)
    {
        for (var utteranceIndex = 0; utteranceIndex < numberWord.Utterances.Count; utteranceIndex++)
        {
            _localizationValidator.Validate(
                localizationIssues,
                model.SupportedLanguages,
                defaultLanguage,
                model.StrictLocalization,
                numberWord.Utterances[utteranceIndex],
                $"$.numberWords[{numberWordIndex}].utterances[{utteranceIndex}]",
                numberWordId,
                localizationCode);
        }
    }
}
