using Sprechen.Mda.Contracts.V1.Telemetry;


namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet Fehlertelemetrie, Rückmeldung, begrenzte Ansagewiederholung
/// und zunehmende Hilfestellung bei Fehlern während der Feldeingabe.
/// </summary>
internal sealed class FormVoiceCommandFieldErrorHandlingService
{
    private readonly IEvaluationFormTelemetry _evaluationTelemetry;
    private readonly IFormVoiceCommandSpeechService _speechService;
    private readonly IFormVoiceCommandSpokenOutputReplayBuffer _replayBuffer;
    private readonly CaptureReplayPolicy _captureReplayPolicy;

    public FormVoiceCommandFieldErrorHandlingService(
        IEvaluationFormTelemetry evaluationTelemetry,
        IFormVoiceCommandSpeechService speechService,
        IFormVoiceCommandSpokenOutputReplayBuffer replayBuffer,
        CaptureReplayPolicy captureReplayPolicy)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
        _replayBuffer = replayBuffer ?? throw new ArgumentNullException(nameof(replayBuffer));
        _captureReplayPolicy = captureReplayPolicy ?? throw new ArgumentNullException(nameof(captureReplayPolicy));
    }

    public async Task<FormVoiceCommandFieldHandlingResult> HandleCaptureFailureAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        string expectedInputMode,
        string issueCode,
        bool autoReplayEligible,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedInputMode);
        ArgumentException.ThrowIfNullOrWhiteSpace(issueCode);

        turnContext.StateTracker.RecordNoInputTimeout();
        _evaluationTelemetry.TrackFieldAttempt(turnContext.Session, turnContext.Field.FieldId, expectedInputMode, success: false);
        // Die fehlende verwertbare Aufnahme als Erkennungsrückmeldung kennzeichnen,
        // nicht als Prüfung eines übergebenen Feldwerts.
        _evaluationTelemetry.TrackConstraintViolation(turnContext.Session, turnContext.Field.FieldId, issueCode, SeverityLevels.Warning, TelemetryErrorTriggers.SystemRecognition);
        _evaluationTelemetry.TrackRepairAttempt(turnContext.Session, turnContext.Field.FieldId, issueCode);

        // Bei freigegebenem Aufnahmeausgang und frischer Ansage aus dem Feldzug
        // deren Wiederholung anfordern, bevor die weitere automatische Hilfe folgt.
        await MaybeAutoReplayAnnouncementAsync(turnContext, autoReplayEligible, cancellationToken).ConfigureAwait(false);

        await turnContext.AutoHelp.RegisterFailureAsync(turnContext.Field.FieldId, cancellationToken).ConfigureAwait(false);
        return FormVoiceCommandFieldHandlingResult.Continue();
    }

    private async Task MaybeAutoReplayAnnouncementAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        bool autoReplayEligible,
        CancellationToken cancellationToken)
    {
        var decision = _captureReplayPolicy.Decide(autoReplayEligible, _replayBuffer.CurrentFieldTurnAnnouncement);
        if (!decision.ShouldReplay || string.IsNullOrWhiteSpace(decision.Announcement))
        {
            return;
        }

        // Die Wiederholungsmöglichkeit vor dem Sprachaufruf verbrauchen,
        // damit ein Ausgabefehler sie nicht für weitere Wiederholungen offen lässt.
        _replayBuffer.MarkCurrentFieldTurnAnnouncementReplayed();

        var leadIn = CaptureReplayLeadInPromptTextResolver.GetReplayLeadIn(turnContext.Context.IsoCode);
        var spokenReplay = $"{leadIn.Trim()} {decision.Announcement.Trim()}";

        await _speechService.SpeakReplayAsync(
                spokenReplay,
                turnContext.Context.IsoCode,
                turnContext.Context.VoiceSettings,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Fordert die Rückmeldung für einen Zug ohne verwertbaren Wert an.
    /// Der vom Aufrufer gelieferte Auslöser unterscheidet die Telemetrieeinordnung.
    /// </summary>
    public async Task<FormVoiceCommandFieldHandlingResult> HandlePromptedFailureAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        string issueCode,
        string prompt,
        string trigger,
        CancellationToken cancellationToken,
        bool trackFieldAttempt = false,
        string? expectedInputMode = null)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(issueCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        if (trackFieldAttempt)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(expectedInputMode);
            _evaluationTelemetry.TrackFieldAttempt(turnContext.Session, turnContext.Field.FieldId, expectedInputMode, success: false);
        }

        _evaluationTelemetry.TrackConstraintViolation(turnContext.Session, turnContext.Field.FieldId, issueCode, SeverityLevels.Warning, trigger);
        _evaluationTelemetry.TrackRepairAttempt(turnContext.Session, turnContext.Field.FieldId, issueCode);
        if (IsRecognitionFailure(issueCode))
        {
            turnContext.StateTracker.ArmStructuralTeachingReteach(turnContext.Field.FieldId);
        }

        turnContext.StateTracker.BeginErrorPrompt();
        await _speechService.SpeakSafeAsync(
                prompt,
                turnContext.Context.IsoCode,
                turnContext.Context.VoiceSettings,
                cancellationToken)
            .ConfigureAwait(false);
        await turnContext.AutoHelp.RegisterFailureAsync(turnContext.Field.FieldId, cancellationToken).ConfigureAwait(false);
        return FormVoiceCommandFieldHandlingResult.Continue();
    }

    /// <summary>
    /// Meldet pro Aufruf Feldversuch, Validierungsverstoß und Reparatursignal
    /// für eine vor der Werteingabe behandelte Modusablehnung. Der Aufruf selbst
    /// spielt keine Ansage ab und verhindert keine doppelte Meldung durch seinen Aufrufer.
    /// </summary>
    public Task TrackPreCaptureRejectionAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        string issueCode,
        string expectedInputMode)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(issueCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedInputMode);

        _evaluationTelemetry.TrackFieldAttempt(turnContext.Session, turnContext.Field.FieldId, expectedInputMode, success: false);
        // Die Modusablehnung mit dem dafür vorgesehenen Auslöser melden.
        _evaluationTelemetry.TrackConstraintViolation(turnContext.Session, turnContext.Field.FieldId, issueCode, SeverityLevels.Warning, TelemetryErrorTriggers.UserInput);
        _evaluationTelemetry.TrackRepairAttempt(turnContext.Session, turnContext.Field.FieldId, issueCode);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Fordert die Ablehnungsansage für einen nicht zur Spracheingabe freigegebenen
    /// Feldzug an. Ob anschließend eine Aufnahme geöffnet wird, entscheidet der Aufrufer.
    /// </summary>
    public async Task SpeakPreCaptureDenialAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        string prompt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        await _speechService.SpeakSafeAsync(
                prompt,
                turnContext.Context.IsoCode,
                turnContext.Context.VoiceSettings,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static bool IsRecognitionFailure(string issueCode)
        => string.Equals(issueCode, FormVoiceCommandFieldErrorCodes.LowSttConfidence, StringComparison.Ordinal)
            || string.Equals(issueCode, FormVoiceCommandFieldErrorCodes.UnrecognizedInput, StringComparison.Ordinal)
            || string.Equals(issueCode, FormVoiceCommandFieldErrorCodes.UnknownCommand, StringComparison.Ordinal);

    public async Task<FormVoiceCommandFieldHandlingResult> HandleManualCorrectionRequestedAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);

        _evaluationTelemetry.TrackRepairAttempt(
            turnContext.Session,
            turnContext.Field.FieldId,
            FormVoiceCommandFieldErrorCodes.ManualCorrection);
        turnContext.StateTracker.BeginFieldPromptRestart();
        await _speechService.SpeakSafeAsync(
                FormInputFeedbackPromptBuilder.GetCorrectionRequestedPrompt(
                    turnContext.Context.Contract,
                    turnContext.Context.IsoCode,
                    turnContext.Context.VoiceSettings.GetPolitenessLevel()),
                turnContext.Context.IsoCode,
                turnContext.Context.VoiceSettings,
                cancellationToken)
            .ConfigureAwait(false);

        return FormVoiceCommandFieldHandlingResult.Continue();
    }

    public async Task<FormVoiceCommandFieldHandlingResult> HandleValidationViolationAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        string repairCode,
        string violationPrompt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(repairCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(violationPrompt);

        _evaluationTelemetry.TrackRepairAttempt(turnContext.Session, turnContext.Field.FieldId, repairCode);
        turnContext.StateTracker.BeginErrorPrompt();
        await _speechService.SpeakSafeAsync(
                violationPrompt,
                turnContext.Context.IsoCode,
                turnContext.Context.VoiceSettings,
                cancellationToken)
            .ConfigureAwait(false);
        return FormVoiceCommandFieldHandlingResult.Continue();
    }
}
