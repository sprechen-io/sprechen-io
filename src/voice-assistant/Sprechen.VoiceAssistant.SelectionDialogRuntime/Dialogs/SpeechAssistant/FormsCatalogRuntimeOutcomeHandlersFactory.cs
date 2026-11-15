namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Passt Katalogeingaben, Bestätigungen und Endergebnisse an die gemeinsame Auswahllaufzeit an.
/// Katalogzustand und ein möglicher Sprachneustart bleiben dabei außerhalb der generischen
/// Kandidatensteuerung.
/// </summary>
internal sealed class FormsCatalogRuntimeOutcomeHandlersFactory
{
    private readonly FormsCatalogListeningInputRoute _listeningInputRoute;

    public FormsCatalogRuntimeOutcomeHandlersFactory(FormsCatalogListeningInputRoute listeningInputRoute)
    {
        _listeningInputRoute = listeningInputRoute ?? throw new ArgumentNullException(nameof(listeningInputRoute));
    }

    public SelectionDialogOutcomeHandlers<ApplicationInfo, FormsCatalogDialogResult> Create(
        FormsCatalogDialogSession session,
        string correlationId,
        FormsCatalogDialogRuntimeState state,
        SelectionDialogCommonIntentHandler<ApplicationInfo> intentHandler,
        SelectionDialogConfirmationHandler<ApplicationInfo> confirmationHandler)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intentHandler);
        ArgumentNullException.ThrowIfNull(confirmationHandler);

        return new SelectionDialogOutcomeHandlers<ApplicationInfo, FormsCatalogDialogResult>(
            onListeningInputAsync: async (transcript, candidates, lastPrompt, ct) =>
                await ProcessListeningInputAsync(
                        session,
                        correlationId,
                        state,
                        intentHandler,
                        transcript,
                        candidates,
                        lastPrompt,
                        ct)
                    .ConfigureAwait(false),
            onConfirmInputAsync: (confirmTranscript, ct) =>
                confirmationHandler.ParseAsync(confirmTranscript, ct),
            selectedLaunch: new SelectionDialogLaunchDefinition<ApplicationInfo, FormsCatalogDialogResult>(
                buildPrompt: (selected, _) => FormsCatalogPromptBuilder.GetLaunchPrompt(
                    session.PromptContext,
                    selected.Item,
                    session.IsoCode,
                    session.PolitenessLevel),
                projectResult: (selected, _) => FormsCatalogDialogResult.Selected(
                    selected.Item.Id,
                    selected.Item.DisplayName,
                    selected.Item.FormCount,
                    selected.Item.DefaultFormId,
                    session.IsoCode)),
            createCancelledResult: (reason, _) => FormsCatalogDialogResult.Cancelled(
                reason == SelectionDialogCompletionReason.RestartRequested
                    ? DialogCancellationReason.UserCancelled
                    : FormsCatalogCancellationReasonMapper.Map(reason),
                session.IsoCode));
    }

    private async Task<SelectionDialogIntentStep<ApplicationInfo>> ProcessListeningInputAsync(
        FormsCatalogDialogSession session,
        string correlationId,
        FormsCatalogDialogRuntimeState state,
        SelectionDialogCommonIntentHandler<ApplicationInfo> intentHandler,
        string transcript,
        SelectionDialogTurnSnapshot<ApplicationInfo> candidates,
        SelectionDialogPrompt? lastPrompt,
        CancellationToken cancellationToken)
    {
        var intentHandling = await _listeningInputRoute
            .ProcessAsync(
                session,
                correlationId,
                state,
                intentHandler,
                transcript,
                candidates,
                lastPrompt,
                cancellationToken)
            .ConfigureAwait(false);

        state.Apply(intentHandling);
        return new SelectionDialogIntentStep<ApplicationInfo>(
            intentHandling.NextPhase,
            intentHandling.CandidateTransition,
            intentHandling.SpokenPrompt,
            intentHandling.CompletionReason);
    }
}
