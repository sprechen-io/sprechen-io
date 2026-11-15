
namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet Feldorientierung, Aufnahmeorganisation und Wertanwendung und eröffnet dabei den feldbezogenen Bereich für spätere Ansagewiederholung.
/// </summary>
internal sealed class FormVoiceCommandFieldInteractionHandler : IFormVoiceCommandFieldInteractionHandler
{
    private readonly IEvaluationFormTelemetry _evaluationTelemetry;
    private readonly IFormVoiceCommandSpeechService _speechService;
    private readonly FormVoiceCommandFieldApplyService _fieldApplyService;
    private readonly FormVoiceCommandFieldTranscriptOrchestrator _transcriptOrchestrator;
    private readonly IFormVoiceCommandSpokenOutputReplayBuffer _replayBuffer;

    public FormVoiceCommandFieldInteractionHandler(
        IEvaluationFormTelemetry evaluationTelemetry,
        IFormVoiceCommandSpeechService speechService,
        FormVoiceCommandFieldTranscriptOrchestrator transcriptOrchestrator,
        FormVoiceCommandFieldApplyService fieldApplyService,
        IFormVoiceCommandSpokenOutputReplayBuffer replayBuffer)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
        _transcriptOrchestrator = transcriptOrchestrator ?? throw new ArgumentNullException(nameof(transcriptOrchestrator));
        _fieldApplyService = fieldApplyService ?? throw new ArgumentNullException(nameof(fieldApplyService));
        _replayBuffer = replayBuffer ?? throw new ArgumentNullException(nameof(replayBuffer));
    }

    public async Task<FormVoiceCommandFieldHandlingResult> HandleAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);

        var field = turnContext.Field;

        // Vor den Ansagen einen neuen Wiederholungsbereich für diesen Feldzug beginnen.
        _replayBuffer.BeginFieldTurn(field.FieldId);

        var announceReadOnlyFieldGroupContent = turnContext.StateTracker.ShouldAnnounceFieldGroupIntro(field.FieldGroupId);
        await MaybeAnnounceFieldGroupIntroAsync(turnContext, announceReadOnlyFieldGroupContent, cancellationToken).ConfigureAwait(false);

        var expectedInputMode = field.InputMode;

        turnContext.StateTracker.BeginFieldPrompt(field.FieldGroupId);
        _evaluationTelemetry.TrackFieldFocus(turnContext.Session, field.FieldId);

        // Die einmal gelesene grundlegende Statusprojektion für Telemetrie und
        // Feldorientierung verwenden, statt sie für den Ansagetext erneut abzufragen.
        var statusSnapshot = await FormVoiceCommandStatusSnapshotFactory
            .CreateAsync(turnContext.Context, turnContext.Interpreter)
            .ConfigureAwait(false);
        _evaluationTelemetry.TrackStatusSnapshot(turnContext.Session, statusSnapshot.ToEvaluationStatusSnapshot());
        _evaluationTelemetry.TrackFieldAndGroupStatus(
            turnContext.Session,
            (await turnContext.Interpreter.GetDetailedStatusSnapshotAsync().ConfigureAwait(false)).ToEvaluationDetailedStatus());

        // Die Feldorientierung aus Position und optionalem Hinweis aufbauen.
        // Index und Gesamtzahl stammen aus derselben grundlegenden Statusprojektion.
        var expectedInputHint = await FormVoiceCommandEntryHintBuilder
            .BuildAsync(turnContext)
            .ConfigureAwait(false);

        turnContext = turnContext with
        {
            FieldEntryPrompt = FormFieldPositionPromptBuilder.GetFieldPositionPrompt(
                turnContext.Context.Contract,
                turnContext.Context.IsoCode,
                turnContext.Context.VoiceSettings.GetPolitenessLevel(),
                statusSnapshot.CurrentFieldIndex,
                statusSnapshot.Progress.AllFields.Total,
                field.Label,
                expectedInputHint)
        };
        turnContext.StateTracker.RecordFieldEntryAnswerableTotal(statusSnapshot.Progress.AllFields.Total);

        await MaybeOfferSilentInputOptionAsync(
                turnContext,
                expectedInputMode,
                cancellationToken)
            .ConfigureAwait(false);

        var orchestrationResult = await _transcriptOrchestrator.OrchestrateAsync(
                turnContext,
                expectedInputMode,
                announceReadOnlyFieldGroupContent,
                cancellationToken)
            .ConfigureAwait(false);

        if (orchestrationResult.HasFinalResult)
        {
            return orchestrationResult.FinalResult!;
        }

        // Die ausdrücklich leere Mehrfachauswahl über ihren eigenen Anfrageweg
        // weitergeben. Der gewöhnliche Konstruktor weist leeren Text zurück.
        var applyRequest = orchestrationResult.IsEmptySelectionContinuation
            ? FormVoiceCommandFieldApplyRequest.ForEmptySetSelection(
                expectedInputMode,
                orchestrationResult.SttConfidencePercent)
            : orchestrationResult.IsComposedValueContinuation
                ? FormVoiceCommandFieldApplyRequest.ForComposedValue(
                    expectedInputMode,
                    orchestrationResult.RequiredTranscriptForApply,
                    orchestrationResult.SttConfidencePercent)
                : new FormVoiceCommandFieldApplyRequest(
                    expectedInputMode,
                    orchestrationResult.RequiredTranscriptForApply,
                    orchestrationResult.AmbiguityTelemetry,
                    orchestrationResult.SttConfidencePercent,
                    orchestrationResult.FinalizedByExplicitAccept);

        return await _fieldApplyService.ApplyAsync(turnContext, applyRequest, cancellationToken)
            .ConfigureAwait(false);
    }

    // Beim Gruppeneinstieg eine vorhandene Titelansage und anschließend die
    // optionale Zusammenfassung anfordern. Ohne aufgelösten Titel entsteht hier keine Titelansage.
    private async Task MaybeAnnounceFieldGroupIntroAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        bool announceReadOnlyFieldGroupContent,
        CancellationToken cancellationToken)
    {
        if (!announceReadOnlyFieldGroupContent)
        {
            return;
        }

        if (turnContext.Context.FormProjection?.FieldGroupsById is not null
            && turnContext.Context.FormProjection.FieldGroupsById.TryGetValue(turnContext.Field.FieldGroupId, out var fieldGroup))
        {
            if (!string.IsNullOrWhiteSpace(fieldGroup.Title))
            {
                await _speechService
                    .SpeakSafeAsync(
                        FormFieldGroupIntroPromptBuilder.GetFieldGroupIntroPrompt(
                            turnContext.Context.Contract,
                            turnContext.Context.IsoCode,
                            turnContext.Context.VoiceSettings.GetPolitenessLevel(),
                            fieldGroup.Title.Trim()),
                        turnContext.Context.IsoCode,
                        turnContext.Context.VoiceSettings,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(fieldGroup.VoiceSummary))
            {
                await _speechService
                    .SpeakSafeAsync(
                        fieldGroup.VoiceSummary.Trim(),
                        turnContext.Context.IsoCode,
                        turnContext.Context.VoiceSettings,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    private async Task MaybeOfferSilentInputOptionAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        InputMode expectedInputMode,
        CancellationToken cancellationToken)
    {
        if (expectedInputMode == InputMode.Keyboard)
        {
            return;
        }

        var shouldOfferResult = await turnContext.PrivacyReadbackPolicy.ShouldOfferSilentInputOptionAsync(turnContext.Field.VariableId)
            .ConfigureAwait(false);
        if (!shouldOfferResult.Allowed)
        {
            return;
        }

        var silentInputLabel = FormInputFeedbackPromptBuilder.GetSilentInputFallbackLabel(
            turnContext.Context.Contract,
            turnContext.Context.IsoCode,
            shouldOfferResult.SilentInputFallback,
            turnContext.Context.VoiceSettings.GetPolitenessLevel());
        await _speechService.SpeakSafeAsync(
                FormInputFeedbackPromptBuilder.GetSilentInputOfferPrompt(
                    turnContext.Context.Contract,
                    turnContext.Context.IsoCode,
                    turnContext.Context.VoiceSettings.GetPolitenessLevel(),
                    silentInputLabel),
                turnContext.Context.IsoCode,
                turnContext.Context.VoiceSettings,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
