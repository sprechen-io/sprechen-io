using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Wählt Spur- und Logeinträge nach der Phase eines Wake-Word-Ereignisses aus,
/// damit Diagnose und Wiederherstellung einheitlich beschrieben werden.
/// </summary>
internal sealed class WakeWordMissTelemetryActionFactory
{
    private readonly WakeWordTelemetryValueFormatter _valueFormatter;
    private readonly WakeWordActivationFormCodec _activationForms = new();
    private readonly SpeechAssistantTraceScope _wakeWordEffectScope;
    private readonly SpeechAssistantTraceScope _postWakeRecoveryScope;

    public WakeWordMissTelemetryActionFactory(
        WakeWordTelemetryValueFormatter valueFormatter,
        SpeechAssistantTraceScope wakeWordEffectScope,
        SpeechAssistantTraceScope postWakeRecoveryScope)
    {
        _valueFormatter = valueFormatter ?? throw new ArgumentNullException(nameof(valueFormatter));
        _wakeWordEffectScope = wakeWordEffectScope;
        _postWakeRecoveryScope = postWakeRecoveryScope;
    }

    public WakeWordMissTelemetryAction Create(WakeWordMissEvent telemetryEvent)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);

        return telemetryEvent.Phase switch
        {
            WakeWordMissPhase.MissTrace => new(CreateMissTraceRecord(telemetryEvent), null),
            WakeWordMissPhase.MissDiagnostic => new(CreateMissDiagnosticTraceRecord(telemetryEvent), null),
            WakeWordMissPhase.EarconFailed => new(
                CreateWakeWordEffectWarningTraceRecord("Wake-word miss earcon playback failed; continuing listener loop."),
                new WakeWordTelemetryLogRecord(telemetryEvent.Exception, "Wake-word miss earcon playback failed.")),
            WakeWordMissPhase.RecordingCleanupFailed => new(
                CreateWakeWordEffectWarningTraceRecord("Wake-word detection could not clear the recording state before transition."),
                new WakeWordTelemetryLogRecord(telemetryEvent.Exception, "Failed to clear recording state before wake word transition.")),
            WakeWordMissPhase.GuidanceSpoken => new(
                new WakeWordTelemetryTraceRecord(
                    _wakeWordEffectScope,
                    SpeechAssistantTraceLevel.Information,
                    "Spoke the wake-recovery guidance instead of the miss earcon (pre-first-wake recovery)."),
                null),
            _ => WakeWordMissTelemetryAction.None,
        };
    }

    private WakeWordTelemetryTraceRecord CreateMissTraceRecord(WakeWordMissEvent telemetryEvent)
    {
        return telemetryEvent.Status == WakeWordActivationStatus.NoRecognizedSpeech
            ? new(
                _wakeWordEffectScope,
                SpeechAssistantTraceLevel.Information,
                "Wake-word transcription returned no recognized speech.")
            : CreateWakeWordEffectWarningTraceRecord(
                $"Wake-word transcription did not match the modeled inventory. confidence={_valueFormatter.FormatConfidence(telemetryEvent.ConfidencePercent)}");
    }

    private WakeWordTelemetryTraceRecord CreateMissDiagnosticTraceRecord(WakeWordMissEvent telemetryEvent)
    {
        var diagnostic = telemetryEvent.Diagnostic;
        return new(
            _postWakeRecoveryScope,
            SpeechAssistantTraceLevel.Information,
            $"wake miss status={telemetryEvent.Status} transcriptLen={telemetryEvent.TranscriptLength ?? 0} confidence={_valueFormatter.FormatConfidence(telemetryEvent.ConfidencePercent)} activationFormCount={telemetryEvent.ActivationFormCount ?? 0} recognitionOnlyActivationFormCount={telemetryEvent.RecognitionOnlyActivationFormCount ?? 0} tokenCount={diagnostic?.TokenCount ?? 0} activationForms=[{FormatForms(diagnostic?.ActivationForms)}] recognitionOnlyActivationForms=[{FormatForms(diagnostic?.RecognitionOnlyActivationForms)}]");
    }

    private string FormatForms(IReadOnlyList<string>? forms)
        => forms is null || forms.Count == 0
            ? string.Empty
            : _activationForms.Format(forms);

    private WakeWordTelemetryTraceRecord CreateWakeWordEffectWarningTraceRecord(string message)
        => new(_wakeWordEffectScope, SpeechAssistantTraceLevel.Warning, message);
}
