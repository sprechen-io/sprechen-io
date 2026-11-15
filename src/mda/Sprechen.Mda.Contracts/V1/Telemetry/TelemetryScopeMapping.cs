using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Zuordnung vom <c>ConstraintScope</c> des Vertrags in camelCase
/// zum Telemetrie-Geltungsbereich in snake_case. Sie entspricht der Zuordnung
/// im Telemetrie-Fragmentschema.
/// </summary>
public static class TelemetryScopeMapping
{
    /// <summary>
    /// Zuordnung der Vertrags-Geltungsbereiche zu ihren Telemetriebezeichnungen.
    /// </summary>
    public static IReadOnlyDictionary<string, string> ContractToLoggingScope { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ConstraintScopes.Field] = TelemetryScopes.Field,
            [ConstraintScopes.FieldGroup] = TelemetryScopes.FieldGroup,
            [ConstraintScopes.CrossGroup] = TelemetryScopes.CrossGroup,
            [ConstraintScopes.Form] = TelemetryScopes.Form,
        };

    /// <summary>
    /// Ordnet Vertrags- und Telemetriebezeichnungen unabhängig von Groß- und Kleinschreibung
    /// dem Telemetrie-Geltungsbereich zu. Führende und nachgestellte Leerzeichen werden entfernt.
    /// Unbekannte Werte werden als <see cref="TelemetryScopes.Field"/> behandelt.
    /// Null, leere Werte und reine Leerzeichen werden abgewiesen.
    /// </summary>
    public static string MapToLoggingScope(string scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);

        var normalized = scope.Trim().ToLowerInvariant();
        foreach (var (contractScope, loggingScope) in ContractToLoggingScope)
        {
            if (normalized == contractScope.ToLowerInvariant() || normalized == loggingScope)
            {
                return loggingScope;
            }
        }

        return TelemetryScopes.Field;
    }
}
