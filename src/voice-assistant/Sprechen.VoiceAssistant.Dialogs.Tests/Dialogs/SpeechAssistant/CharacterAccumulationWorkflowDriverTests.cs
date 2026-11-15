using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Immutable;
using System.Text.Json;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft den gemeinsamen Ablauf für zusammengesetzte Eingaben mit einer vorgegebenen Strategie.
/// Bei vorhandenem Inhalt fragt die erste Meldung ohne Sprache erneut nach; erst die zweite
/// aufeinanderfolgende Meldung oder ein Abschlussbefehl beendet die Eingabe.
/// Fehler des Erkenners werden getrennt behandelt und dürfen keinen Teilwert übernehmen.
/// </summary>
public sealed class CharacterAccumulationWorkflowDriverTests
{
    [Fact]
    public async Task NonEmptyBufferSilence_RePromptsThenFinalizesOnSecondSilence_NeverDiscards()
    {
        // Bei vorhandenem Inhalt führt die erste Meldung ohne Sprache zur Nachfrage.
        // Erst die zweite aufeinanderfolgende Meldung beendet die Eingabe mit dem bisherigen Wert.
        var strategy = new ScriptedStrategy(
            outcomes: [CharacterAccumulationTurnOutcome.AwaitNextTurn()],
            captures: [CharacterAccumulationTurnCapture.Silence, CharacterAccumulationTurnCapture.Silence],
            hasContent: _ => true)
        {
            FinalizedValue = "markus"
        };

        var result = await new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance)
            .RunAsync(strategy, CreateContext(), "letzten Block löschen", CancellationToken.None);

