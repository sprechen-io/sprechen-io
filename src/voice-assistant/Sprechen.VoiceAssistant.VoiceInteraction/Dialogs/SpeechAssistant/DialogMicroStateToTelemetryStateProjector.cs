namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet bekannte Mikrozustände ihren Telemetriezuständen zu und meldet unbekannte Kennungen ohne erfundene Ersatzzuordnung zurück.
/// </summary>
internal static class DialogMicroStateToTelemetryStateProjector
{
    private static readonly IReadOnlyDictionary<string, string> TelemetryStatesByMicroState =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [DialogMicroStateIds.Prompting] = DialogStates.Prompting,
            [DialogMicroStateIds.Listening] = DialogStates.Listening,
            [DialogMicroStateIds.Interpreting] = DialogStates.Interpreting,
            [DialogMicroStateIds.Validating] = DialogStates.Validating,
            [DialogMicroStateIds.Confirmation] = DialogStates.Confirmation,
            [DialogMicroStateIds.ErrorPrompt] = DialogStates.ErrorPrompt,
            [DialogMicroStateIds.Repair] = DialogStates.Repair
        };

    internal static bool TryProject(string? microState, out string telemetryState)
    {
        if (microState is not null && TelemetryStatesByMicroState.TryGetValue(microState, out telemetryState!))
        {
            return true;
        }

        telemetryState = string.Empty;
        return false;
    }
}
