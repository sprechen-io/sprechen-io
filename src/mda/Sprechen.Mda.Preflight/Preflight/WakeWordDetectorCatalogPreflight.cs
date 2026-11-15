using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Vorprüfung des Katalogs akustischer Aktivierungswortdetektoren.
/// </summary>
public static class WakeWordDetectorCatalogPreflight
{
    /// <summary>
    /// Prüft Kennungen und Parameter der im Katalog enthaltenen Detektorprofile.
    /// </summary>
    public static PreflightResult Validate(WakeWordDetectorCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return new WakeWordDetectorCatalogPreflightValidator().Validate(catalog);
    }
}
