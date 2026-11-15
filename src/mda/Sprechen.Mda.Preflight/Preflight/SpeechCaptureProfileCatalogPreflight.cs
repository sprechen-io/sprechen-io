using Sprechen.Mda.Contracts.V1.SpeechCapture;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Vorprüfung der Aufnahmeprofile auf Pflichtangaben,
/// eindeutige Moduskennungen und zulässige Parameterwerte.
/// </summary>
public static class SpeechCaptureProfileCatalogPreflight
{
    public static PreflightResult Validate(SpeechCaptureProfileCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return new SpeechCaptureProfileCatalogPreflightValidator().Validate(catalog);
    }
}
