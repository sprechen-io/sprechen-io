using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft die Sprachprofile eigenständiger Lexikonkataloge vor deren Verwendung.
/// </summary>
public static class VoiceLexiconCatalogPreflight
{
    /// <summary>
    /// Prüft die Sprachprofilzuordnung eines eigenständigen Lexikonkatalogs.
    /// </summary>
    public static PreflightResult Validate(VoiceLexiconCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return new VoiceLexiconCatalogPreflightValidator().Validate(catalog);
    }
}
