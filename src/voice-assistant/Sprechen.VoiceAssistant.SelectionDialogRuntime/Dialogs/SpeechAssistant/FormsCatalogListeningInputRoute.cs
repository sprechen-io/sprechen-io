namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Berücksichtigt bei Katalogeingaben offene Rückfragen zur Sprache oder zur
/// Anwendungsbeschreibung. Wird die gewünschte Anwendung geklärt, kann ihre Beschreibung
/// ausgegeben werden, ohne sie zu starten.
/// </summary>
internal sealed class FormsCatalogListeningInputRoute
{
    private readonly IFormsCatalogIntentParser _intentParser;
    private readonly FormsCatalogDialogIntentRouter _intentRouter;

    public FormsCatalogListeningInputRoute(
        IFormsCatalogIntentParser intentParser,
        FormsCatalogDialogIntentRouter intentRouter)
    {
        _intentParser = intentParser ?? throw new ArgumentNullException(nameof(intentParser));
        _intentRouter = intentRouter ?? throw new ArgumentNullException(nameof(intentRouter));
    }

    public async Task<FormsCatalogIntentHandlingResult> ProcessAsync(
        FormsCatalogDialogSession session,
        string correlationId,
        FormsCatalogDialogRuntimeState state,
        SelectionDialogCommonIntentHandler<ApplicationInfo> intentHandler,
        string transcript,
        SelectionDialogTurnSnapshot<ApplicationInfo> candidates,
        SelectionDialogPrompt? lastPrompt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intentHandler);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);

        var turnContext = new FormsCatalogDialogTurnContext(
            intentHandler,
            session.Catalog,
            correlationId,
            new FormsCatalogDialogTurnSelection(
                candidates,
                lastPrompt),
            session.PromptRuntime);

        if (state.AwaitingLanguageTarget)
        {
            return await _intentRouter
                .ProcessPendingLanguageTargetAsync(turnContext, transcript, cancellationToken)
                .ConfigureAwait(false);
        }

        var intentResult = _intentParser.Parse(
            session.Catalog,
            transcript,
            session.IsoCode,
            session.PolitenessLevel);
        var intentHandling = await _intentRouter
            .ProcessIntentAsync(
                turnContext,
                intentResult,
                cancellationToken)
            .ConfigureAwait(false);

        if (state.AwaitingDescriptionCandidate && intentHandling.SelectedCandidate is not null)
        {
            state.ClearDescriptionResolution();
            return _intentRouter.CompletePendingDescription(
                turnContext,
                intentHandling.SelectedCandidate);
        }

        if (intentResult.Intent == FormsCatalogIntent.Describe)
        {
            if (intentHandling.CandidateTransition.Kind ==
                SelectionDialogCandidateTransitionKind.AmbiguityRetention)
            {
                state.BeginDescriptionResolution();
            }
            else
            {
                state.ClearDescriptionResolution();
            }
        }
        else if (intentResult.Intent is FormsCatalogIntent.Search
                 or FormsCatalogIntent.ChangeLanguage
                 or FormsCatalogIntent.Cancel)
        {
            state.ClearDescriptionResolution();
        }

        return intentHandling;
    }
}
