using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft doppelte Sprachprofile und übereinstimmende Sprachmengen
/// der Ja-/Nein- und Anredeprofile.
/// </summary>
internal sealed class VoiceLexiconCatalogPreflightValidator
{
    private readonly VoiceLexiconProfileSetPreflightValidator _profileSetValidator = new();

    public PreflightResult Validate(VoiceLexiconCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var errors = new List<PreflightIssue>();
        _profileSetValidator.ValidateStandaloneCatalog(errors, catalog);

        return new PreflightResult(errors, []);
    }
}
