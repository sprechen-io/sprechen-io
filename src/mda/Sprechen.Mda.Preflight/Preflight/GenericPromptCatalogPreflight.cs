using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Vorprüfung eines eigenständigen allgemeinen Ansagenkatalogs.
/// </summary>
public static class GenericPromptCatalogPreflight
{
    /// <summary>
    /// Prüft, ob der Ansagenkatalog mindestens eine Sprachzuordnung enthält.
    /// </summary>
    public static PreflightResult Validate(GenericPromptCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return new GenericPromptCatalogPreflightValidator().Validate(catalog);
    }
}
