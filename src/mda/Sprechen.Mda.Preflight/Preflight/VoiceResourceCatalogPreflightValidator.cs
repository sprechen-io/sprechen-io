using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft einen eigenständigen Textressourcenkatalog auf doppelte Ressourcenkennungen.
/// </summary>
internal sealed class VoiceResourceCatalogPreflightValidator
{
    private readonly VoiceResourceCatalogResourceValidator _resourceValidator = new();

    public PreflightResult Validate(VoiceResourceCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var errors = new List<PreflightIssue>();
        _resourceValidator.Validate(
            errors,
            [],
            strictLocalization: false,
            catalog.Resources,
            "$.resources");

        return new PreflightResult(errors, []);
    }
}
