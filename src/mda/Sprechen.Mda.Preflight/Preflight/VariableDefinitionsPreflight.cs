using Sprechen.Mda.Contracts.V1.Variables;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Ermöglicht die eigenständige Vorprüfung wiederverwendbarer Variablenkataloge, damit Identität,
/// Werttypen, Datenschutz und Lokalisierung gemeinsam geprüft werden.
/// </summary>
public static class VariableDefinitionsPreflight
{
    /// <summary>
    /// Prüft einen eigenständigen wiederverwendbaren Variablenkatalog.
    /// </summary>
    public static PreflightResult Validate(VariableDefinitions catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return new VariableDefinitionsPreflightValidator().Validate(catalog);
    }
}
