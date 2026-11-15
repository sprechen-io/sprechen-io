using Sprechen.Contracts.FormSurfaces;
using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet Orientierung, Veröffentlichung und Feldbearbeitung einer Formularsitzung und übergibt fehlenden Fokus an Abschlussprüfung oder Absendeweg.
/// </summary>
internal sealed class FormVoiceCommandFieldCoordinator
{
    private readonly IFormVoiceCommandPromptService _promptService;
    private readonly IFormVoiceCommandSubmissionService _submissionService;
    private readonly IFormVoiceCommandFieldInteractionHandler _fieldHandler;
    private readonly FormVoiceCommandSessionSurfacePublisher _surfacePublisher;
    private readonly FormVoiceCommandSessionResultFactory _resultFactory;
    private readonly FormClosingReviewTurnFlow _closingReviewTurnFlow;

    public FormVoiceCommandFieldCoordinator(
        IFormVoiceCommandPromptService promptService,
        IFormVoiceCommandSubmissionService submissionService,
        IFormVoiceCommandFieldInteractionHandler fieldHandler,
        FormVoiceCommandSessionSurfacePublisher surfacePublisher,
        FormVoiceCommandSessionResultFactory resultFactory,
        FormClosingReviewTurnFlow closingReviewTurnFlow)
    {
        _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));
        _submissionService = submissionService ?? throw new ArgumentNullException(nameof(submissionService));
        _fieldHandler = fieldHandler ?? throw new ArgumentNullException(nameof(fieldHandler));
        _surfacePublisher = surfacePublisher ?? throw new ArgumentNullException(nameof(surfacePublisher));
        _resultFactory = resultFactory ?? throw new ArgumentNullException(nameof(resultFactory));
        _closingReviewTurnFlow = closingReviewTurnFlow ?? throw new ArgumentNullException(nameof(closingReviewTurnFlow));
    }

    public async Task<FormVoiceCommandSessionRunResult> RunAsync(
        FormVoiceCommandContext sessionContext,
        IFormDialogRuntime runtime,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        FormSurfaceDefinition formProjection,
        FormSurfacePublicationSession publication,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionContext);
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(stateTracker);
        ArgumentNullException.ThrowIfNull(formProjection);
        ArgumentNullException.ThrowIfNull(publication);

        await AnnounceSharedVariablesAsync(sessionContext, cancellationToken).ConfigureAwait(false);
        await _surfacePublisher.TryPublishActiveAsync(publication, sessionContext, runtime, formProjection)
            .ConfigureAwait(false);

        await _promptService.SpeakFormOrientationAsync(
                sessionContext.Application,
                sessionContext.Form,
                sessionContext.Contract,
                runtime,
                sessionContext.IsoCode,
                sessionContext.VoiceSettings,
                cancellationToken)
            .ConfigureAwait(false);

        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            sessionContext.VoiceSettings,
            _promptService,
            stateTracker,
            runtime,
            sessionContext.IsoCode);
        var escalation = new FormVoiceCommandUnrecognizedInputEscalationPolicy();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var resolution = await ResolveFocusedFieldOrSubmitAsync(
                    sessionContext,
                    runtime,
                    session,
                    stateTracker,
                    formProjection,
                    publication,
                    cancellationToken)
                .ConfigureAwait(false);

            if (resolution.Terminal is not null)
            {
                return resolution.Terminal;
            }

            var field = resolution.Field!;
            var handlingResult = await _fieldHandler.HandleAsync(
                    new FormVoiceCommandFieldTurnContext(
                        sessionContext,
                        runtime,
                        field,
                        session,
                        stateTracker,
                        autoHelp,
                        escalation,
                        (IFormDialogPrivacyReadbackPolicy)runtime),
                    cancellationToken)
                .ConfigureAwait(false);
            var terminalResult = ResolveTerminalResult(handlingResult);
            if (terminalResult is not null)
            {
                return terminalResult;
            }

            await _surfacePublisher.TryPublishActiveAsync(publication, sessionContext, runtime, formProjection)
                .ConfigureAwait(false);
        }
    }

    private async Task AnnounceSharedVariablesAsync(
        FormVoiceCommandContext sessionContext,
        CancellationToken cancellationToken)
    {
        if (sessionContext.SharedAdoptions.Length == 0)
        {
            return;
        }

        await _promptService.SpeakSharedVariableAnnouncementAsync(
                sessionContext.Application,
                sessionContext.Form,
                sessionContext.Contract,
                sessionContext.SharedAdoptions,
                sessionContext.IsoCode,
                sessionContext.VoiceSettings,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<FocusResolution> ResolveFocusedFieldOrSubmitAsync(
        FormVoiceCommandContext sessionContext,
        IFormDialogRuntime runtime,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        FormSurfaceDefinition formProjection,
        FormSurfacePublicationSession publication,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            if (await runtime.GetFocusedFieldIdAsync().ConfigureAwait(false) is null)
            {
                // Ohne fokussiertes Feld je nach Fortsetzungsmodus den Abschlussdialog oder
                // den Absendeversuch anfordern.
                if (sessionContext.VoiceSettings.GetAutoAdvanceAfterCommit())
                {
                    var closingResult = await _closingReviewTurnFlow.RunAsync(
                            sessionContext,
                            runtime,
                            session,
                            stateTracker,
                            cancellationToken)
                        .ConfigureAwait(false);

                    if (closingResult.Terminal is not null)
                    {
                        return new FocusResolution(
                            Field: null,
                            Terminal: ResolveTerminalResult(closingResult.Terminal));
                    }

                    await _surfacePublisher.TryPublishActiveAsync(publication, sessionContext, runtime, formProjection)
                        .ConfigureAwait(false);
                    continue;
                }

                var submission = await _submissionService.TrySubmitAsync(
                        runtime,
                        sessionContext.Contract,
                        session,
                        sessionContext.IsoCode,
                        sessionContext.VoiceSettings,
                        stateTracker,
                        cancellationToken)
                    .ConfigureAwait(false);

                switch (submission)
                {
                    case FormSubmissionResult.Submitted:
                        stateTracker.RecordSubmitted();
                        return new FocusResolution(
                            Field: null,
                            Terminal: new FormVoiceCommandSessionRunResult(
                                EvaluationOutcome.Submitted, DialogResult: null, FormSurfaceStage.Submitted));

                    case FormSubmissionResult.BlockedUnresolvable:
                        // Nach nicht auflösbarer Absendeablehnung zuerst einen weiteren Feldfokus
                        // versuchen. Ohne erreichbares Ziel bleibt das Abbruchergebnis.
                        if (await runtime.TryMoveFocusNextAsync().ConfigureAwait(false)
                            || await runtime.TryMoveFocusBackAsync().ConfigureAwait(false))
                        {
                            var recoveryField = await runtime.GetFocusedFieldAsync().ConfigureAwait(false);
                            if (recoveryField is not null)
                            {
                                return new FocusResolution(recoveryField, Terminal: null);
                            }
                        }

                        return new FocusResolution(
                            Field: null,
                            Terminal: FormVoiceCommandSessionRunResult.Abandoned());

                    default:
                        await _surfacePublisher.TryPublishActiveAsync(publication, sessionContext, runtime, formProjection)
                            .ConfigureAwait(false);
                        continue;
                }
            }

            var field = await runtime.GetFocusedFieldAsync().ConfigureAwait(false);
            return new FocusResolution(field, Terminal: null);
        }
    }

    private FormVoiceCommandSessionRunResult? ResolveTerminalResult(FormVoiceCommandFieldHandlingResult handlingResult)
    {
        if (handlingResult.DialogResult is not null)
        {
            return new FormVoiceCommandSessionRunResult(
                handlingResult.DialogResult.Outcome,
                handlingResult.DialogResult,
                _resultFactory.MapSurfaceStage(handlingResult.DialogResult.Outcome, handlingResult.DialogResult.AbortReason),
                handlingResult.DialogResult.AbortReason);
        }

        if (handlingResult.WasSubmitted)
        {
            return new FormVoiceCommandSessionRunResult(EvaluationOutcome.Submitted, DialogResult: null, FormSurfaceStage.Submitted);
        }

        return null;
    }
}
