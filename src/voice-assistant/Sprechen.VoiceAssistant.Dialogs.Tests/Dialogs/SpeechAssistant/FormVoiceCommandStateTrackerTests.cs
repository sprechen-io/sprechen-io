using Moq;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandStateTrackerTests
{
    [Fact]
    public void FieldGroupPromptState_TracksIntroCompletionAndNextGroupEntry()
    {
        var state = new FormVoiceCommandFieldGroupPromptState();

        Assert.True(state.ShouldAnnounceFieldGroupIntro("group-a"));

        state.BeginFieldPrompt("group-a");

        Assert.Equal(DialogMacroStateIds.FieldLoop, state.CurrentMacroState);
        Assert.False(state.ShouldAnnounceFieldGroupIntro("group-a"));
        Assert.True(state.ShouldAnnounceFieldGroupIntro("group-b"));
    }

    [Fact]
    public void RecordSubmitted_FromFieldGroupIntro_TransitionsToSubmittedWithoutThrowing()
    {
        var state = new FormVoiceCommandFieldGroupPromptState();

        Assert.Equal(DialogMacroStateIds.FieldGroupIntro, state.CurrentMacroState);

        state.RecordSubmitted();

        Assert.Equal(DialogMacroStateIds.Submitted, state.CurrentMacroState);
    }

    [Fact]
    public void RecordSubmitted_FromFieldLoop_TransitionsToSubmitted()
    {
        var state = new FormVoiceCommandFieldGroupPromptState();
        state.BeginFieldPrompt("group-a");
        Assert.Equal(DialogMacroStateIds.FieldLoop, state.CurrentMacroState);

        state.RecordSubmitted();

        Assert.Equal(DialogMacroStateIds.Submitted, state.CurrentMacroState);
    }

    [Fact]
    public void BeginFieldPrompt_CompletesFieldGroupIntroAndEmitsPrompting()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var session = new EvaluationSession("session-1", EvaluationChannel.Voice, "app", "app/form");
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Prompting));

        var tracker = new FormVoiceCommandStateTracker(telemetry.Object, session);

        tracker.BeginFieldPrompt("group-a");

        Assert.Equal(DialogMacroStateIds.FieldLoop, tracker.CurrentMacroState);
        Assert.Equal(DialogMicroStateIds.Prompting, tracker.CurrentMicroState);
        Assert.Equal(DialogStates.Prompting, tracker.CurrentTelemetryState);
        telemetry.VerifyAll();
    }

    [Fact]
    public void ShouldAnnounceFieldGroupIntro_IsTrueOnlyForRealFieldGroupEntries()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var session = new EvaluationSession("session-entry", EvaluationChannel.Voice, "app", "app/form");
        var tracker = new FormVoiceCommandStateTracker(telemetry.Object, session);

        Assert.True(tracker.ShouldAnnounceFieldGroupIntro("group-a"));

        tracker.BeginFieldPrompt("group-a");

        Assert.False(tracker.ShouldAnnounceFieldGroupIntro("group-a"));
        Assert.True(tracker.ShouldAnnounceFieldGroupIntro("group-b"));
    }

    [Fact]
    public void StructuralTeachingReteachMarker_ConsumesOnlyTheMatchingFieldAndThenClears()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var session = new EvaluationSession("session-reteach", EvaluationChannel.Voice, "app", "app/form");
        var tracker = new FormVoiceCommandStateTracker(telemetry.Object, session);

        tracker.ArmStructuralTeachingReteach("field-a");

        Assert.False(tracker.ConsumeStructuralTeachingReteach("field-b"));
        Assert.False(tracker.ConsumeStructuralTeachingReteach(" field-a "));
        Assert.True(tracker.ConsumeStructuralTeachingReteach("field-a"));
        Assert.False(tracker.ConsumeStructuralTeachingReteach("field-a"));
    }

    [Fact]
    public void StructuralTeachingReteachMarker_ReArmOverwritesThePendingField()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var session = new EvaluationSession("session-reteach-overwrite", EvaluationChannel.Voice, "app", "app/form");
        var tracker = new FormVoiceCommandStateTracker(telemetry.Object, session);

        tracker.ArmStructuralTeachingReteach("field-a");
        tracker.ArmStructuralTeachingReteach("field-b");

        Assert.False(tracker.ConsumeStructuralTeachingReteach("field-a"));
        Assert.True(tracker.ConsumeStructuralTeachingReteach("field-b"));
    }

    [Fact]
    public void MainFieldFlow_TracksMicroStatesThroughConfirmation()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var session = new EvaluationSession("session-2", EvaluationChannel.Voice, "app", "app/form");
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Prompting));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Listening));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Interpreting));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Validating));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Confirmation));

        var tracker = new FormVoiceCommandStateTracker(telemetry.Object, session);

        tracker.BeginFieldPrompt("group-a");
        tracker.BeginListening();
        tracker.BeginInterpretation();
        tracker.BeginValidation();
        tracker.BeginFieldConfirmation();
        tracker.CompleteConfirmedField();

        Assert.Null(tracker.CurrentMicroState);
        Assert.Equal(DialogMacroStateIds.FieldLoop, tracker.CurrentMacroState);
        Assert.Equal(DialogStates.Confirmation, tracker.CurrentTelemetryState);
        telemetry.VerifyAll();
    }

    [Fact]
    public void RecordAutoAdvanceToNextGroup_ReentersFieldLoopOnNextPrompt()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var session = new EvaluationSession("session-3", EvaluationChannel.Voice, "app", "app/form");
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Prompting));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Listening));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Interpreting));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Validating));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Confirmation));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Prompting));

        var tracker = new FormVoiceCommandStateTracker(telemetry.Object, session);

        tracker.BeginFieldPrompt("group-a");
        tracker.BeginListening();
        tracker.BeginInterpretation();
        tracker.BeginValidation();
        tracker.BeginFieldConfirmation();
        tracker.RecordAutoAdvanceToNextGroup("group-b");
        tracker.BeginFieldPrompt("group-b");

        Assert.Equal(DialogMacroStateIds.FieldLoop, tracker.CurrentMacroState);
        Assert.Equal(DialogMicroStateIds.Prompting, tracker.CurrentMicroState);
        Assert.Equal(DialogStates.Prompting, tracker.CurrentTelemetryState);
        telemetry.VerifyAll();
    }

    [Fact]
    public void RecordSubmitted_TransitionsMacroStateToSubmitted()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var session = new EvaluationSession("session-4", EvaluationChannel.Voice, "app", "app/form");
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Prompting));

        var tracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        tracker.BeginFieldPrompt("group-a");

        tracker.RecordSubmitted();

        Assert.Equal(DialogMacroStateIds.Submitted, tracker.CurrentMacroState);
        telemetry.VerifyAll();
    }

    [Fact]
    public void BeginListening_WhenAlreadyListening_ReArmsWithoutThrowing()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var session = new EvaluationSession("session-listen-rearm", EvaluationChannel.Voice, "app", "app/form");
        var tracker = new FormVoiceCommandStateTracker(telemetry.Object, session);

        tracker.BeginFieldPrompt("group-a");
        tracker.BeginListening();
        tracker.BeginListening();

        Assert.Equal(DialogMicroStateIds.Listening, tracker.CurrentMicroState);
    }

    [Fact]
    public void BeginListening_WhenPromptWasSkipped_Throws()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var session = new EvaluationSession("session-5", EvaluationChannel.Voice, "app", "app/form");
        var tracker = new FormVoiceCommandStateTracker(telemetry.Object, session);

        var exception = Assert.Throws<InvalidOperationException>(() => tracker.BeginListening());

        Assert.Contains("Dialog micro state is not set", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BeginFieldConfirmation_WhenValidationWasSkipped_Throws()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var session = new EvaluationSession("session-6", EvaluationChannel.Voice, "app", "app/form");
        var tracker = new FormVoiceCommandStateTracker(telemetry.Object, session);

        tracker.BeginFieldPrompt("group-a");
        tracker.BeginListening();
        tracker.BeginInterpretation();

        var exception = Assert.Throws<InvalidOperationException>(() => tracker.BeginFieldConfirmation());

        Assert.Contains("Unsupported dialog micro transition", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompleteConfirmedField_WhenConfirmationWasSkipped_Throws()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var session = new EvaluationSession("session-7", EvaluationChannel.Voice, "app", "app/form");
        var tracker = new FormVoiceCommandStateTracker(telemetry.Object, session);

        tracker.BeginFieldPrompt("group-a");

        var exception = Assert.Throws<InvalidOperationException>(() => tracker.CompleteConfirmedField());

        Assert.Contains("Unsupported dialog micro transition", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RecordNoInputTimeout_ReentersPromptingAndAllowsRetryCapture()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var session = new EvaluationSession("session-8", EvaluationChannel.Voice, "app", "app/form");
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Prompting));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Listening));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Prompting));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Listening));

        var tracker = new FormVoiceCommandStateTracker(telemetry.Object, session);

        tracker.BeginFieldPrompt("group-a");
        tracker.BeginListening();
        tracker.RecordNoInputTimeout();
        tracker.BeginFieldPrompt("group-a");
        tracker.BeginListening();

        Assert.Equal(DialogMicroStateIds.Listening, tracker.CurrentMicroState);
        Assert.Equal(DialogStates.Listening, tracker.CurrentTelemetryState);
        telemetry.VerifyAll();
    }

    [Fact]
    public void BeginFieldRepair_AllowsRetryListeningWithoutPromptReset()
    {
        // Der erneute Aufnahmeversuch durchläuft nach Confirmation die Zustände Repair und Prompting.
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var session = new EvaluationSession("session-9", EvaluationChannel.Voice, "app", "app/form");
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Prompting));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Listening));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Interpreting));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Validating));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Confirmation));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Repair));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Listening));

        var tracker = new FormVoiceCommandStateTracker(telemetry.Object, session);

        tracker.BeginFieldPrompt("group-a");
        tracker.BeginListening();
        tracker.BeginInterpretation();
        tracker.BeginValidation();
        tracker.BeginFieldConfirmation();
        tracker.BeginFieldRepair();
        tracker.BeginFieldPrompt("group-a");
        tracker.BeginListening();

        Assert.Equal(DialogMicroStateIds.Listening, tracker.CurrentMicroState);
        Assert.Equal(DialogStates.Listening, tracker.CurrentTelemetryState);
        telemetry.VerifyAll();
    }

    [Fact]
    public void BeginFieldPromptRestart_PreparesRetryWithoutHiddenReset()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var session = new EvaluationSession("session-10", EvaluationChannel.Voice, "app", "app/form");
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Prompting));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Listening));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Interpreting));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Prompting));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Listening));

        var tracker = new FormVoiceCommandStateTracker(telemetry.Object, session);

        tracker.BeginFieldPrompt("group-a");
        tracker.BeginListening();
        tracker.BeginInterpretation();
        tracker.BeginFieldPromptRestart();
        tracker.BeginFieldPrompt("group-a");
        tracker.BeginListening();

        Assert.Equal(DialogMicroStateIds.Listening, tracker.CurrentMicroState);
        Assert.Equal(DialogStates.Listening, tracker.CurrentTelemetryState);
        telemetry.VerifyAll();
    }

    [Fact]
    public void BeginValidation_AfterErrorPromptRelisten_ReachesValidating()
    {
        // Nach einer unverstandenen Eingabe steht die nächste Aufnahme im Zuhören.
        // Die Wertübernahme muss über den modellierten Sprachzugang in die Prüfung gelangen.
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var session = new EvaluationSession("session-error-relisten", EvaluationChannel.Voice, "app", "app/form");
        var tracker = new FormVoiceCommandStateTracker(telemetry.Object, session);

        tracker.BeginFieldPrompt("group-a");
        tracker.BeginListening();
        tracker.BeginInterpretation();
        tracker.BeginErrorPrompt();
        tracker.BeginListening();
        Assert.Equal(DialogMicroStateIds.Listening, tracker.CurrentMicroState);

        tracker.BeginValidation();

        Assert.Equal(DialogMicroStateIds.Validating, tracker.CurrentMicroState);
        Assert.Equal(DialogStates.Validating, tracker.CurrentTelemetryState);
    }

    [Theory]
    [InlineData("prompting")]
    [InlineData("error-prompt")]
    [InlineData("confirmation")]
    [InlineData("repair")]
    [InlineData("unset")]
    public void BeginValidation_FromIllegalSources_Throws(string source)
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var session = new EvaluationSession($"session-validate-from-{source}", EvaluationChannel.Voice, "app", "app/form");
        var tracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        DriveTrackerToIllegalValidationSource(tracker, source);

        var exception = Assert.Throws<InvalidOperationException>(tracker.BeginValidation);

        Assert.Contains(
            source == "unset" ? "Dialog micro state is not set" : "Unsupported dialog micro transition",
            exception.Message,
            StringComparison.Ordinal);
    }

    private static void DriveTrackerToIllegalValidationSource(FormVoiceCommandStateTracker tracker, string source)
    {
        if (source == "unset")
        {
            return;
        }

        tracker.BeginFieldPrompt("group-a");
        if (source == "prompting")
        {
            return;
        }

        tracker.BeginListening();
        tracker.BeginInterpretation();
        if (source == "error-prompt")
        {
            tracker.BeginErrorPrompt();
            return;
        }

        tracker.BeginValidation();
        tracker.BeginFieldConfirmation();
        if (source == "confirmation")
        {
            return;
        }

        if (source == "repair")
        {
            tracker.BeginFieldRepair();
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(source), source, "Unknown illegal validation source.");
    }
}
