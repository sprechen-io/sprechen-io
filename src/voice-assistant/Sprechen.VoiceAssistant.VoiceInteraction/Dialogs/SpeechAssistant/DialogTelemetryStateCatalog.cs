namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft, ob ein Zustandsname zu den bekannten Telemetriezuständen gehört.
/// Alle Aufrufer verwenden damit dieselbe Menge von Kennungen.
/// </summary>
internal static class DialogTelemetryStateCatalog
{
    private static readonly HashSet<string> KnownTelemetryStates = new(StringComparer.Ordinal)
    {
        DialogStates.Init,
        DialogStates.Prompting,
        DialogStates.Listening,
        DialogStates.Interpreting,
        DialogStates.Validating,
        DialogStates.Confirmation,
        DialogStates.ErrorPrompt,
        DialogStates.CancelConfirmation,
        DialogStates.Repair
    };

    internal static bool Contains(string telemetryState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(telemetryState);

        return KnownTelemetryStates.Contains(telemetryState.Trim());
    }
}
