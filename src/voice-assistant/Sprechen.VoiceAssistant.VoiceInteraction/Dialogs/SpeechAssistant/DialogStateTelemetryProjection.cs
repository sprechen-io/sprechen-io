namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt die Prüfung bekannter Telemetriezustände und die Zuordnung von
/// Mikrozuständen für die Dialogaufzeichnung.
/// </summary>
internal static class DialogStateTelemetryProjection
{
    internal static bool IsKnownTelemetryState(string telemetryState)
    {
        return DialogTelemetryStateCatalog.Contains(telemetryState);
    }

    internal static bool TryProjectMicroStateToTelemetryState(string? microState, out string telemetryState)
    {
        return DialogMicroStateToTelemetryStateProjector.TryProject(microState, out telemetryState);
    }
}
