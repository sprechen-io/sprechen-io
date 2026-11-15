namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert zuerst einen konfigurierten Bestätigungston an und meldet danach
/// die Aktivierung anhand von Text mit möglichem Folgebefehl an den Assistentenablauf.
/// </summary>
internal sealed class WakeWordLexicalActivationDispatcher
{
    public async ValueTask DispatchAsync(
        WakeRuntimeContext context,
        IWakeRuntimeOutcomeSink outcomeSink,
        IWakeWordTelemetrySink telemetry,
        string correlationId,
        WakeWordActivationEvaluation activation,
        WakeWordInventory wakeWordInventory)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(outcomeSink);
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentNullException.ThrowIfNull(activation);
        ArgumentNullException.ThrowIfNull(wakeWordInventory);

        // Den Bestätigungston vor Aufräumen und Telemetrie anfordern, um früh
        // auf die erkannte Aktivierung hinzuweisen. Tonfehler werden abgefangen
        // und gemeldet. Der weitere Ablauf hängt von den aufgerufenen Komponenten ab.
        if (wakeWordInventory.ActivatedEarconId is { } activatedEarcon)
        {
            try
            {
                await context.EarconPlayer
                    .PlayAsync(activatedEarcon, WakeRuntimeTerminalCancellation.ForRequiredTerminalWork())
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await telemetry
                    .RecordMissAsync(
                        new WakeWordMissEvent(
                            correlationId,
                            WakeWordMissPhase.RecordingCleanupFailed,
                            activation.Status,
                            ConfidencePercent: null,
                            TranscriptLength: null,
                            ActivationFormCount: null,
                            RecognitionOnlyActivationFormCount: null,
                            ex,
                            Diagnostic: null),
                        WakeRuntimeTerminalCancellation.ForRequiredTerminalWork())
                    .ConfigureAwait(false);
            }
        }

        try
        {
            await context.SpeechInteractionSignals.EnsureStoppedAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await telemetry
                .RecordMissAsync(
                    new WakeWordMissEvent(
                        correlationId,
                        WakeWordMissPhase.RecordingCleanupFailed,
                        activation.Status,
                        ConfidencePercent: null,
                        TranscriptLength: null,
                        ActivationFormCount: null,
                        RecognitionOnlyActivationFormCount: null,
                        ex,
                        Diagnostic: null),
                    WakeRuntimeTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);
        }

        var commandTextPresent = !string.IsNullOrWhiteSpace(activation.CommandText);
        await telemetry
            .RecordActivationAsync(
                new WakeWordActivationEvent(
                    correlationId,
                    WakeWordActivationPhase.LexicalDetected,
                    commandTextPresent,
                    TranscriptLength: 0,
                    CommandTextLength: 0,
                    ConfidencePercent: null),
                WakeRuntimeTerminalCancellation.ForRequiredTerminalWork())
            .ConfigureAwait(false);
        await telemetry
            .RecordActivationAsync(
                new WakeWordActivationEvent(
                    correlationId,
                    WakeWordActivationPhase.Diagnostic,
                    commandTextPresent,
                    (activation.TranscriptText ?? string.Empty).Length,
                    (activation.CommandText ?? string.Empty).Length,
                    activation.ConfidencePercent),
                WakeRuntimeTerminalCancellation.ForRequiredTerminalWork())
            .ConfigureAwait(false);
        await outcomeSink
            .PublishAsync(
                new WakeRuntimeLexicalDetectedOutcome(
                    activation.TranscriptText ?? string.Empty,
                    activation.CommandText,
                    correlationId,
                    activation.ConfidencePercent),
                WakeRuntimeTerminalCancellation.ForRequiredTerminalWork())
            .ConfigureAwait(false);
    }
}
