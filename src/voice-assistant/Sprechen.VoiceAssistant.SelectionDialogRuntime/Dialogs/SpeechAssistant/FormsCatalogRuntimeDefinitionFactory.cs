using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Setzt Katalogsitzung, Ansagefunktionen, Ergebnisbehandlung und Erkennungshinweise
/// zur Ausführungsdefinition zusammen. Die generische Laufzeit kann damit Anwendungen
/// auswählen, ohne die katalogspezifischen Regeln selbst zu implementieren.
/// </summary>
internal sealed class FormsCatalogRuntimeDefinitionFactory
{
    private readonly SelectionDialogSpeechTurnRunner _speech;
    private readonly ISpeechAssistantTrace _speechAssistantTrace;
    private readonly FormsCatalogRuntimeIntentHandlerFactory _intentHandlerFactory;
    private readonly FormsCatalogRuntimeConfirmationHandlerFactory _confirmationHandlerFactory;
    private readonly FormsCatalogRuntimePromptProvidersFactory _promptProvidersFactory;
    private readonly FormsCatalogRuntimeOutcomeHandlersFactory _outcomeHandlersFactory;
    private readonly ApplicationSelectionKeytermVocabulary _applicationKeyterms = new();
    private readonly ApplicationMatcher _applicationMatcher = new();

    public FormsCatalogRuntimeDefinitionFactory(
        SelectionDialogSpeechTurnRunner speech,
        IFormsCatalogIntentParser intentParser,
        FormsCatalogDialogIntentRouter intentRouter,
        ISpeechAssistantTrace speechAssistantTrace,
        FormsCatalogSelectionTransitionPolicy transitionPolicy)
    {
        _speech = speech ?? throw new ArgumentNullException(nameof(speech));
        ArgumentNullException.ThrowIfNull(intentParser);
        _speechAssistantTrace = speechAssistantTrace ?? throw new ArgumentNullException(nameof(speechAssistantTrace));
        _intentHandlerFactory = new FormsCatalogRuntimeIntentHandlerFactory(
            transitionPolicy ?? throw new ArgumentNullException(nameof(transitionPolicy)),
            _applicationMatcher);
        _confirmationHandlerFactory = new FormsCatalogRuntimeConfirmationHandlerFactory(intentParser);
        _promptProvidersFactory = new FormsCatalogRuntimePromptProvidersFactory();
        _outcomeHandlersFactory = new FormsCatalogRuntimeOutcomeHandlersFactory(
            new FormsCatalogListeningInputRoute(
                intentParser,
                intentRouter ?? throw new ArgumentNullException(nameof(intentRouter))));
    }

    public SelectionDialogExecutionDefinition<ApplicationInfo, FormsCatalogDialogResult> Create(
        FormsCatalogDialogSession session,
        string correlationId,
        SpeechTranscript? initialCatalogTranscript,
        FormsCatalogDialogRuntimeState state)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentNullException.ThrowIfNull(state);

        var intentHandler = _intentHandlerFactory.Create(session);
        var confirmationHandler = _confirmationHandlerFactory.Create(session);
        var initialCandidates = _applicationMatcher.CandidateMatcher.CreateSnapshot(
            session.Applications,
            SelectionRevision.Initial);
        // Die bereits lokalisierten Anwendungsnamen liefern optionale Erkennungshinweise aus
        // derselben Quelle wie der Kandidatenvergleich. Die begrenzte Projektion ergänzt keine
        // Namen und gibt ohne verwendbare Namen eine leere Liste zurück.
        var applicationKeyterms = _applicationKeyterms.GetKeyterms(session.Applications);
        var traceContext = new SpeechAssistantTraceContext(
            _speechAssistantTrace,
            correlationId,
            SpeechAssistantTraceScope.SelectionDialogRuntime);
        return new SelectionDialogExecutionDefinition<ApplicationInfo, FormsCatalogDialogResult>(
            new SelectionDialogExecutionSettings<ApplicationInfo>(
                speech: _speech,
                isoCode: session.IsoCode,
                initialCandidates: initialCandidates,
                interactionSettings: session.InteractionSettings,
                maxRetries: session.MaxRetries,
                noInputTimeoutMs: session.NoInputTimeoutMs,
                initialListeningTranscript: initialCatalogTranscript,
                trace: traceContext,
                keyterms: new FormsCatalogSelectionTurnKeyterms(
                    session.Catalog,
                    session.IsoCode,
                    session.PolitenessLevel,
                    applicationKeyterms)),
            _promptProvidersFactory.Create(session),
            _outcomeHandlersFactory.Create(
                session,
                correlationId,
                state,
                intentHandler,
                confirmationHandler),
            new SelectionConfirmationTransitionPolicy<ApplicationInfo>(
                _applicationMatcher.CandidateMatcher,
                session.InteractionSettings.SttMinConfidenceToAccept,
                session.InteractionSettings.SttMinConfidenceToConfirm));
    }
}