        Assert.True(result.HasCapturedTranscript);
        Assert.Equal("markus", result.CapturedTranscript);
        Assert.Equal(1, strategy.RePromptCount);
        Assert.Equal(2, strategy.CaptureCount);
    }

    [Fact]
    public async Task MidAccumulationSilence_RePromptsAndKeepsListening_DoesNotFinalizeOnFirstPause()
    {
        // Ein weiterer Text nach der Nachfrage setzt die Folge ohne Sprache zurück.
        // Spätere Meldungen ohne Sprache lösen deshalb erneut eine Nachfrage aus.
        var strategy = new ScriptedStrategy(
            outcomes:
            [
                CharacterAccumulationTurnOutcome.AwaitNextTurn(),
                CharacterAccumulationTurnOutcome.AwaitNextTurn()
            ],
            captures: [CharacterAccumulationTurnCapture.Silence, CharacterAccumulationTurnCapture.Transcribed("second"), CharacterAccumulationTurnCapture.Silence, CharacterAccumulationTurnCapture.Silence],
            hasContent: _ => true)
        {
            FinalizedValue = "ab"
        };

        var result = await new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance)
            .RunAsync(strategy, CreateContext(), "first", CancellationToken.None);

        Assert.True(result.HasCapturedTranscript);
        // Nach der ersten Meldung ohne Sprache wird der zweite Text noch verarbeitet.
        Assert.Equal(["first", "second"], strategy.ProcessedTranscripts);
        Assert.Equal(2, strategy.RePromptCount);
        Assert.Equal(4, strategy.CaptureCount);
    }

    [Fact]
    public async Task RePromptedSilence_ThenExplicitDoneCommand_Finalizes()
    {
        // Die vorgegebene Strategie deutet „fertig“ als Abschluss.
        // Nach der Nachfrage ist deshalb keine zweite Meldung ohne Sprache nötig.
        var strategy = new ScriptedStrategy(
            outcomes:
            [
                CharacterAccumulationTurnOutcome.AwaitNextTurn(),
                CharacterAccumulationTurnOutcome.Finalize()
            ],
            captures: [CharacterAccumulationTurnCapture.Silence, CharacterAccumulationTurnCapture.Transcribed("fertig")],
            hasContent: _ => true)
        {
            FinalizedValue = "ab"
        };

        var result = await new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance)
            .RunAsync(strategy, CreateContext(), "first", CancellationToken.None);

        Assert.True(result.HasCapturedTranscript);
        Assert.Equal("ab", result.CapturedTranscript);
        Assert.Equal(["first", "fertig"], strategy.ProcessedTranscripts);
        Assert.Equal(1, strategy.RePromptCount);
        Assert.Equal(2, strategy.CaptureCount);
    }

    [Fact]
    public async Task ExplicitCompletion_FinalizesWithoutCapturingAnotherTurn()
    {
        var strategy = new ScriptedStrategy(
            outcomes: [CharacterAccumulationTurnOutcome.Finalize()],
            captures: [],
            hasContent: _ => true)
        {
            FinalizedValue = "abc"
        };

        var result = await new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance)
            .RunAsync(strategy, CreateContext(), "fertig", CancellationToken.None);

        Assert.True(result.HasCapturedTranscript);
        Assert.Equal("abc", result.CapturedTranscript);
        Assert.Equal(0, strategy.CaptureCount);
    }

    [Fact]
    public async Task SingleFirstCapture_NeverSelfCommits_DriverAlwaysAwaitsACaptureTurn()
    {
        // Nach AwaitNextTurn muss erneut erfasst werden. Erst nach zwei aufeinanderfolgenden
        // Meldungen ohne Sprache bei vorhandenem Inhalt darf die Eingabe enden.
        var strategy = new ScriptedStrategy(
            outcomes:
            [
                CharacterAccumulationTurnOutcome.AwaitNextTurn(),
                CharacterAccumulationTurnOutcome.AwaitNextTurn()
            ],
            captures: [CharacterAccumulationTurnCapture.Transcribed("second"), CharacterAccumulationTurnCapture.Silence, CharacterAccumulationTurnCapture.Silence],
            hasContent: captureCount => captureCount >= 2)
        {
            FinalizedValue = "ab"
        };

        var result = await new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance)
            .RunAsync(strategy, CreateContext(), "first", CancellationToken.None);

        Assert.True(result.HasCapturedTranscript);
        Assert.Equal(3, strategy.CaptureCount);
        Assert.Equal(1, strategy.RePromptCount);
        Assert.Equal(["first", "second"], strategy.ProcessedTranscripts);
    }

    [Fact]
    public async Task EmptyBufferSilence_NeverAborts_ReCapturesInField()
    {
        // Bei leerem Puffer führen Meldungen ohne Sprache zu weiteren Erfassungen.
        // Erst bei vorhandenem Inhalt greifen Nachfrage und anschließender Abschluss.
        var strategy = new ScriptedStrategy(
            outcomes:
            [
                CharacterAccumulationTurnOutcome.AwaitNextTurn(),
                CharacterAccumulationTurnOutcome.AwaitNextTurn()
            ],
            captures: [CharacterAccumulationTurnCapture.Silence, CharacterAccumulationTurnCapture.Silence, CharacterAccumulationTurnCapture.Transcribed("go"), CharacterAccumulationTurnCapture.Silence, CharacterAccumulationTurnCapture.Silence],
            hasContent: captureCount => captureCount >= 4)
        {
            FinalizedValue = "x"
        };

        var result = await new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance)
            .RunAsync(strategy, CreateContext(), "start", CancellationToken.None);

        Assert.True(result.HasCapturedTranscript);
        Assert.Equal(5, strategy.CaptureCount);
        // Nur die Meldung ohne Sprache bei vorhandenem Inhalt löst hier eine Nachfrage aus.
        Assert.Equal(1, strategy.RePromptCount);
        Assert.Equal(["start", "go"], strategy.ProcessedTranscripts);
    }

    [Fact]
    public async Task AggregatedConfidence_FlowsFromStrategyToResult()
    {
        var strategy = new ScriptedStrategy(
            outcomes: [CharacterAccumulationTurnOutcome.Finalize()],
            captures: [],
            hasContent: _ => true)
        {
            FinalizedValue = "v",
            AggregatedConfidencePercent = 42
        };

        var result = await new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance)
            .RunAsync(strategy, CreateContext(), "fertig", CancellationToken.None);

        Assert.Equal(42, result.AggregatedConfidencePercent);
    }

    [Fact]
    public async Task RecognizerUnavailableMidAccumulation_NeverFinalizes_ParticipantDictatesTheRestOfTheValue()
    {
        // Ein Erkennerfehler sagt nichts darüber aus, ob gesprochen wurde. Zwei solche Fehler
        // dürfen deshalb nicht wie zwei Meldungen ohne Sprache einen Teilwert abschließen.
        // Die vorgegebene Strategie liefert nach den Wiederholungen den vollständigen Testwert.
        var strategy = new ScriptedStrategy(
            outcomes:
            [
                CharacterAccumulationTurnOutcome.AwaitNextTurn(),
                CharacterAccumulationTurnOutcome.Finalize()
            ],
            captures:
            [
                CharacterAccumulationTurnCapture.RecognizerUnavailable,
                CharacterAccumulationTurnCapture.RecognizerUnavailable,
                CharacterAccumulationTurnCapture.Transcribed("3704")
            ],
            hasContent: _ => true)
        {
            FinalizedValue = "de893704"
        };

        var result = await new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance)
            .RunAsync(strategy, CreateContext(), "de89", CancellationToken.None);

        Assert.True(result.HasCapturedTranscript);
        Assert.Equal("de893704", result.CapturedTranscript);
        Assert.Equal(3, strategy.CaptureCount);
        Assert.Equal(["de89", "3704"], strategy.ProcessedTranscripts);
        // Innerhalb der Wiederholungsgrenze wird noch kein zusätzlicher Fehlerhinweis ausgegeben.
        Assert.Equal(0, strategy.RecognizerUnavailableAnnouncementCount);
        // Ein Fehler verbraucht keine Nachfrage für eine Meldung ohne Sprache.
        Assert.Equal(0, strategy.RePromptCount);
    }

    [Fact]
    public async Task RecognizerUnavailable_DoesNotConsumeTheRePromptBudget_ALaterRealSilenceStillGetsIt()
    {
        // Der simulierte Erkennerfehler darf nicht als erste Meldung ohne Sprache zählen.
        // Bleibt die Spracheingabe danach aus, muss weiterhin nachgefragt werden.
        var strategy = new ScriptedStrategy(
            outcomes: [CharacterAccumulationTurnOutcome.AwaitNextTurn()],
            captures:
            [
                CharacterAccumulationTurnCapture.RecognizerUnavailable,
                CharacterAccumulationTurnCapture.Silence,
                CharacterAccumulationTurnCapture.Silence
            ],
            hasContent: _ => true)
        {
            FinalizedValue = "de89"
        };

        var result = await new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance)
            .RunAsync(strategy, CreateContext(), "de89", CancellationToken.None);

        Assert.True(result.HasCapturedTranscript);
        Assert.Equal(3, strategy.CaptureCount);
        Assert.Equal(1, strategy.RePromptCount);
    }

    [Fact]
    public async Task SilenceThenFaultThenSilence_DoesNotFinalize_TheFaultBreaksTheConsecutiveSilenceChain()
    {
        // Ein Erkennerfehler zwischen zwei Meldungen ohne Sprache unterbricht deren Folge.
        // Die nächste Meldung ohne Sprache beginnt erneut mit einer Nachfrage,
        // statt einen möglicherweise unvollständigen Wert abzuschließen.
        var strategy = new ScriptedStrategy(
            outcomes: [CharacterAccumulationTurnOutcome.AwaitNextTurn()],
            captures:
            [
                CharacterAccumulationTurnCapture.Silence,
                CharacterAccumulationTurnCapture.RecognizerUnavailable,
                CharacterAccumulationTurnCapture.Silence,
                CharacterAccumulationTurnCapture.Silence
            ],
            hasContent: _ => true)
        {
            FinalizedValue = "de89"
        };

        var result = await new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance)
            .RunAsync(strategy, CreateContext(), "de89", CancellationToken.None);

        Assert.True(result.HasCapturedTranscript);
        Assert.Equal(4, strategy.CaptureCount);
        // Je eine Nachfrage vor und nach der Unterbrechung durch den Erkennerfehler.
        Assert.Equal(2, strategy.RePromptCount);
    }

    [Fact]
    public async Task RecognizerNeverRecovers_TerminatesAndHandsTheFieldBackUncaptured_NeverCommitsThePartialValue()
    {
        // Bei dauerhaftem Erkennerfehler muss der Ablauf enden, ohne einen Teilwert zu übernehmen.
        // Sprachbefehle können den Fehler nicht zuverlässig beenden, daher ist die Wiederholung begrenzt.
        var strategy = new ScriptedStrategy(
            outcomes: [CharacterAccumulationTurnOutcome.AwaitNextTurn()],
            captures: Enumerable.Repeat(CharacterAccumulationTurnCapture.RecognizerUnavailable, 50),
            hasContent: _ => true)
        {
            FinalizedValue = "de89"
        };

        var result = await new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance)
            .RunAsync(strategy, CreateContext(), "de89", CancellationToken.None);

        Assert.False(result.HasCapturedTranscript);
        Assert.Equal(string.Empty, result.CapturedTranscript);
        // Die Voreinstellung erlaubt drei Wiederholungen; der vierte Fehler beendet den Ablauf.
        Assert.Equal(4, strategy.CaptureCount);
        // Ein Erkennerfehler soll keinen Hinweis zur Art der Benutzereingabe auslösen.
        Assert.Equal(0, strategy.RePromptCount);
        // Ein einzelner Fehlerhinweis erklärt, warum die bisherige Eingabe nicht übernommen wurde.
        Assert.Equal(1, strategy.RecognizerUnavailableAnnouncementCount);
    }

    [Fact]
    public async Task RecognizerNeverRecovers_AndSayingSoAlsoFails_StillHandsTheFieldBackInsteadOfAbandoningTheForm()
    {
        // Auch die Ausgabe des Fehlerhinweises kann scheitern. Dieser zweite Fehler darf
        // nicht aus dem Ablauf entweichen: Er muss weiterhin ohne erfassten Wert zurückkehren.
        // Der Test simuliert den Ausgabefehler; er prüft nicht den gesamten Formularzustand.
        var strategy = new ScriptedStrategy(
            outcomes: [CharacterAccumulationTurnOutcome.AwaitNextTurn()],
            captures: Enumerable.Repeat(CharacterAccumulationTurnCapture.RecognizerUnavailable, 50),
            hasContent: _ => true)
        {
            FinalizedValue = "de89",
            ThrowOnRecognizerUnavailableAnnouncement = true
        };

        var result = await new CharacterAccumulationWorkflowDriver(
                NullLogger<CharacterAccumulationWorkflowDriver>.Instance)
            .RunAsync(strategy, CreateContext(), "de89", CancellationToken.None);

        Assert.False(result.HasCapturedTranscript);
        Assert.Equal(1, strategy.RecognizerUnavailableAnnouncementCount);
        Assert.Equal(4, strategy.CaptureCount);
    }

    private static CharacterAccumulationContext CreateContext()
    {
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose).Object;
        return new CharacterAccumulationContext(
            SpellingWorkflowTestFactory.CreateContext("de"),
            new Mock<IFormDialogRuntime>(MockBehavior.Loose).Object,
            SpellingWorkflowTestFactory.CreateField(),
            session,
            new FormVoiceCommandStateTracker(telemetry, session),
            new FormVoiceCommandAutoHelpTracker(
                new VoiceDialogSettings(),
                new Mock<IFormVoiceCommandPromptService>(MockBehavior.Loose).Object,
                new Mock<IFormVoiceCommandDialogStateTracker>(MockBehavior.Loose).Object,
                new Mock<IFormDialogRuntime>(MockBehavior.Loose).Object,
                "de"),
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            AnchoredCommandFlowTestFactory.Create());
    }
}
