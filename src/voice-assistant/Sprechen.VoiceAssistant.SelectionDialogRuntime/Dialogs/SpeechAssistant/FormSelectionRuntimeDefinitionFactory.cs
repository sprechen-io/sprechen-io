namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Setzt Formularbestand, Ansagen, Ergebnisbehandlung und Konfidenzregeln zur
/// Ausführungsdefinition zusammen. Die gemeinsame Laufzeit übernimmt anschließend die
/// Dialogsteuerung.
/// </summary>
internal sealed class FormSelectionRuntimeDefinitionFactory
{
    private readonly SelectionDialogSpeechTurnRunner _speech;
    private readonly ISpeechAssistantTrace _speechAssistantTrace;
    private readonly FormSelectionRuntimeIntentHandlerFactory _intentHandlerFactory;
    private readonly FormSelectionRuntimeConfirmationHandlerFactory _confirmationHandlerFactory;
    private readonly FormSelectionRuntimePromptProvidersFactory _promptProvidersFactory;
    private readonly FormSelectionRuntimeOutcomeHandlersFactory _outcomeHandlersFactory;
    private readonly IFormSelectionMatcher _matcher;

    public FormSelectionRuntimeDefinitionFactory(
        SelectionDialogSpeechTurnRunner speech,
        IFormsCatalogIntentParser intentParser,
        IFormSelectionMatcher matcher,
        ISpeechAssistantTrace speechAssistantTrace,
        FormSelectionTransitionPolicy transitionPolicy)
    {
        _speech = speech ?? throw new ArgumentNullException(nameof(speech));
        ArgumentNullException.ThrowIfNull(intentParser);
        _matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
        _speechAssistantTrace = speechAssistantTrace ?? throw new ArgumentNullException(nameof(speechAssistantTrace));
        _intentHandlerFactory = new FormSelectionRuntimeIntentHandlerFactory(
            _matcher,
            transitionPolicy ?? throw new ArgumentNullException(nameof(transitionPolicy)));
        _confirmationHandlerFactory = new FormSelectionRuntimeConfirmationHandlerFactory(intentParser);
        _promptProvidersFactory = new FormSelectionRuntimePromptProvidersFactory();
        _outcomeHandlersFactory = new FormSelectionRuntimeOutcomeHandlersFactory(
            new FormSelectionListeningInputRoute(intentParser),
            new FormSelectionCancellationReasonMapper());
    }

    public SelectionDialogExecutionDefinition<FormInfo, FormSelectionDialogResult> Create(
        FormSelectionDialogSession session,
        string correlationId)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        var intentHandler = _intentHandlerFactory.Create(session);
        var confirmationHandler = _confirmationHandlerFactory.Create(session);
        var initialCandidates = _matcher.CandidateMatcher.CreateSnapshot(
            session.Forms,
            SelectionRevision.Initial);
        var traceContext = new SpeechAssistantTraceContext(
            _speechAssistantTrace,
            correlationId,
            SpeechAssistantTraceScope.SelectionDialogRuntime);
        return new SelectionDialogExecutionDefinition<FormInfo, FormSelectionDialogResult>(
            new SelectionDialogExecutionSettings<FormInfo>(
                speech: _speech,
                isoCode: session.IsoCode,
                initialCandidates: initialCandidates,
                interactionSettings: session.InteractionSettings,
                maxRetries: session.MaxRetries,
                noInputTimeoutMs: session.NoInputTimeoutMs,
                trace: traceContext),
            _promptProvidersFactory.Create(session),
            _outcomeHandlersFactory.Create(session, intentHandler, confirmationHandler),
            new SelectionConfirmationTransitionPolicy<FormInfo>(
                _matcher.CandidateMatcher,
                session.InteractionSettings.SttMinConfidenceToAccept,
                session.InteractionSettings.SttMinConfidenceToConfirm));
    }
}
