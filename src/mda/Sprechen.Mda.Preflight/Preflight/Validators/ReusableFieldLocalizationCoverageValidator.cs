using Sprechen.Mda.Contracts.V1.Fields;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Texte wiederverwendbarer Felder für alle unterstützten Sprachen
/// und ordnet Befunde je nach StrictLocalization als Fehler oder Warnung ein.
/// </summary>
internal sealed class ReusableFieldLocalizationCoverageValidator
{
    public void Validate(FieldDefinitions catalog, List<PreflightIssue> errors, List<PreflightIssue> warnings)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(errors);
        ArgumentNullException.ThrowIfNull(warnings);

        var localizationCode = catalog.StrictLocalization
            ? PreflightCodes.ErrorLocalizationMissing
            : PreflightCodes.WarnLocalizationPartial;
        var localizationIssues = catalog.StrictLocalization ? errors : warnings;

        foreach (var language in catalog.SupportedLanguages)
        {
            ValidateDefinitions(catalog.Definitions, language, localizationCode, localizationIssues);
        }
    }

    private static void ValidateDefinitions(
        IReadOnlyList<ReusableFieldDefinition> definitions,
        string language,
        string localizationCode,
        List<PreflightIssue> localizationIssues)
    {
        for (var i = 0; i < definitions.Count; i++)
        {
            ReusableFieldDefinitionLocalizationCoverageValidator.Validate(
                definitions[i],
                i,
                language,
                localizationCode,
                localizationIssues);
        }
    }
}
