using Sprechen.Mda.Contracts.V1.Fields;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Koordiniert Identitäts-, Funktionsumfangs- und Übersetzungsprüfungen
/// eines wiederverwendbaren Feldkatalogs.
/// </summary>
internal sealed class FieldDefinitionsPreflightValidator
{
    private readonly FieldDefinitionsIdentityValidator _identityValidator = new();
    private readonly ReusableFieldSupportedSubsetValidator _supportedSubsetValidator = new();
    private readonly ReusableFieldLocalizationCoverageValidator _localizationCoverageValidator = new();

    public PreflightResult Validate(FieldDefinitions catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var errors = new List<PreflightIssue>();
        var warnings = new List<PreflightIssue>();

        _identityValidator.Validate(catalog, errors);
        _supportedSubsetValidator.Validate(catalog, errors);
        _localizationCoverageValidator.Validate(catalog, errors, warnings);

        return new PreflightResult(errors, warnings);
    }
}
