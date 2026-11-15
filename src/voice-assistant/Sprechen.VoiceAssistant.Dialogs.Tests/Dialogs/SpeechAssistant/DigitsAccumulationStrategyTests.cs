using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.TestDoubles;
using Xunit.Abstractions;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft zusammengesetzte Zifferneingaben, blockweise Korrekturen und deren Konfidenz.
/// Das Aggregat ist das Minimum der gemeldeten Konfidenzen erhaltener Blöcke.
/// Abgelehnte Eingaben, Befehle und vollständig gelöschte oder ersetzte Blöcke tragen nicht bei.
/// Ohne erhaltene gemeldete Konfidenz bleibt das Aggregat unbekannt.
/// Der Parser-Mock übersetzt eine feste Wortliste in Ziffern; echte Spracherkennung wird nicht geprüft.
/// </summary>
public sealed class DigitsAccumulationStrategyTests
{
    private readonly ITestOutputHelper _output;

    public DigitsAccumulationStrategyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static readonly IReadOnlyDictionary<string, string> DigitMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["eins"] = "1",
        ["zwei"] = "2",
        ["drei"] = "3",
        ["vier"] = "4",
        ["fünf"] = "5",
        ["sechs"] = "6",
        ["eins zwei"] = "12",
        ["eins zwei drei"] = "123"
    };

    // Der bekannte Sollwert entsteht nur aus den im endgültigen Wert erhaltenen Blöcken.
    [Fact]
    public async Task WholeSequenceConfidence_UsesOnlySurvivingValueContributions()
    {
        var captures = Captures(
            ("zwei", 90),                  // Bleibt als Ziffer 2 erhalten.
            ("drei", 40),                  // Wird im nächsten Schritt gelöscht; Konfidenz 40 entfällt.
            ("letzten block löschen", 10), // Löscht die Ziffer 3; die Befehls-Konfidenz trägt nicht bei.
            ("vier", 95),                  // Bleibt als Ziffer 4 erhalten.
            ("Quatsch", 5),                // Abgelehnte Eingabe ohne Beitrag zu Wert oder Konfidenz.
            ("fünf", null),                // Bleibt als Ziffer 5 ohne gemeldete Konfidenz erhalten.
            ("sechs", 85),                 // Bleibt als Ziffer 6 erhalten.
            Silence,
            Silence);

        var result = await RunAsync(captures, initialTranscript: "eins", initialConfidence: 90);

        Assert.Equal("12456", result.CapturedTranscript);
        // Erhaltene Konfidenzen: 1=90, 2=90, 4=95, 6=85; das Minimum ist 85.
        // Würden abgelehnte Eingaben oder Befehle mitzählen, sänke das Ergebnis fälschlich auf 5.
        Assert.Equal(85, result.AggregatedConfidencePercent);

        // Die folgende Ausgabe leitet nur eine Konfidenzstufe für diesen Testfall ab.
        // Trotz der Ausgabebezeichnungen confirmationRate und confirmationTurns zählt sie keine
        // tatsächlichen Bestätigungen und liefert keinen Sollwert für die Berechnung der Gesamtkonfidenz.
        var level = SttConfidenceGate.Evaluate(
            SpellingWorkflowTestFactory.CreateContext("de"),
            result.AggregatedConfidencePercent);
        var confirmationTurns = level == SttConfidenceLevel.Sufficient ? 0 : 1;
        _output.WriteLine(
            $"confirmationRate corpus=1 confirmationTurns={confirmationTurns} completedDigitsValues=1 aggregate={result.AggregatedConfidencePercent} note=band-pressure-not-skipped-confirmation");
    }

    [Fact]
    public async Task RejectedInitialTurn_DoesNotContributeItsConfidence()
    {
        var result = await RunAsync(
            Captures(("eins", 90), Silence, Silence),
            initialTranscript: "Quatsch",
            initialConfidence: 30);

        Assert.Equal("1", result.CapturedTranscript);
        Assert.Equal(90, result.AggregatedConfidencePercent);
    }

    // Der Telemetrie-Mock unterscheidet einen Löschbefehl von einer abgelehnten Eingabe.
    // Nur so ist belegt, dass die Befehls-Konfidenz und nicht bloß eine ungültige Eingabe ausgeschlossen wird.
    [Fact]
    public async Task CommandOnlyInitialTurn_DoesNotContributeItsConfidence()
    {
        var run = await RunAsync(
            Captures(("eins", 90), Silence, Silence),
            initialTranscript: "letzten block löschen",
            initialConfidence: 30,
            trackRepairs: true);

        Assert.Equal("1", run.Result.CapturedTranscript);
        Assert.Equal(90, run.Result.AggregatedConfidencePercent);
        // Das Löschen im leeren Puffer meldet NoValue, nicht UnrecognizedBlock.
        run.FormTelemetry.Verify(
            t => t.TrackRepairAttempt(
                It.IsAny<EvaluationSession>(),
                It.IsAny<string>(),
                DigitsWorkflowRepairEventCodes.NoValue),
            Times.Once);
        run.FormTelemetry.Verify(
            t => t.TrackRepairAttempt(
                It.IsAny<EvaluationSession>(),
                It.IsAny<string>(),
                DigitsWorkflowRepairEventCodes.UnrecognizedBlock),
            Times.Never);
    }

    // Auch die erste Eingabe trägt ihre Konfidenz bei, wenn ihr Block erhalten bleibt.
    [Fact]
    public async Task ContributingInitialTurn_FoldsItsConfidenceLikeAnyContributingTurn()
    {
        var result = await RunAsync(
            Captures(("zwei", 88), Silence, Silence),
            initialTranscript: "eins",
            initialConfidence: 60);

        Assert.Equal("12", result.CapturedTranscript);
        Assert.Equal(60, result.AggregatedConfidencePercent);
    }

    [Fact]
    public async Task RejectedTurn_DoesNotLowerTheAggregate()
    {
        var result = await RunAsync(
            Captures(("Quatsch", 5), ("zwei", 85), Silence, Silence),
            initialTranscript: "eins",
            initialConfidence: 90);

        Assert.Equal("12", result.CapturedTranscript);
        Assert.Equal(85, result.AggregatedConfidencePercent);
    }

    [Fact]
    public async Task CommandTurn_DoesNotLowerTheAggregate()
    {
        var result = await RunAsync(
            Captures(("letzten block löschen", 10), ("zwei", 85), Silence, Silence),
            initialTranscript: "eins",
            initialConfidence: 90);

        Assert.Equal("2", result.CapturedTranscript);
        Assert.Equal(85, result.AggregatedConfidencePercent);
    }

    // Ein verkürzter Block stammt weiterhin aus derselben Erkennung und behält deren Konfidenz.
    // Eine eigene Konfidenz pro Ziffer wurde nicht gemeldet und darf nicht erfunden werden.
    [Fact]
    public async Task RemoveLastDigit_ShortenedBlockKeepsItsUtteranceConfidence()
    {
        var result = await RunAsync(
            Captures(("ziffer zurück", 95), Silence, Silence),
            initialTranscript: "eins zwei",
            initialConfidence: 90);

        Assert.Equal("1", result.CapturedTranscript);
        Assert.Equal(90, result.AggregatedConfidencePercent);
    }

    [Fact]
    public async Task RemoveLastDigit_EmptiedBlockDropsItsConfidence()
    {
        var result = await RunAsync(
            Captures(("zwei", 40), ("ziffer zurück", 95), Silence, Silence),
            initialTranscript: "eins",
            initialConfidence: 90);

        Assert.Equal("1", result.CapturedTranscript);
        Assert.Equal(90, result.AggregatedConfidencePercent);
    }

    // Erneutes Diktieren ersetzt den alten Block samt Konfidenz 40 durch den neuen mit Konfidenz 85.
    [Fact]
    public async Task RedictateLastBlock_ReplacesTheBlocksConfidence()
    {
        var result = await RunAsync(
            Captures(("zwei", 40), ("block neu diktieren", 95), ("drei", 85), Silence, Silence),
            initialTranscript: "eins",
            initialConfidence: 90);

        Assert.Equal("13", result.CapturedTranscript);
        Assert.Equal(85, result.AggregatedConfidencePercent);
    }

    // Eine gemeldete Konfidenz von null Prozent ist ein Wert und darf nicht wie eine fehlende
    // Konfidenz behandelt werden. Die Schwellenprüfung ordnet sie als Insufficient ein.
    [Fact]
    public async Task ContributingTurnAtZeroConfidence_YieldsZero_AndTheGateRejectsByDesign()
    {
        var result = await RunAsync(
            Captures(("zwei", 90), Silence, Silence),
            initialTranscript: "eins",
            initialConfidence: 0);

        Assert.Equal("12", result.CapturedTranscript);
        Assert.Equal(0, result.AggregatedConfidencePercent);
        Assert.Equal(
            SttConfidenceLevel.Insufficient,
            SttConfidenceGate.Evaluate(
                SpellingWorkflowTestFactory.CreateContext("de"),
                result.AggregatedConfidencePercent));
    }

    // Auch bei einer Konfidenz von 100 Prozent bleibt das Minimum der erhaltenen Blöcke maßgeblich.
    [Fact]
    public async Task ContributingTurnAtFullConfidence_FoldsNormally()
    {
        var result = await RunAsync(
            Captures(("zwei", 95), Silence, Silence),
            initialTranscript: "eins",
            initialConfidence: 100);

        Assert.Equal("12", result.CapturedTranscript);
        Assert.Equal(95, result.AggregatedConfidencePercent);
    }

    // Der Wiederholungsbefehl fügt keinen Block hinzu. Seine Konfidenz 99 trägt deshalb nicht bei.
    [Fact]
    public async Task ReadbackCommandTurn_DoesNotContributeItsConfidence()
    {
        var run = await RunAsync(
            Captures(("block wiederholen", 99), ("zwei", 85), Silence, Silence),
            initialTranscript: "eins",
            initialConfidence: 90,
            trackRepairs: true);

        Assert.Equal("12", run.Result.CapturedTranscript);
        Assert.Equal(85, run.Result.AggregatedConfidencePercent);
        run.FormTelemetry.Verify(
            t => t.TrackRepairAttempt(
                It.IsAny<EvaluationSession>(),
                It.IsAny<string>(),
                DigitsWorkflowRepairEventCodes.ReadbackDigits),
            Times.Once);
    }

    // Ohne gemeldete Konfidenz eines erhaltenen Blocks bleibt das Aggregat unbekannt.
    // Eine nachgelagerte Bestätigung wird hier nicht geprüft.
    [Fact]
    public async Task OnlyNullConfidences_YieldUnknownAggregateConfidence()
    {
        var result = await RunAsync(
            Captures(("zwei", null), Silence, Silence),
            initialTranscript: "eins",
            initialConfidence: null);

        Assert.Equal("12", result.CapturedTranscript);
        Assert.Null(result.AggregatedConfidencePercent);
    }

    private static readonly (string? Transcript, int? Confidence) Silence = (null, null);

    private static Queue<(string? Transcript, int? Confidence)> Captures(
        params (string? Transcript, int? Confidence)[] steps)
        => new(steps);

    private static async Task<CharacterAccumulationResult> RunAsync(
        Queue<(string? Transcript, int? Confidence)> captures,
        string initialTranscript,
        int? initialConfidence)
    {
        var (result, _) = await RunAsync(captures, initialTranscript, initialConfidence, trackRepairs: false);
        return result;
    }

    private static async Task<(
        CharacterAccumulationResult Result,
        Mock<IEvaluationFormTelemetry> FormTelemetry)> RunAsync(
        Queue<(string? Transcript, int? Confidence)> captures,
        string initialTranscript,
        int? initialConfidence,
        bool trackRepairs)
    {
        var formTelemetry = new Mock<IEvaluationFormTelemetry>(MockBehavior.Loose);

        var parser = new Mock<IFormTranscriptParser>(MockBehavior.Loose);
        parser
            .Setup(p => p.ParseDigits(It.IsAny<string?>(), It.IsAny<string>()))
            .Returns((string? _, string transcript) =>
                DigitMap.TryGetValue(transcript.Trim(), out var mapped)
                    ? FormTranscriptParseOutcome.Succeeded(mapped)
                    : FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized));

        var speech = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
        speech
            .Setup(s => s.SpeakSafeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        speech
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>(
                (_, _, _, onStartListening, _, _, _, _) =>
                {
                    onStartListening();
                    var (transcript, confidence) = captures.Dequeue();
                    return transcript is null
                        ? Task.FromException<SpeechCaptureResult>(new NoSpeechDetectedException())
                        : Task.FromResult(new SpeechCaptureResult(transcript, confidence));
                });
        speech.ForwardSingleAttemptCaptureToValueCapture();

        var service = new FormVoiceCommandDigitsWorkflowService(
            new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
            AnchoredCommandFlowTestFactory.Create(),
            parser.Object,
            new DigitsWorkflowCommandResolver(
                new DigitsWorkflowLocalCommandMapper(),
                new DigitsWorkflowGlobalCommandMapper()),
            new DigitsWorkflowCommandActionFlow(
                formTelemetry.Object,
                speech.Object,
                new DigitsWorkflowNoValuePrompt(speech.Object)),
            new DigitsWorkflowInputRejectionFlow(
                new WorkflowUnrecognizedInputAnnouncer(formTelemetry.Object, speech.Object)),
            new DigitsWorkflowTranscriptCapture(speech.Object));

        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose).Object;
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            new VoiceDialogSettings(),
            new Mock<IFormVoiceCommandPromptService>(MockBehavior.Loose).Object,
            new Mock<IFormVoiceCommandDialogStateTracker>(MockBehavior.Loose).Object,
            new Mock<IFormDialogRuntime>(MockBehavior.Loose).Object,
            "de");

        var result = await service.CaptureDigitsTranscriptAsync(
            SpellingWorkflowTestFactory.CreateContext("de"),
            new Mock<IFormDialogRuntime>(MockBehavior.Loose).Object,
            SpellingWorkflowTestFactory.CreateField(),
            session,
            ArmFieldLoop(new FormVoiceCommandStateTracker(telemetry, session)),
            autoHelp,
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            initialTranscript,
            initialConfidence,
            CancellationToken.None);

        return (result, formTelemetry);
    }

    private static FormVoiceCommandStateTracker ArmFieldLoop(FormVoiceCommandStateTracker tracker)
    {
        tracker.BeginFieldPrompt("main");
        tracker.BeginListening();
        return tracker;
    }

}
