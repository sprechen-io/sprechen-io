using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SpeechAssistantReducerTests
{
    [Fact]
    public void Reduce_WhenIdleAndUnsupportedEvent_ThrowsInvalidOperationException()
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial;

        var exception = Assert.Throws<InvalidOperationException>(() => reducer.Reduce(state, new SpeechAssistantWakeWordDetectedEvent("noise")));

        Assert.Contains("Unsupported speech assistant event", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Reduce_WhenIdleAndPressed_QueuesReturnToArmedWithModeledDefaultLanguage()
    {
        var reducer = new SpeechAssistantReducer();

        var update = reducer.Reduce(SpeechAssistantState.Initial, new SpeechAssistantPressedEvent());

        Assert.Equal(SpeechAssistantPhase.Completed, update.State.Phase);
        Assert.Null(update.State.ActiveDialog);
        Assert.Equal(VoiceInterfaceModelCatalog.DefaultLanguage, update.State.SelectedIsoCode);
        Assert.Collection(update.Effects, effect => Assert.IsType<DispatchSpeechAssistantEventEffect>(effect));
    }

    [Fact]
    public void Reduce_WhenIdleAndLanguageSelected_QueuesReturnToArmed()
    {
        var reducer = new SpeechAssistantReducer();

        var update = reducer.Reduce(SpeechAssistantState.Initial, new SpeechAssistantLanguageSelectedEvent("de-DE"));

        Assert.Equal(SpeechAssistantPhase.Completed, update.State.Phase);
        Assert.Null(update.State.ActiveDialog);
        Assert.Equal("de", update.State.SelectedIsoCode);
        Assert.Collection(
            update.Effects,
            effect => Assert.IsType<DispatchSpeechAssistantEventEffect>(effect));
    }

    [Fact]
    public void Reduce_WhenCompletedAndReturnToArmed_PlaysListeningCueBeforeWakeWordListening()
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Completed, SelectedIsoCode = "de" };

        var update = reducer.Reduce(state, new SpeechAssistantReturnToArmedEvent());

        Assert.Equal(SpeechAssistantPhase.Armed, update.State.Phase);
        Assert.Null(update.State.ActiveDialog);
        Assert.Collection(
            update.Effects,
            effect => Assert.IsType<PlayArmedListeningCueEffect>(effect),
            effect => Assert.IsType<ListenForWakeWordEffect>(effect));
    }

    [Fact]
    public void Reduce_WhenCompletedAndLanguageSelected_InterruptsReturnToArmed()
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Completed, SelectedIsoCode = "en" };

        var update = reducer.Reduce(state, new SpeechAssistantLanguageSelectedEvent("de-DE"));

        Assert.Equal(SpeechAssistantPhase.Completed, update.State.Phase);
        Assert.Null(update.State.ActiveDialog);
        Assert.Equal("de", update.State.SelectedIsoCode);
        Assert.Collection(
            update.Effects,
            effect => Assert.IsType<DispatchSpeechAssistantEventEffect>(effect));
    }

    [Fact]
    public void Reduce_WhenArmedAndWakeWordDetectedWithoutCommand_OpensFormsCatalogAnnouncement()
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Armed, SelectedIsoCode = "de" };

        var update = reducer.Reduce(state, new SpeechAssistantWakeWordDetectedEvent("Okay"));

        // Ohne Anfangstranskript startet die Katalogankündigung. Ein zusätzlicher
        // Effekt für die Aufnahmebereitschaft darf ihr hier nicht vorausgehen.
        Assert.Equal(SpeechAssistantPhase.Engaged, update.State.Phase);
        Assert.Equal(SpeechAssistantDialogKind.FormsCatalog, update.State.ActiveDialog);
        var dialogEffect = Assert.IsType<RunFormsCatalogDialogEffect>(Assert.Single(update.Effects));
        Assert.False(dialogEffect.HasInitialTranscript);
    }

    [Fact]
    public void Reduce_WhenArmedAndWakeWordDetectedWithCommand_StartsFormsCatalogDialog()
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Armed, SelectedIsoCode = "de" };

        var update = reducer.Reduce(state, new SpeechAssistantWakeWordDetectedEvent("Okay Alpha", "Alpha"));

        Assert.Equal(SpeechAssistantPhase.Engaged, update.State.Phase);
        Assert.Equal(SpeechAssistantDialogKind.FormsCatalog, update.State.ActiveDialog);
        Assert.Collection(
            update.Effects,
            effect => Assert.IsType<RunFormsCatalogDialogEffect>(effect));
    }

    [Fact]
    public void Reduce_WhenArmedAndAcousticWakeWordDetected_OpensFormsCatalogAnnouncement()
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Armed, SelectedIsoCode = "de" };
        var wakeEvent = SpeechAssistantWakeWordDetectedEvent.FromAcousticEvidence(
            new AcousticWakeWordEvidence("acoustic.okay", 0.91d, new SampleCursor(42)),
            correlationId: "corr");

        var update = reducer.Reduce(state, wakeEvent);

        // Auch akustische Aktivierung liefert kein Anfangstranskript und startet denselben Ankündigungspfad.
        Assert.Equal(SpeechAssistantPhase.Engaged, update.State.Phase);
        Assert.Equal(SpeechAssistantDialogKind.FormsCatalog, update.State.ActiveDialog);
        var dialogEffect = Assert.IsType<RunFormsCatalogDialogEffect>(Assert.Single(update.Effects));
        Assert.False(dialogEffect.HasInitialTranscript);
    }

    [Fact]
    public void Reduce_WhenApplicationSelectedWithMultipleForms_ResetsSelectedFormBeforeFormSelection()
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Engaged,
            ActiveDialog = SpeechAssistantDialogKind.FormsCatalog,
            SelectedIsoCode = "de",
            SelectedApplicationId = "old-app",
            SelectedFormId = "old-form"
        };

        var update = reducer.Reduce(state, new SpeechAssistantApplicationSelectedEvent("new-app", "New App", 2, default, "en"));

        Assert.Equal(SpeechAssistantPhase.Engaged, update.State.Phase);
        Assert.Equal(SpeechAssistantDialogKind.FormSelection, update.State.ActiveDialog);
        Assert.Equal("en", update.State.SelectedIsoCode);
        Assert.Equal("new-app", update.State.SelectedApplicationId);
        Assert.Null(update.State.SelectedFormId);
        Assert.Collection(
            update.Effects,
            effect => Assert.IsType<RunFormSelectionDialogEffect>(effect));
    }

    [Fact]
    public void Reduce_WhenApplicationSelectionCancelledAfterLanguageChange_PreservesNewIsoCodeBeforeRearm()
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Engaged,
            ActiveDialog = SpeechAssistantDialogKind.FormsCatalog,
            SelectedIsoCode = "de"
        };

        var update = reducer.Reduce(state, new SpeechAssistantApplicationSelectionCancelledEvent("en"));

        Assert.Equal(SpeechAssistantPhase.Completed, update.State.Phase);
        Assert.Null(update.State.ActiveDialog);
        Assert.Equal("en", update.State.SelectedIsoCode);
        Assert.Collection(
            update.Effects,
            effect => Assert.IsType<DispatchSpeechAssistantEventEffect>(effect));
    }

    [Fact]
    public void Reduce_WhenArmedAndLanguageSelected_CompletesAndQueuesReturnToArmed()
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Armed, SelectedIsoCode = "en" };

        var update = reducer.Reduce(state, new SpeechAssistantLanguageSelectedEvent("de-DE"));

        Assert.Equal(SpeechAssistantPhase.Completed, update.State.Phase);
        Assert.Null(update.State.ActiveDialog);
        Assert.Equal("de", update.State.SelectedIsoCode);
        Assert.Collection(
            update.Effects,
            effect => Assert.IsType<DispatchSpeechAssistantEventEffect>(effect));
    }

    [Fact]
    public void Reduce_WhenVoiceCommandCompleted_CompletesAndQueuesReturnToArmed()
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Engaged, ActiveDialog = SpeechAssistantDialogKind.VoiceCommand, SelectedIsoCode = "de" };

        var update = reducer.Reduce(
            state,
            new SpeechAssistantVoiceCommandCompletedEvent(
                VoiceCommandDialogResult.Submitted(
                    "test-app",
                    "test-form",
                    ImmutableDictionary.CreateRange(
                        StringComparer.OrdinalIgnoreCase,
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["firstName"] = "Max" }))));

        Assert.Equal(SpeechAssistantPhase.Completed, update.State.Phase);
        Assert.Null(update.State.ActiveDialog);
        Assert.Collection(update.Effects, effect => Assert.IsType<DispatchSpeechAssistantEventEffect>(effect));

        Assert.True(update.State.ApplicationSessions.ContainsKey("test-app"));
        var appSession = update.State.ApplicationSessions["test-app"];
        Assert.True(appSession.FormsById.ContainsKey("test-form"));
        Assert.True(appSession.FormsById["test-form"].IsSubmitted);
        Assert.Equal("Max", appSession.FormsById["test-form"].ValuesByVariableId["firstName"]);
    }

    [Fact]
    public void Reduce_WhenArmedAndPressed_CompletesAndQueuesReturnToArmed()
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Armed, SelectedIsoCode = "de" };

        var update = reducer.Reduce(state, new SpeechAssistantPressedEvent());

        Assert.Equal(SpeechAssistantPhase.Completed, update.State.Phase);
        Assert.Null(update.State.ActiveDialog);
        Assert.Equal("de", update.State.SelectedIsoCode);
        Assert.Collection(
            update.Effects,
            effect => Assert.IsType<DispatchSpeechAssistantEventEffect>(effect));
    }

    [Fact]
    public void Reduce_WhenArmedAndVoiceInputPaused_CapturesPauseSnapshotWithoutEffects()
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Armed, SelectedIsoCode = "de" };

        var update = reducer.Reduce(state, new SpeechAssistantVoiceInputPausedEvent());

        Assert.Equal(SpeechAssistantPhase.Paused, update.State.Phase);
        Assert.Null(update.State.ActiveDialog);
        Assert.Empty(update.Effects);
        var snapshot = Assert.IsType<SpeechAssistantPauseSnapshot>(update.State.PauseSnapshot);
        Assert.Equal(SpeechAssistantPhase.Armed, snapshot.Phase);
        Assert.Equal("de", snapshot.SelectedIsoCode);
    }

    [Fact]
    public void Reduce_WhenPausedAndVoiceInputResumedFromArmed_RestoresWakeListening()
    {
        var reducer = new SpeechAssistantReducer();
        var paused = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Paused,
            SelectedIsoCode = "de",
            PauseSnapshot = new SpeechAssistantPauseSnapshot(
                SpeechAssistantPhase.Armed,
                null,
                null,
                "de",
                null,
                null)
        };

        var update = reducer.Reduce(paused, new SpeechAssistantVoiceInputResumedEvent());

        Assert.Equal(SpeechAssistantPhase.Armed, update.State.Phase);
        Assert.Null(update.State.PauseSnapshot);
        Assert.Collection(
            update.Effects,
            effect => Assert.IsType<PlayArmedListeningCueEffect>(effect),
            effect => Assert.IsType<ListenForWakeWordEffect>(effect));
    }

    [Fact]
    public void Reduce_WhenPausedAndVoiceInputResumedFromVoiceCommand_RestoresVoiceCommandDialog()
    {
        var reducer = new SpeechAssistantReducer();
        var paused = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Paused,
            ActiveDialog = SpeechAssistantDialogKind.VoiceCommand,
            SelectedIsoCode = "de",
            SelectedApplicationId = "app",
            SelectedFormId = "form",
            CorrelationId = "corr",
            PauseSnapshot = new SpeechAssistantPauseSnapshot(
                SpeechAssistantPhase.Engaged,
                SpeechAssistantDialogKind.VoiceCommand,
                "corr",
                "de",
                "app",
                "form")
        };

        var update = reducer.Reduce(paused, new SpeechAssistantVoiceInputResumedEvent());

        Assert.Equal(SpeechAssistantPhase.Engaged, update.State.Phase);
        Assert.Equal(SpeechAssistantDialogKind.VoiceCommand, update.State.ActiveDialog);
        Assert.Null(update.State.PauseSnapshot);
        Assert.Collection(update.Effects, effect => Assert.IsType<RunVoiceCommandDialogEffect>(effect));
    }

    [Fact]
    public void Reduce_WhenPausedAndVoiceCommandPaused_MergesDictatedValuesAndKeepsPausedState()
    {
        var reducer = new SpeechAssistantReducer();
        var snapshot = new SpeechAssistantPauseSnapshot(
            SpeechAssistantPhase.Engaged,
            SpeechAssistantDialogKind.VoiceCommand,
            "corr",
            "de",
            "app",
            "form");
        var paused = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Paused,
            ActiveDialog = SpeechAssistantDialogKind.VoiceCommand,
            SelectedIsoCode = "de",
            SelectedApplicationId = "app",
            SelectedFormId = "form",
            CorrelationId = "corr",
            PauseSnapshot = snapshot
        };

        var update = reducer.Reduce(
            paused,
            new SpeechAssistantVoiceCommandPausedEvent(
                VoiceCommandDialogResult.Paused(
                    "app",
                    "form",
                    ImmutableDictionary.CreateRange(
                        StringComparer.OrdinalIgnoreCase,
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["firstName"] = "Max" }))));

        // Die neuen Werte werden im pausierten Zustand übernommen.
        // Der gespeicherte Fortsetzungszustand bleibt für den späteren Neustart erhalten.
        Assert.Equal(SpeechAssistantPhase.Paused, update.State.Phase);
        Assert.Equal(SpeechAssistantDialogKind.VoiceCommand, update.State.ActiveDialog);
        Assert.Empty(update.Effects);
        Assert.Equal(snapshot, update.State.PauseSnapshot);

        var formSession = update.State.ApplicationSessions["app"].FormsById["form"];
        Assert.Equal("Max", formSession.ValuesByVariableId["firstName"]);
        Assert.False(formSession.IsSubmitted);
    }

    [Fact]
    public void Reduce_WhenSilentInputRequestedWhilePaused_EntersTransitioningWithoutLeavingPause()
    {
        var reducer = new SpeechAssistantReducer();
        var snapshot = SpeechAssistantPauseSnapshot.Capture(SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Engaged,
            ActiveDialog = SpeechAssistantDialogKind.VoiceCommand,
            CorrelationId = "corr",
            SelectedIsoCode = "de",
            SelectedApplicationId = "app",
            SelectedFormId = "form"
        });
        var paused = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Paused,
            ActiveDialog = SpeechAssistantDialogKind.VoiceCommand,
            SelectedIsoCode = "de",
            SelectedApplicationId = "app",
            SelectedFormId = "form",
            CorrelationId = "corr",
            PauseSnapshot = snapshot
        };

        var update = reducer.Reduce(
            paused,
            new SpeechAssistantVoiceCommandPausedEvent(
                VoiceCommandDialogResult.SilentInputRequested(
                    new FormSilentInputEligibilityProjection(
                        "nameField",
                        SpeechAssistantSilentInputControlKind.Text))));

        Assert.Equal(SpeechAssistantPhase.Paused, update.State.Phase);
        Assert.Equal(1, update.State.SurfaceRevision);
        Assert.NotNull(update.State.SilentInput);
        Assert.Equal(SpeechAssistantSilentInputPhase.Transitioning, update.State.SilentInput.Phase);
        Assert.Equal("nameField", update.State.SilentInput.FieldId);
        Assert.Equal(SpeechAssistantSilentInputControlKind.Text, update.State.SilentInput.ControlKind);
        Assert.Equal(0, update.State.SilentInput.OriginSurfaceRevision);
        Assert.Equal(2, update.Effects.Count);
        Assert.IsType<PublishSilentInputSurfaceEffect>(update.Effects[0]);
        Assert.IsType<SpeakSilentInputReadyEffect>(update.Effects[1]);
    }

    [Fact]
    public void Reduce_WhenSilentInputReadyPlaybackMatches_EntersEditing()
    {
        var reducer = new SpeechAssistantReducer();
        var transitioning = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Paused,
            SurfaceRevision = 1,
            SilentInput = new SpeechAssistantFormSurfaceInteractionState(
                SpeechAssistantSilentInputPhase.Transitioning,
                "nameField",
                SpeechAssistantSilentInputControlKind.Text,
                OriginSurfaceRevision: 0,
                RepairKind: null)
        };

        var update = reducer.Reduce(
            transitioning,
            new SpeechAssistantSilentInputReadySpokenEvent(1));

        Assert.Equal(SpeechAssistantSilentInputPhase.Editing, update.State.SilentInput!.Phase);
        Assert.Equal(2, update.State.SurfaceRevision);
        Assert.Single(update.Effects);
        Assert.IsType<PublishSilentInputSurfaceEffect>(update.Effects[0]);
    }

    [Fact]
    public void Reduce_WhenSilentInputReadyPlaybackIsStale_LeavesStateUnchanged()
    {
        var reducer = new SpeechAssistantReducer();
        var transitioning = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Paused,
            SurfaceRevision = 1,
            SilentInput = new SpeechAssistantFormSurfaceInteractionState(
                SpeechAssistantSilentInputPhase.Transitioning,
                "nameField",
                SpeechAssistantSilentInputControlKind.Text,
                OriginSurfaceRevision: 0,
                RepairKind: null)
        };

        var update = reducer.Reduce(
            transitioning,
            new SpeechAssistantSilentInputReadySpokenEvent(0));

        Assert.Same(transitioning, update.State);
    }

    [Fact]
    public void Reduce_WhenSilentInputReadyPlaybackFails_ReturnsToAvailable()
    {
        var reducer = new SpeechAssistantReducer();
        var transitioning = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Paused,
            SelectedIsoCode = "de",
            SurfaceRevision = 1,
            SilentInput = new SpeechAssistantFormSurfaceInteractionState(
                SpeechAssistantSilentInputPhase.Transitioning,
                "nameField",
                SpeechAssistantSilentInputControlKind.Text,
                OriginSurfaceRevision: 0,
                RepairKind: null)
        };

        var update = reducer.Reduce(
            transitioning,
            new SpeechAssistantSilentInputReadyPlaybackFailedEvent(1));

        Assert.Equal(SpeechAssistantSilentInputPhase.Available, update.State.SilentInput!.Phase);
        Assert.Equal(2, update.State.SurfaceRevision);
    }

    [Fact]
    public void Reduce_WhenSilentInputSubmitMatches_EntersPendingAndQueuesTransaction()
    {
        var reducer = new SpeechAssistantReducer();
        var identity = new SpeechAssistantSilentInputSnapshotIdentity(
            "corr",
            "run",
            2,
            "app",
            "form",
            "nameField");
        var editing = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Paused,
            CorrelationId = "corr",
            SelectedIsoCode = "de",
            SelectedApplicationId = "app",
            SelectedFormId = "form",
            SessionId = "run",
            SurfaceRevision = 2,
            SilentInput = new SpeechAssistantFormSurfaceInteractionState(
                SpeechAssistantSilentInputPhase.Editing,
                "nameField",
                SpeechAssistantSilentInputControlKind.Text,
                OriginSurfaceRevision: 0,
                RepairKind: null)
        };

        var update = reducer.Reduce(
            editing,
            new SpeechAssistantSilentInputSubmitRequestedEvent(identity, "Ada"));

        Assert.Equal(SpeechAssistantSilentInputPhase.Pending, update.State.SilentInput!.Phase);
        Assert.False(string.IsNullOrWhiteSpace(update.State.SilentInputOperationId));
        Assert.Equal(2, update.Effects.Count);
        Assert.IsType<PublishSilentInputSurfaceEffect>(update.Effects[0]);
        Assert.IsType<ExecuteFormSilentInputTransactionEffect>(update.Effects[1]);
    }

    [Fact]
    public void Reduce_WhenSilentInputSubmitIsStale_LeavesStateUnchanged()
    {
        var reducer = new SpeechAssistantReducer();
        var editing = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Paused,
            CorrelationId = "corr",
            SelectedIsoCode = "de",
            SelectedApplicationId = "app",
            SelectedFormId = "form",
            SessionId = "run",
            SurfaceRevision = 2,
            SilentInput = new SpeechAssistantFormSurfaceInteractionState(
                SpeechAssistantSilentInputPhase.Editing,
                "nameField",
                SpeechAssistantSilentInputControlKind.Text,
                OriginSurfaceRevision: 0,
                RepairKind: null)
        };

        var update = reducer.Reduce(
            editing,
            new SpeechAssistantSilentInputSubmitRequestedEvent(
                new SpeechAssistantSilentInputSnapshotIdentity(
                    "corr",
                    "run",
                    1,
                    "app",
                    "form",
                    "nameField"),
                "Ada"));

        Assert.Same(editing, update.State);
        Assert.Empty(update.Effects);
    }

    [Fact]
    public void Reduce_WhenSilentInputApplied_DispatchesAcceptedEvent()
    {
        var reducer = new SpeechAssistantReducer();
        var snapshot = new FormSurfaceSnapshot();
        var pending = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Paused,
            CorrelationId = "corr",
            SelectedIsoCode = "de",
            SelectedApplicationId = "app",
            SelectedFormId = "form",
            SessionId = "run",
            SurfaceRevision = 3,
            SilentInputOperationId = "op",
            SilentInputPendingIntent = FormSilentInputIntentKind.Replace,
            SilentInput = new SpeechAssistantFormSurfaceInteractionState(
                SpeechAssistantSilentInputPhase.Pending,
                "nameField",
                SpeechAssistantSilentInputControlKind.Text,
                OriginSurfaceRevision: 1,
                RepairKind: null)
        };

        var update = reducer.Reduce(
            pending,
            new SpeechAssistantSilentInputTransactionCompletedEvent(
                "op",
                FormSilentInputTransactionResult.Applied(
                    SpeechAssistantApplicationSessionState.Empty,
                    canonicalSame: false,
                    snapshot,
                    new FormSilentInputEligibilityProjection(
                        "nameField",
                        SpeechAssistantSilentInputControlKind.Text))));

        Assert.Equal(SpeechAssistantSilentInputPhase.Available, update.State.SilentInput!.Phase);
        Assert.Equal(2, update.Effects.Count);
        var accepted = Assert.IsType<DispatchSilentInputAcceptedEffect>(update.Effects[1]);
        Assert.Equal(FormSilentInputAcceptedIntentKind.Replace, accepted.Accepted.IntentKind);
        Assert.Equal("op", accepted.Accepted.OperationId);
        Assert.Equal(1, accepted.Accepted.OriginSurfaceRevision);
        Assert.Equal(4, accepted.Accepted.PublishedSurfaceRevision);
    }

    [Fact]
    public void Reduce_WhenVoiceCommandPausedOutsidePausedPhase_ReducesAsNoOpWithoutMergeOrFault()
    {
        var reducer = new SpeechAssistantReducer();
        var engaged = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Engaged,
            ActiveDialog = SpeechAssistantDialogKind.VoiceCommand,
            SelectedIsoCode = "de",
            SelectedApplicationId = "app",
            SelectedFormId = "form"
        };

        var update = reducer.Reduce(
            engaged,
            new SpeechAssistantVoiceCommandPausedEvent(
                VoiceCommandDialogResult.Paused(
                    "app",
                    "form",
                    ImmutableDictionary.CreateRange(
                        StringComparer.OrdinalIgnoreCase,
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["firstName"] = "Max" }))));

        // Nach Verlassen der Pause darf ein verspätetes Werteereignis
        // weder Werte übernehmen noch neue Effekte auslösen.
        Assert.Same(engaged, update.State);
        Assert.Empty(update.Effects);
        Assert.False(update.State.ApplicationSessions.ContainsKey("app"));
    }

    [Fact]
    public void Reduce_WhenIdleAndVoiceInputPaused_ReducesAsNoOp()
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial;

        var update = reducer.Reduce(state, new SpeechAssistantVoiceInputPausedEvent());

        Assert.Same(state, update.State);
        Assert.Empty(update.Effects);
    }

    [Fact]
    public void Reduce_WhenCompletedAndVoiceInputPaused_ReducesAsNoOp()
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Completed, SelectedIsoCode = "de" };

        var update = reducer.Reduce(state, new SpeechAssistantVoiceInputPausedEvent());

        Assert.Same(state, update.State);
        Assert.Empty(update.Effects);
    }

    [Fact]
    public void Reduce_WhenPausedAndVoiceInputPaused_ReducesAsNoOp()
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Paused, SelectedIsoCode = "de" };

        var update = reducer.Reduce(state, new SpeechAssistantVoiceInputPausedEvent());

        Assert.Same(state, update.State);
        Assert.Empty(update.Effects);
    }

    public static TheoryData<SpeechAssistantEvent> LateWakeRuntimeEvents() => new()
    {
        new SpeechAssistantWakeWordDetectedEvent("Okay"),
        new SpeechAssistantWakeWordMissedEvent(nameof(WakeWordActivationStatus.MissingLeadingWakeWord), 38, "corr"),
        new SpeechAssistantWakeWordRecognitionFailedEvent("queue-full"),
        new SpeechAssistantWakeWordListenerFailedEvent()
    };

    [Theory]
    [MemberData(nameof(LateWakeRuntimeEvents))]
    public void Reduce_WhenCompletedAndLateWakeRuntimeEvent_ReducesAsNoOp(SpeechAssistantEvent wakeEvent)
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Completed, SelectedIsoCode = "de" };

        var update = reducer.Reduce(state, wakeEvent);

        Assert.Same(state, update.State);
        Assert.Empty(update.Effects);
    }

    [Theory]
    [MemberData(nameof(LateWakeRuntimeEvents))]
    public void Reduce_WhenPausedAndLateWakeRuntimeEvent_ReducesAsNoOp(SpeechAssistantEvent wakeEvent)
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Paused, SelectedIsoCode = "de" };

        var update = reducer.Reduce(state, wakeEvent);

        Assert.Same(state, update.State);
        Assert.Empty(update.Effects);
    }

    [Fact]
    public void Reduce_WhenPausedAndStartupLanguageSelected_ReducesAsNoOp()
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Paused, SelectedIsoCode = "de" };

        var update = reducer.Reduce(state, new SpeechAssistantLanguageSelectedEvent("en"));

        Assert.Same(state, update.State);
        Assert.Empty(update.Effects);
    }

    [Theory]
    [InlineData(SpeechAssistantDialogKind.VoiceCommand)]
    [InlineData(SpeechAssistantDialogKind.FormsCatalog)]
    [InlineData(SpeechAssistantDialogKind.FormSelection)]
    public void Reduce_WhenEngagedAndStartupLanguageSelected_ReducesAsNoOp(SpeechAssistantDialogKind dialog)
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Engaged,
            ActiveDialog = dialog,
            SelectedIsoCode = "de",
            SelectedApplicationId = "app",
            SelectedFormId = "form"
        };

        var update = reducer.Reduce(state, new SpeechAssistantLanguageSelectedEvent("en"));

        Assert.Same(state, update.State);
        Assert.Empty(update.Effects);
    }

    [Fact]
    public void Reduce_WhenArmedAndWakeWordListenerFailed_CompletesAndQueuesReturnToArmed()
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Armed, SelectedIsoCode = "de" };

        var update = reducer.Reduce(state, new SpeechAssistantWakeWordListenerFailedEvent());

        Assert.Equal(SpeechAssistantPhase.Completed, update.State.Phase);
        Assert.Null(update.State.ActiveDialog);
        Assert.Equal("de", update.State.SelectedIsoCode);
        Assert.Collection(
            update.Effects,
            effect => Assert.IsType<DispatchSpeechAssistantEventEffect>(effect));
    }

    [Fact]
    public void Reduce_WhenArmedAndWakeWordMissed_KeepsStateAndProducesNoEffect()
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Armed, SelectedIsoCode = "de" };

        var update = reducer.Reduce(
            state,
            new SpeechAssistantWakeWordMissedEvent(
                status: nameof(WakeWordActivationStatus.MissingLeadingWakeWord),
                confidencePercent: 38,
                correlationId: "abc"));

        Assert.Equal(SpeechAssistantPhase.Armed, update.State.Phase);
        Assert.Equal("de", update.State.SelectedIsoCode);
        Assert.Empty(update.Effects);
    }

    [Fact]
    public void Reduce_WhenFormsCatalogEngagedAndUnsupportedEvent_ThrowsInvalidOperationException()
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Engaged,
            ActiveDialog = SpeechAssistantDialogKind.FormsCatalog,
            SelectedIsoCode = "de"
        };

        var exception = Assert.Throws<InvalidOperationException>(() => reducer.Reduce(state, new SpeechAssistantPressedEvent()));

        Assert.Contains("Unsupported speech assistant event", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Reduce_WhenFormSelectionEngagedAndUnsupportedEvent_ThrowsInvalidOperationException()
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Engaged,
            ActiveDialog = SpeechAssistantDialogKind.FormSelection,
            SelectedIsoCode = "de"
        };

        var exception = Assert.Throws<InvalidOperationException>(() => reducer.Reduce(state, new SpeechAssistantPressedEvent()));

        Assert.Contains("Unsupported speech assistant event", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Reduce_WhenVoiceCommandEngagedAndUnsupportedEvent_ThrowsInvalidOperationException()
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Engaged, ActiveDialog = SpeechAssistantDialogKind.VoiceCommand, SelectedIsoCode = "de" };

        var exception = Assert.Throws<InvalidOperationException>(() => reducer.Reduce(state, new SpeechAssistantPressedEvent()));

        Assert.Contains("Unsupported speech assistant event", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Reduce_WhenCompletedAndUnsupportedEvent_ThrowsInvalidOperationException()
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Completed, SelectedIsoCode = "de" };

        var exception = Assert.Throws<InvalidOperationException>(() => reducer.Reduce(state, new SpeechAssistantPressedEvent()));

        Assert.Contains("Unsupported speech assistant event", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Reduce_WhenUnsupportedPhase_ThrowsInvalidOperationException()
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with { Phase = (SpeechAssistantPhase)123, SelectedIsoCode = "de" };

        var exception = Assert.Throws<InvalidOperationException>(() => reducer.Reduce(state, new SpeechAssistantPressedEvent()));

        Assert.Contains("Unsupported speech assistant phase", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Reduce_WhenEngagedWithUnknownDialog_ThrowsInvalidOperationException()
    {
        var reducer = new SpeechAssistantReducer();
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Engaged, ActiveDialog = (SpeechAssistantDialogKind)123, SelectedIsoCode = "de" };

        var exception = Assert.Throws<InvalidOperationException>(() => reducer.Reduce(state, new SpeechAssistantPressedEvent()));

        Assert.Contains("Unsupported speech assistant dialog", exception.Message, StringComparison.Ordinal);
    }
}
