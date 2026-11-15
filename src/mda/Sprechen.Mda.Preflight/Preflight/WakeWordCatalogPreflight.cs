using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Vorprüfung eines eigenständigen Aktivierungswortkatalogs.
/// </summary>
public static class WakeWordCatalogPreflight
{
    /// <summary>
    /// Prüft Profile, Aktivierungsformen und Einstellungen des Katalogs.
    /// </summary>
    public static PreflightResult Validate(WakeWordCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return new WakeWordCatalogPreflightValidator().Validate(catalog);
    }
}
