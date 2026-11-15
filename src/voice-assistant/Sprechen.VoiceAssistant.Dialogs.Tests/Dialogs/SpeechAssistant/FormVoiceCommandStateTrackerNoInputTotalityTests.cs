using Moq;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

// Prüft die Rückkehr zu Prompting nach ausbleibender Eingabe aus ErrorPrompt, Repair und Listening.
// Aus ErrorPrompt und Repair ist dafür kein erneuter Übergang nach Listening nötig.
public sealed class FormVoiceCommandStateTrackerNoInputTotalityTests
{
    [Fact]
    public void RecordNoInputTimeout_WhenCarriedErrorPrompt_NormalizesToPromptingWithoutThrowing()
    {
        var tracker = CreateTracker();
        tracker.BeginFieldPrompt("group");
        tracker.BeginListening();
        tracker.BeginInterpretation();
        tracker.BeginValidation();
        tracker.BeginErrorPrompt();
        Assert.Equal(DialogMicroStateIds.ErrorPrompt, tracker.CurrentMicroState);

        tracker.RecordNoInputTimeout();

        Assert.Equal(DialogMicroStateIds.Prompting, tracker.CurrentMicroState);
    }

    [Fact]
    public void RecordNoInputTimeout_WhenCarriedRepair_NormalizesToPromptingWithoutThrowing()
    {
        var tracker = CreateTracker();
        tracker.BeginFieldPrompt("group");
        tracker.BeginListening();
        tracker.BeginInterpretation();
        tracker.BeginValidation();
        tracker.BeginFieldConfirmation();
        tracker.BeginFieldRepair();
        Assert.Equal(DialogMicroStateIds.Repair, tracker.CurrentMicroState);

        tracker.RecordNoInputTimeout();

        Assert.Equal(DialogMicroStateIds.Prompting, tracker.CurrentMicroState);
    }

    [Fact]
    public void RecordNoInputTimeout_WhenArmedListening_TakesTheModeledListeningToPromptingEdge()
    {
        var tracker = CreateTracker();
        tracker.BeginFieldPrompt("group");
        tracker.BeginListening();
        Assert.Equal(DialogMicroStateIds.Listening, tracker.CurrentMicroState);

        tracker.RecordNoInputTimeout();

        Assert.Equal(DialogMicroStateIds.Prompting, tracker.CurrentMicroState);
    }

    private static FormVoiceCommandStateTracker CreateTracker()
    {
        var telemetry = new Mock<IEvaluationSessionTelemetry>(MockBehavior.Loose);
        var session = new EvaluationSession("totality-session", EvaluationChannel.Voice, "app", "app/form");
        return new FormVoiceCommandStateTracker(telemetry.Object, session);
    }
}
