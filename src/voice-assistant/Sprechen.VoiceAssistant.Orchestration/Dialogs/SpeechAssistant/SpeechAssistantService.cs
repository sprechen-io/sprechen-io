using Microsoft.Extensions.Logging;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.ModelRuntime.PromptTemplates;
using Sprechen.Speech.State;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant.Logging;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Führt den Sitzungsstart mit Sprachangebot, optionaler Zuordnung zur Studie
/// und anschließendem Start des Dialoghosts aus.
/// </summary>
internal sealed class SpeechAssistantService : ISpeechAssistantService, ISpeechAssistantSilentInputController, IAsyncDisposable
{
    private readonly SpeechAssistantContext _context;
    private readonly SpeechAssistantLifecycle _lifecycle;
    private readonly SpeechAssistantLanguageInitializer _languageInitializer;
    private readonly SpeechAssistantSessionOnboardingAnnouncer _onboardingAnnouncer;
    private readonly SessionEntryLanguageOfferTurn _languageOfferTurn;
    private readonly SpeechAssistantVoiceInputToggle _voiceInputToggle;
    private readonly SpeechAssistantSilentInputActions _silentInput;
    private readonly StudyDirectEntry _studyDirectEntry;

    private SpeechAssistantState _lastKnownState = SpeechAssistantState.Initial;

    public SpeechAssistantService(SpeechAssistantContext context)
        : this(context, DialogHostFactory.Create<SpeechAssistantEvent>)
    {
    }

