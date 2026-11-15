namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Ordnet interne Fehlercodes der Sprachoberfläche einer Studienereignisart zu.
/// Nicht aufgeführte Codes werden als Validierungsfehler behandelt. Darüber werden
/// insbesondere die Regelcodes des Formularvertrags erfasst.
/// </summary>
public static class TelemetryStudyErrorRouting
{
    /// <summary>
    /// Zuordnung interner Fehlercodes zu Ereignisarten entsprechend <c>StudyErrorRouting</c> im Modell.
    /// </summary>
    public static IReadOnlyDictionary<string, TelemetryStudyErrorRoute> ByErrorCode { get; } =
        new Dictionary<string, TelemetryStudyErrorRoute>(StringComparer.Ordinal)
        {
            ["no_speech_detected"] = TelemetryStudyErrorRoute.Silence,
            ["no_input"] = TelemetryStudyErrorRoute.Silence,
            ["capture_interrupted"] = TelemetryStudyErrorRoute.Recognition,
            ["recognized_as_empty"] = TelemetryStudyErrorRoute.Recognition,
            ["low_stt_confidence"] = TelemetryStudyErrorRoute.Recognition,
            ["stt_error"] = TelemetryStudyErrorRoute.Recognition,
            ["unrecognized_input"] = TelemetryStudyErrorRoute.None,
            ["unknown_command"] = TelemetryStudyErrorRoute.None,
            ["keyboard_mode_active"] = TelemetryStudyErrorRoute.Validation,
            ["voice_input_forbidden"] = TelemetryStudyErrorRoute.Validation,
            ["spoken_value_unavailable"] = TelemetryStudyErrorRoute.Validation,
            ["manual_correction"] = TelemetryStudyErrorRoute.None,
            ["switch_to_spelling"] = TelemetryStudyErrorRoute.None,
        };

    /// <summary>
    /// Liefert die zugeordnete Ereignisart oder <see cref="TelemetryStudyErrorRoute.Validation"/>
    /// für einen Code ohne Eintrag.
    /// </summary>
    public static TelemetryStudyErrorRoute Route(string errorCode)
    {
        return ByErrorCode.TryGetValue(errorCode, out var route)
            ? route
            : TelemetryStudyErrorRoute.Validation;
    }
}
