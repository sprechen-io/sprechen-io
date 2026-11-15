namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SpeechAssistantArmedReducerTests
{
    [Fact]
    public void Reduce_WhenWakeRecognitionFailed_CompletesAndQueuesReturnToArmed()
    {
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Armed, SelectedIsoCode = "de" };

        var update = SpeechAssistantArmedReducer.Reduce(
            state,
            new SpeechAssistantWakeWordRecognitionFailedEvent(SpeechRecognitionBackoffExhaustedException.CodeValue));

        Assert.Equal(SpeechAssistantPhase.Completed, update.State.Phase);
        Assert.Null(update.State.ActiveDialog);
        Assert.Equal("de", update.State.SelectedIsoCode);
        Assert.Collection(update.Effects, effect => Assert.IsType<DispatchSpeechAssistantEventEffect>(effect));
    }

    [Fact]
    public void Reduce_WhenWakeMissed_KeepsStateAndEmitsNoEffects()
    {
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Armed, SelectedIsoCode = "de" };

        var update = SpeechAssistantArmedReducer.Reduce(
            state,
            new SpeechAssistantWakeWordMissedEvent(
                status: nameof(WakeWordActivationStatus.MissingLeadingWakeWord),
                confidencePercent: 38,
                correlationId: "corr"));

        Assert.Equal(state, update.State);
        Assert.Empty(update.Effects);
    }

    [Fact]
    public void Reduce_WhenWakeWordHasCommand_StartsFormsCatalogWithCorrelation()
    {
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Armed, SelectedIsoCode = "de" };

        var update = SpeechAssistantArmedReducer.Reduce(
            state,
            SpeechAssistantWakeWordDetectedEvent.FromLexicalEvidence(
                "Okay Alpha",
                "Alpha",
                "corr",
                confidencePercent: 91));

        Assert.Equal(SpeechAssistantPhase.Engaged, update.State.Phase);
        Assert.Equal(SpeechAssistantDialogKind.FormsCatalog, update.State.ActiveDialog);
        Assert.Equal("corr", update.State.CorrelationId);
        var dialogEffect = Assert.IsType<RunFormsCatalogDialogEffect>(Assert.Single(update.Effects));
        Assert.Equal(91, dialogEffect.InitialTranscriptConfidencePercent);
    }

    [Fact]
    public void Reduce_WhenWakeWordHasNoCommand_OpensFormsCatalogAnnouncementWithoutPrematureReadyCue()
    {
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Armed, SelectedIsoCode = "de" };

        var update = SpeechAssistantArmedReducer.Reduce(
            state,
            new SpeechAssistantWakeWordDetectedEvent("Okay", commandText: null, correlationId: "corr"));

        // Ein reines Aktivierungswort startet den Katalog ohne Anfangstranskript.
        // Damit bleibt dessen Ankündigungsphase zuständig; hier wird noch kein
        // zusätzlicher Effekt für die Aufnahmebereitschaft angefordert.
        Assert.Equal(SpeechAssistantPhase.Engaged, update.State.Phase);
        Assert.Equal(SpeechAssistantDialogKind.FormsCatalog, update.State.ActiveDialog);
        Assert.Equal("corr", update.State.CorrelationId);
        var dialogEffect = Assert.IsType<RunFormsCatalogDialogEffect>(Assert.Single(update.Effects));
        Assert.False(dialogEffect.HasInitialTranscript);
    }

    [Fact]
    public void Reduce_WhenStudySessionBound_StartsStudyFormInsteadOfCatalog()
    {
        var state = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Armed,
            SelectedIsoCode = "de",
            StudySession = new StudySession("study.umfrage", "hintergrund-und-setup")
        };

        var update = SpeechAssistantArmedReducer.Reduce(
            state,
            new SpeechAssistantWakeWordDetectedEvent("Okay", commandText: null, correlationId: "corr"));

        Assert.Equal(SpeechAssistantPhase.Engaged, update.State.Phase);
        Assert.Equal(SpeechAssistantDialogKind.VoiceCommand, update.State.ActiveDialog);
        Assert.Equal("study.umfrage", update.State.SelectedApplicationId);
        Assert.Equal("hintergrund-und-setup", update.State.SelectedFormId);
        var dialogEffect = Assert.IsType<RunVoiceCommandDialogEffect>(Assert.Single(update.Effects));
        Assert.Equal("study.umfrage", dialogEffect.SelectedApplicationId);
        Assert.Equal("hintergrund-und-setup", dialogEffect.SelectedFormId);
        Assert.Equal(SpeechAssistantApplicationSessionState.Empty, dialogEffect.ApplicationSession);
    }

    [Fact]
    public void Reduce_WithoutStudySession_StillOpensCatalog()
    {
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Armed, SelectedIsoCode = "de" };

        var update = SpeechAssistantArmedReducer.Reduce(
            state,
            new SpeechAssistantWakeWordDetectedEvent("Okay", commandText: null, correlationId: "corr"));

        Assert.Equal(SpeechAssistantDialogKind.FormsCatalog, update.State.ActiveDialog);
        Assert.IsType<RunFormsCatalogDialogEffect>(Assert.Single(update.Effects));
    }
}
