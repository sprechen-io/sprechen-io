using Sprechen.Mda.Contracts.V1.Fields;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Ermöglicht die eigenständige Vorprüfung wiederverwendbarer Feldkataloge, damit Identität,
/// unterstützter Funktionsumfang und Lokalisierung gemeinsam geprüft werden.
/// </summary>
public static class FieldDefinitionsPreflight
{
    /// <summary>
    /// Prüft einen eigenständigen wiederverwendbaren Feldkatalog.
    /// </summary>
    public static PreflightResult Validate(FieldDefinitions catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return new FieldDefinitionsPreflightValidator().Validate(catalog);
    }
}
