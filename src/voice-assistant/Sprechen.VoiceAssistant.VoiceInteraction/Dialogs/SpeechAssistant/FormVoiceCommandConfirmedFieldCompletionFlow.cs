namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet den Abschluss eines bestätigten Felds, dessen Rückmeldung und die weitere Navigation. Ohne nächstes Ziel wird bei automatischer Fortsetzung die Abschlussprüfung aufgerufen.
/// </summary>
internal sealed class FormVoiceCommandConfirmedFieldCompletionFlow
{
    private readonly IFormVoiceCommandSpeechService _speechService;
    private readonly IEarconPlayer _earconPlayer;
    private readonly FormClosingReviewTurnFlow _closingReviewTurnFlow;

    public FormVoiceCommandConfirmedFieldCompletionFlow(
        IFormVoiceCommandSpeechService speechService,
        IEarconPlayer earconPlayer,
        FormClosingReviewTurnFlow closingReviewTurnFlow)
    {
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
        _earconPlayer = earconPlayer ?? throw new ArgumentNullException(nameof(earconPlayer));
        _closingReviewTurnFlow = closingReviewTurnFlow ?? throw new ArgumentNullException(nameof(closingReviewTurnFlow));
    }

    public Task<FormVoiceCommandFieldHandlingResult> CompleteAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        CancellationToken cancellationToken)
        => CompleteAsync(turnContext, committedValueReadback: null, cancellationToken);

    // Ein übergebener Vorlesewert ergänzt die Bestätigung beim automatischen
    // Weitergehen. Ohne diesen Text wird die allgemeine Annahmebestätigung verwendet.
    public async Task<FormVoiceCommandFieldHandlingResult> CompleteAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        FormVoiceCommandUnderstoodValue? committedValueReadback,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);

        var context = turnContext.Context;
        var interpreter = turnContext.Interpreter;
        var stateTracker = turnContext.StateTracker;
        stateTracker.CompleteConfirmedField();

        if (!context.VoiceSettings.GetAutoAdvanceAfterCommit())
        {
            await SpeakAnswerableTotalChangeIfNeededAsync(turnContext, cancellationToken)
                .ConfigureAwait(false);
            // Bei erster manueller Fortsetzung dieser Sitzung den ausführlichen
            // Fortsetzungshinweis anfordern, danach die kürzere Erinnerung.
            var manualContinuationPrompt = stateTracker.HasAlreadyTaught(DialogTeachOnceTopics.ManualContinuationTail)
                ? FormDialogFlowPromptTextResolver.GetAutoAdvanceManualContinuationShortPrompt(context.IsoCode)
                : FormDialogFlowPromptTextResolver.GetAutoAdvanceManualContinuationPrompt(context.IsoCode, context.VoiceSettings.GetPolitenessLevel());
            await _speechService.SpeakSafeAsync(
                    manualContinuationPrompt,
                    context.IsoCode,
                    context.VoiceSettings,
                    cancellationToken)
                .ConfigureAwait(false);
            return FormVoiceCommandFieldHandlingResult.Continue();
        }

        if (await interpreter.TryMoveFocusNextAsync().ConfigureAwait(false))
        {
            var acceptedPrompt = committedValueReadback is { IncludesValue: true } namedValue
                ? FormConfirmationPromptBuilder.GetCommitAcceptedNamingValuePrompt(
                    context.IsoCode,
                    turnContext.Field.Label,
                    namedValue.Value)
                : FormDialogFlowPromptTextResolver.GetCommitAcceptedPrompt(context.IsoCode);
            await _speechService.SpeakSafeAsync(
                    acceptedPrompt,
                    context.IsoCode,
                    context.VoiceSettings,
                    cancellationToken)
                .ConfigureAwait(false);
            await SpeakAnswerableTotalChangeIfNeededAsync(turnContext, cancellationToken)
                .ConfigureAwait(false);
            await PlayCueSafeAsync(EarconId.ValidationSuccess, cancellationToken).ConfigureAwait(false);
            await RecordFieldGroupAutoAdvanceAsync(turnContext, cancellationToken).ConfigureAwait(false);
            return FormVoiceCommandFieldHandlingResult.Continue();
        }

        await SpeakAnswerableTotalChangeIfNeededAsync(turnContext, cancellationToken)
            .ConfigureAwait(false);

        // Ohne weiteres Fokusziel an den Abschlussdialog übergeben. Dessen Endergebnis
        // beendet den Ablauf. Andernfalls setzt der Koordinator den Feldablauf fort.
        var committedValueReadbackPrompt = committedValueReadback is { IncludesValue: true } lastFieldNamedValue
            ? FormConfirmationPromptBuilder.GetCommitAcceptedNamingValuePrompt(
                context.IsoCode,
                turnContext.Field.Label,
                lastFieldNamedValue.Value)
            : null;
        var closingResult = await _closingReviewTurnFlow.RunAsync(
                context,
                interpreter,
                turnContext.Session,
                stateTracker,
                committedValueReadbackPrompt,
                cancellationToken)
            .ConfigureAwait(false);

        return closingResult.Terminal ?? FormVoiceCommandFieldHandlingResult.Continue();
    }

    private async Task SpeakAnswerableTotalChangeIfNeededAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        CancellationToken cancellationToken)
    {
        if (turnContext.StateTracker.FieldEntryAnswerableTotal is not int previous)
        {
            return;
        }

        var snapshot = await FormVoiceCommandStatusSnapshotFactory
            .CreateAsync(turnContext.Context, turnContext.Interpreter)
            .ConfigureAwait(false);
        var total = snapshot.Progress.AllFields.Total;
        if (total == previous)
        {
            return;
        }

        var prompt = FormDialogFlowPromptTextResolver.GetProgressAnswerableTotalChangedPrompt(
            turnContext.Context.IsoCode,
            previous,
            total);
        await _speechService.SpeakSafeAsync(
                prompt,
                turnContext.Context.IsoCode,
                turnContext.Context.VoiceSettings,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task RecordFieldGroupAutoAdvanceAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var nextField = await turnContext.Interpreter.GetFocusedFieldAsync().ConfigureAwait(false);
        var crossesGroupBoundary = !string.Equals(
            nextField.FieldGroupId,
            turnContext.Field.FieldGroupId,
            StringComparison.Ordinal);
        if (crossesGroupBoundary)
        {
            turnContext.StateTracker.RecordAutoAdvanceToNextGroup(nextField.FieldGroupId);
        }

        // An dieser Stelle keinen zusätzlichen Strukturton für den automatischen
        // Wechsel anfordern. Die nachfolgende Feldansage bleibt beim weiteren Ablauf.
    }

    private async Task PlayCueSafeAsync(EarconId earcon, CancellationToken cancellationToken)
    {
        try
        {
            await _earconPlayer.PlayAsync(earcon, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // Fehler dieses Tons außer Abbruchausnahmen nicht
            // an den Fortsetzungsablauf weitergeben.
        }
    }
}