    internal SpeechAssistantService(
        SpeechAssistantContext context,
        Func<IDialogHost<SpeechAssistantEvent>> hostFactory)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _lifecycle = new SpeechAssistantLifecycle(
            _context,
            hostFactory ?? throw new ArgumentNullException(nameof(hostFactory)),
            UpdateLastKnownState,
            ResetRuntimeState);
        _languageInitializer = new SpeechAssistantLanguageInitializer(_context.SetupDependencies);
        _onboardingAnnouncer = new SpeechAssistantSessionOnboardingAnnouncer(
            _context.SetupDependencies,
            _context.InteractionDependencies);
        _languageOfferTurn = SessionEntryLanguageOfferTurn.Create(_context);
        _voiceInputToggle = new SpeechAssistantVoiceInputToggle(
            _context,
            _lifecycle,
            () => Volatile.Read(ref _lastKnownState),
            state => Volatile.Write(ref _lastKnownState, state));
        _silentInput = new SpeechAssistantSilentInputActions(_lifecycle);
        _studyDirectEntry = new StudyDirectEntry(
            _context.SetupDependencies.FormsCatalogDataClient,
            _context.SetupDependencies.OperationalProbeSession,
            _context.SetupDependencies.StudyTelemetryContext);
    }

    public async ValueTask<SpeechAssistantStartOutcome> StartAsync(CancellationToken cancellationToken)
    {
        _lifecycle.ThrowIfDisposed();

        var probeContext = _studyDirectEntry.CurrentProbeContext();

        var selectedIsoCode = await _languageInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        var entryOutcome = await _languageOfferTurn
            .DecideLanguageAsync(selectedIsoCode, cancellationToken)
            .ConfigureAwait(false);
        if (entryOutcome.Cancelled)
        {
            // Nach dem Einstiegsabbruch diesen Startversuch vor Einführung und Hoststart beenden. Der
            // ausdrückliche Rückgabewert hält die Oberfläche über den Ausgang informiert, damit sie
            // einen erneuten Start anbieten kann.
            await SpeakEntryCancelledAsync(entryOutcome.ContinueInIsoCode, cancellationToken).ConfigureAwait(false);
            return SpeechAssistantStartOutcome.CancelledAtEntry;
        }

        selectedIsoCode = entryOutcome.ContinueInIsoCode;
        var studyBind = await _studyDirectEntry
            .TryBindAsync(
                !string.IsNullOrWhiteSpace(_context.SetupDependencies.StudyTelemetryContext.ParticipantId) || probeContext is not null,
                probeContext,
                selectedIsoCode,
                cancellationToken)
            .ConfigureAwait(false);

        if (studyBind is not null)
        {
            var announcement = FormDialogFlowPromptTextResolver.GetStudyEntryStudyAnnouncementPrompt(
                selectedIsoCode,
                studyBind.ApplicationDisplayName);
            await _context.InteractionDependencies.TextToSpeechPlayer
                .SpeakAsync(announcement, selectedIsoCode, cancellationToken)
                .ConfigureAwait(false);
        }

        await _onboardingAnnouncer.AnnounceOnceAsync(selectedIsoCode, cancellationToken).ConfigureAwait(false);

        var host = await _lifecycle.EnsureStartedAsync(cancellationToken).ConfigureAwait(false);
        if (studyBind is not null)
        {
            await host.DispatchToActiveAsync(
                    new SpeechAssistantStudySessionBoundEvent(studyBind.Session),
                    DispatchMode.Interrupt,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        await host.DispatchToActiveAsync(new SpeechAssistantLanguageSelectedEvent(selectedIsoCode), DispatchMode.Interrupt, cancellationToken)
            .ConfigureAwait(false);
        return SpeechAssistantStartOutcome.SessionStarted;
    }

    public async ValueTask HandleSpeechCapturePressAsync(CancellationToken cancellationToken)
    {
        _ = await ToggleVoiceInputAsync(cancellationToken).ConfigureAwait(false);
    }

    public ValueTask<SpeechAssistantActivationState> ToggleVoiceInputAsync(CancellationToken cancellationToken)
        => _voiceInputToggle.ToggleAsync(cancellationToken);

    public ValueTask StopAsync(CancellationToken cancellationToken)
        => _lifecycle.StopAsync(cancellationToken);

    public ValueTask EnterAsync(
        SpeechAssistantSilentInputSnapshotIdentity expected,
        CancellationToken cancellationToken)
        => _silentInput.EnterAsync(expected, cancellationToken);

    public ValueTask SubmitAsync(
        SpeechAssistantSilentInputSnapshotIdentity expected,
        string value,
        CancellationToken cancellationToken)
        => _silentInput.SubmitAsync(expected, value, cancellationToken);

    public ValueTask ClearAsync(
        SpeechAssistantSilentInputSnapshotIdentity expected,
        CancellationToken cancellationToken)
        => _silentInput.ClearAsync(expected, cancellationToken);

    public ValueTask CancelAsync(
        SpeechAssistantSilentInputSnapshotIdentity expected,
        CancellationToken cancellationToken)
        => _silentInput.CancelAsync(expected, cancellationToken);

    public ValueTask DisposeAsync()
        => _lifecycle.DisposeAsync();

    /// <summary>
    /// Versucht, den Abbruch am Einstieg und die Möglichkeit eines erneuten Starts
    /// in der Sitzungssprache anzusagen. Gewöhnliche Wiedergabefehler werden protokolliert
    /// und ändern den entschiedenen Abbruch nicht. Abbruch- und Pausensignale werden weitergegeben.
    /// </summary>
    private async ValueTask SpeakEntryCancelledAsync(string isoCode, CancellationToken cancellationToken)
    {
        try
        {
            var politenessLevel = _context.SetupDependencies.UserLanguageState.SelectedPolitenessLevel
                ?? PolitenessLevel.Formal;
            var segment = SessionLanguageOfferPromptTextResolver.GetCancelledSegment(isoCode, politenessLevel);
            await _context.InteractionDependencies.TextToSpeechPlayer
                .SpeakAsync(segment.Text, segment.IsoCode, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not VoiceInputPausedException)
        {
            _context.InteractionDependencies.Logger.LogWarning(
                OrchestrationLogEvents.SessionEntryLanguageDecisionUnavailable,
                ex,
                "Session-entry cancel acknowledgment could not be spoken in {IsoCode}; the cancel stands.",
                isoCode);
        }
    }

    private void ResetRuntimeState()
        => Volatile.Write(ref _lastKnownState, SpeechAssistantState.Initial);

    private void UpdateLastKnownState(SpeechAssistantState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var previousState = Volatile.Read(ref _lastKnownState);
        Volatile.Write(ref _lastKnownState, state);
        SpeechAssistantTraceFaultObserver.Observe(
            TraceStateTransitionAsync(previousState, state),
            _context.InteractionDependencies.Logger);
    }

    private ValueTask TraceStateTransitionAsync(
        SpeechAssistantState previousState,
        SpeechAssistantState nextState)
    {
        ArgumentNullException.ThrowIfNull(previousState);
        ArgumentNullException.ThrowIfNull(nextState);

        var correlationId = string.IsNullOrWhiteSpace(nextState.CorrelationId)
            ? previousState.CorrelationId
            : nextState.CorrelationId;

        return _context.InteractionDependencies.SpeechAssistantTrace.WriteIfConfiguredAsync(
            correlationId,
            SpeechAssistantTraceScope.Reducer,
            SpeechAssistantTraceLevel.Information,
            $"State transition phase={previousState.Phase}->{nextState.Phase} dialog={previousState.ActiveDialog?.ToString() ?? "<none>"}->{nextState.ActiveDialog?.ToString() ?? "<none>"}",
            SpeechAssistantTerminalCancellation.ForRequiredTerminalWork());
    }
}
