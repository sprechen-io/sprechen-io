using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Ordnet gemeldete Fehler einer Telemetrieereignisart zu und übersetzt ihren Gültigkeitsbereich. Die übrigen Werte bleiben für die anschließende Modellprüfung unverändert.
/// </summary>
internal static class EvaluationTelemetryErrorClassifier
{
    internal static EvaluationTelemetryClassifiedError ClassifyConstraintViolation(
        string fieldId,
        string errorCode,
        string severity,
        string scope,
        string? fieldGroupId,
        string trigger)
    {
        return new EvaluationTelemetryClassifiedError(
            errorCode,
            severity,
            MapScope(scope),
            fieldId,
            fieldGroupId,
            TelemetryStudyErrorRouting.Route(errorCode),
            trigger);
    }

    /// <summary>
    /// Übersetzt den Vertragsbereich über die modellierte Zuordnung. Unbekannte
    /// Werte bleiben unverändert, damit die Modellprüfung sie ablehnen kann,
    /// statt einen nicht gemeldeten Bereich einzusetzen.
    /// </summary>
    internal static string MapScope(string scope)
    {
        return TelemetryScopeMapping.ContractToLoggingScope.TryGetValue(scope, out var loggingScope)
            ? loggingScope
            : scope;
    }
}
