using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft, ob eine angegebene Standardsprache zu den unterstützten Sprachen
/// der Anwendungsauswahl gehört. Fehlende oder leere Angaben bleiben hier ungeprüft.
/// </summary>
internal sealed class ApplicationSelectionDefaultLanguageValidator
{
    public void Validate(List<PreflightIssue> errors, ApplicationSelectionDialog model)
    {
        if (string.IsNullOrWhiteSpace(model.DefaultLanguage)
            || model.SupportedLanguages.Contains(model.DefaultLanguage, StringComparer.Ordinal))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorSettingsInvalid,
            Message: $"defaultLanguage '{model.DefaultLanguage}' must be contained in supportedLanguages.",
            Path: "$.defaultLanguage",
            Id: model.SelectionDialogId));
    }
}
