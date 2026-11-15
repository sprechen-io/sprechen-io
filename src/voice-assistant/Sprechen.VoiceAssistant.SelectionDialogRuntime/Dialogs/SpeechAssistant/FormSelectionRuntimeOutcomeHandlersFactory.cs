namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet Formular-Eingaben, Bestätigungen und Start- oder Abbruchergebnisse mit der
/// generischen Laufzeit. Die formularbezogenen Ergebnisdaten bleiben an dieser Anpassungsstelle.
/// </summary>
internal sealed class FormSelectionRuntimeOutcomeHandlersFactory
{
    private readonly FormSelectionListeningInputRoute _listeningInputRoute;
    private readonly FormSelectionCancellationReasonMapper _cancellationReasonMapper;

    public FormSelectionRuntimeOutcomeHandlersFactory(
        FormSelectionListeningInputRoute listeningInputRoute,
        FormSelectionCancellationReasonMapper cancellationReasonMapper)
    {
        _listeningInputRoute = listeningInputRoute ?? throw new ArgumentNullException(nameof(listeningInputRoute));
        _cancellationReasonMapper = cancellationReasonMapper ?? throw new ArgumentNullException(nameof(cancellationReasonMapper));
    }

    public SelectionDialogOutcomeHandlers<FormInfo, FormSelectionDialogResult> Create(
        FormSelectionDialogSession session,
        SelectionDialogCommonIntentHandler<FormInfo> intentHandler,
        SelectionDialogConfirmationHandler<FormInfo> confirmationHandler)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(intentHandler);
        ArgumentNullException.ThrowIfNull(confirmationHandler);

        return new SelectionDialogOutcomeHandlers<FormInfo, FormSelectionDialogResult>(
            onListeningInputAsync: async (transcript, candidates, lastPrompt, ct) =>
            {
                var intentHandling = await _listeningInputRoute
                    .ProcessAsync(session, intentHandler, transcript, candidates, lastPrompt, ct)
                    .ConfigureAwait(false);

                return new SelectionDialogIntentStep<FormInfo>(
                    intentHandling.NextPhase,
                    intentHandling.CandidateTransition,
                    intentHandling.SpokenPrompt,
                    intentHandling.CompletionReason);
            },
            onConfirmInputAsync: (confirmTranscript, ct) =>
                confirmationHandler.ParseAsync(confirmTranscript, ct),
            selectedLaunch: new SelectionDialogLaunchDefinition<FormInfo, FormSelectionDialogResult>(
                buildPrompt: (selected, _) => FormSelectionPromptBuilder.GetLaunchPrompt(
                    session.PromptContext,
                    selected.Item,
                    session.IsoCode,
                    session.PolitenessLevel),
                projectResult: (selected, _) => FormSelectionDialogResult.Selected(
                    selected.Item.Id,
                    selected.Item.DisplayName)),
            createCancelledResult: (reason, _) => FormSelectionDialogResult.Cancelled(_cancellationReasonMapper.Map(reason)),
            singleItemLaunch: new SelectionDialogLaunchDefinition<FormInfo, FormSelectionDialogResult>(
                buildPrompt: (singleForm, _) => FormSelectionPromptBuilder.GetSingleFormPrompt(
                    session.PromptContext,
                    singleForm.Item,
                    session.IsoCode,
                    session.PolitenessLevel),
                projectResult: (singleForm, _) => FormSelectionDialogResult.Selected(
                    singleForm.Item.Id,
                    singleForm.Item.DisplayName)));
    }
}
