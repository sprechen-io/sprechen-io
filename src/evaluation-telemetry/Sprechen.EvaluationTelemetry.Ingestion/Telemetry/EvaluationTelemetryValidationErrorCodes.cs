namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Kennzeichnet Ablehnungen durch die ergänzende Prüfung kanonischer Übergänge.
/// </summary>
internal static class EvaluationTelemetryValidationErrorCodes
{
    public const string None = "telemetry.validation.none";

    /// <summary>
    /// Die aufgezeichneten Übergänge sind keine geordnete Teilmenge
    /// der aus den Ereignisdaten abgeleiteten Übergänge.
    /// </summary>
    public const string UnprojectedCanonicalTransitions = "telemetry.canonical_transitions.unprojected";
}
