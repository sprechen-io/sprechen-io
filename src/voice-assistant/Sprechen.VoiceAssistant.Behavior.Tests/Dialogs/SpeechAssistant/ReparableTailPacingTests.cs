using Moq;
using Sprechen.Speech.App.Contracts.Evaluation;
using Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;
using Xunit;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft den Wechsel vom ausführlichen Korrekturhinweis zur kurzen Erinnerung anhand des Bestätigungszählers.
/// Der Zustandsverlauf zählt vor der Auswahlentscheidung; die ersten beiden Bestätigungen verwenden den ausführlichen Hinweis, die dritte den kurzen.
/// </summary>
public sealed class ReparableTailPacingTests
{
    [Fact]
    public void FirstTurnsSpeakFullTail_SubsequentTurnsSpeakShortTail()
    {
        var session = new EvaluationSession("session", EvaluationChannel.Voice, "app", "app/form");
        var tracker = new FormVoiceCommandStateTracker(Mock.Of<IEvaluationSessionTelemetry>(), session);

        // Drei Felder nacheinander bestätigen und die Auswahl nach BeginFieldConfirmation prüfen, das den Zähler bereits erhöht hat.
        var usesFullTailByConfirmation = new List<bool>();
        for (var field = 0; field < 3; field++)
        {
            EnterFieldConfirmation(tracker);
            usesFullTailByConfirmation.Add(
                FormVoiceCommandConfirmationValuePresenter.UsesFullReparableTail(tracker.ReparableConfirmationTurnCount));
            tracker.CompleteConfirmedField();
        }

        // Die ersten beiden Bestätigungen verwenden den ausführlichen Hinweis, die dritte den kurzen.
        Assert.Equal([true, true, false], usesFullTailByConfirmation);
        Assert.Equal(3, tracker.ReparableConfirmationTurnCount);
    }

    [Fact]
    public void FullReparableTailTurns_GatesTheBoundaryInclusively()
    {
        // Beim Lesen beginnt der Zähler mit eins; die Grenze FullReparableTailTurns gehört noch zum ausführlichen Hinweis.
        Assert.True(FormVoiceCommandConfirmationValuePresenter.UsesFullReparableTail(1));
        Assert.True(FormVoiceCommandConfirmationValuePresenter.UsesFullReparableTail(
            FormVoiceCommandConfirmationValuePresenter.FullReparableTailTurns));
        Assert.False(FormVoiceCommandConfirmationValuePresenter.UsesFullReparableTail(
            FormVoiceCommandConfirmationValuePresenter.FullReparableTailTurns + 1));
    }

    // Durchläuft die Feldzustände bis zur Bestätigung, bei deren Beginn der Zähler steigt.
    private static void EnterFieldConfirmation(FormVoiceCommandStateTracker tracker)
    {
        tracker.BeginFieldPrompt("group");
        tracker.BeginListening();
        tracker.BeginInterpretation();
        tracker.BeginValidation();
        tracker.BeginFieldConfirmation();
    }
}
