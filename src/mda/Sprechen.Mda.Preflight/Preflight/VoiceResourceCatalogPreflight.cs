using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Vorprüfung eigenständiger Textressourcenkataloge der Sprachoberfläche.
/// </summary>
public static class VoiceResourceCatalogPreflight
{
    /// <summary>
    /// Prüft Ressourcenkennungen eines eigenständigen Textkatalogs.
    /// </summary>
    public static PreflightResult Validate(VoiceResourceCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return new VoiceResourceCatalogPreflightValidator().Validate(catalog);
    }
}
