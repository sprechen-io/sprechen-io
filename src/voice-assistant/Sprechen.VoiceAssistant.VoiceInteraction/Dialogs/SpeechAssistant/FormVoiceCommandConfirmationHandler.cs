using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Steuert die Bestätigung eines Feldwerts: erstellt die Rückfrage, nimmt
/// die Antwort auf und fragt bei unklarer Antwort einmal mit passendem
/// Hinweis nach. Anschließend folgen Korrektur, Befehlsausführung oder Abschluss.
/// </summary>
internal sealed class FormVoiceCommandConfirmationHandler
{
    private readonly FormVoiceCommandConfirmationValuePresenter _valuePresenter;
    private readonly FormVoiceCommandConfirmationDecisionService _confirmationDecisionService;
    private readonly FormVoiceCommandConfirmationRepairActionFlow _repairActionFlow;
    private readonly FormVoiceCommandConfirmedFieldCompletionFlow _completionFlow;
    private readonly FormVoiceCommandFieldErrorHandlingService _fieldErrorHandlingService;

    public FormVoiceCommandConfirmationHandler(
        FormVoiceCommandConfirmationValuePresenter valuePresenter,
        FormVoiceCommandConfirmationDecisionService confirmationDecisionService,
        FormVoiceCommandConfirmationRepairActionFlow repairActionFlow,
        FormVoiceCommandConfirmedFieldCompletionFlow completionFlow,
        FormVoiceCommandFieldErrorHandlingService fieldErrorHandlingService)
    {
        _valuePresenter = valuePresenter ?? throw new ArgumentNullException(nameof(valuePresenter));
        _confirmationDecisionService = confirmationDecisionService ?? throw new ArgumentNullException(nameof(confirmationDecisionService));
        _repairActionFlow = repairActionFlow ?? throw new ArgumentNullException(nameof(repairActionFlow));
        _completionFlow = completionFlow ?? throw new ArgumentNullException(nameof(completionFlow));
        _fieldErrorHandlingService = fieldErrorHandlingService ?? throw new ArgumentNullException(nameof(fieldErrorHandlingService));
    }

    public async Task<FormVoiceCommandFieldHandlingResult> HandleAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        FormVoiceCommandConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentNullException.ThrowIfNull(request);

        // Nach ausdrücklicher Übernahme einer Mehrfachauswahl keine zusätzliche
        // Bestätigungsfrage stellen. Stattdessen die übernommenen Werte unter
        // Beachtung der Vorleseregeln für den Abschluss vorbereiten. Andere
        // Abschlussarten durchlaufen die Bestätigungsfrage weiter unten.
        if (request.FinalizedByExplicitAccept)
        {
            turnContext.StateTracker.BeginFieldConfirmation();
            turnContext.AutoHelp.Reset();
            turnContext.Escalation.Reset();
            var committedValueReadback = await _valuePresenter
                .BuildUnderstoodValueAsync(turnContext, request.TranscriptForFallback)
                .ConfigureAwait(false);
            if (committedValueReadback.Status == FormSpokenValueProjectionStatus.Unavailable)
            {
                return await ReplayUnavailableAsync(
                        turnContext,
                        committedValueReadback.Value,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return await _completionFlow.CompleteAsync(turnContext, committedValueReadback, cancellationToken)
                .ConfigureAwait(false);
        }

        turnContext.StateTracker.BeginFieldConfirmation();
        var confirmationPrompt = await _valuePresenter.BuildAsync(turnContext, request, cancellationToken)
            .ConfigureAwait(false);
        if (confirmationPrompt.Status == FormSpokenValueProjectionStatus.Unavailable)
        {
            return await ReplayUnavailableAsync(turnContext, confirmationPrompt.Prompt, cancellationToken)
                .ConfigureAwait(false);
        }

        var confirmationDecision = await _confirmationDecisionService.CaptureDecisionAsync(
                turnContext.Context,
                turnContext.Session,
                turnContext.StateTracker,
                confirmationPrompt.Prompt,
                cancellationToken)
                .ConfigureAwait(false);

        // Bei unklarer Antwort einmal mit einem zum Grund passenden Hinweis und
        // demselben Wert nachfragen. Eine erneut unklare Antwort wird anschließend
        // als Korrekturbedarf behandelt. Den Bestätigungszähler dabei nicht erneut erhöhen.
        if (confirmationDecision.Kind == ConfirmationDecisionKind.Undecided
            && confirmationDecision.UndecidedReason is { } reason)
        {
            _repairActionFlow.TrackReask(turnContext, reason);
            var reaskPrompt = _valuePresenter.BuildReask(turnContext, confirmationPrompt, reason);
            confirmationDecision = await _confirmationDecisionService.CaptureDecisionAsync(
                    turnContext.Context,
                    turnContext.Session,
                    turnContext.StateTracker,
                    reaskPrompt.Prompt,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        // Unklare Antworten, Korrekturbefehle und globale Befehle zunächst an die
        // zuständige Verarbeitung übergeben. Nur wenn daraus kein eigenes
        // Feldergebnis entsteht, wird die Bestätigung abgeschlossen.
        var repairResult = await _repairActionFlow.TryHandleAsync(
                turnContext,
                confirmationDecision,
                cancellationToken)
            .ConfigureAwait(false);
        if (repairResult is not null)
        {
            return repairResult;
        }

        return await _completionFlow.CompleteAsync(turnContext, cancellationToken)
            .ConfigureAwait(false);
    }

    private Task<FormVoiceCommandFieldHandlingResult> ReplayUnavailableAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        string prompt,
        CancellationToken cancellationToken)
    {
        var spoken = string.IsNullOrWhiteSpace(prompt)
            ? FormConfirmationPromptBuilder.GetSpokenValueUnavailablePrompt(
                turnContext.Context.Contract,
                turnContext.Context.IsoCode,
                turnContext.Context.VoiceSettings.GetPolitenessLevel())
            : prompt;
        return _fieldErrorHandlingService.HandlePromptedFailureAsync(
            turnContext,
            FormVoiceCommandFieldErrorCodes.SpokenValueUnavailable,
            spoken,
            TelemetryErrorTriggers.UserInput,
            cancellationToken);
    }
}
