using Sprechen.Speech.ModelRuntime.PromptTemplates;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.TestDoubles;
using Xunit.Abstractions;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft das Sammeln und Korrigieren buchstabierter Blöcke sowie deren Konfidenz.
/// Aufnahme, Ausgabe und Datenschutzfreigabe sind simuliert; zusammengesetzte
/// Transkripte werden in ausgewählten Fällen mit dem echten Parser ausgewertet.
/// </summary>
public sealed class SpellingAccumulationStrategyTests
{
    private readonly ITestOutputHelper _output;

    public SpellingAccumulationStrategyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static readonly IReadOnlyDictionary<string, string> LetterMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Martha"] = "m",
        ["Anton"] = "a",
        ["Richard"] = "r",
        ["Kaufmann"] = "k",
        ["Ulrich"] = "u",
        ["Samuel"] = "s",
        ["Berta"] = "b",
        ["AntonBerta"] = "ab",
        ["Cesar"] = "c",
        ["Dora"] = "d"
    };

    [Fact]
    public async Task Accumulate_AcrossTurns_FinalizesOnSecondConsecutiveSilence_AndAggregatesLowestConfidence()
    {
        var spoken = new List<string>();
        var captures = Captures(
            ("Anton", 90),
            ("Richard", 70),
            ("Kaufmann", 95),
            ("Ulrich", 60),
            ("Samuel", 85),
            Silence,
            Silence);

        var result = await RunAsync(spoken, captures, spellOutAllowed: true, initialTranscript: "Martha", initialConfidence: 80);

        Assert.True(result.HasCapturedTranscript);
        Assert.Equal("markus", result.CapturedTranscript);
        Assert.Equal(60, result.AggregatedConfidencePercent);
    }

    [Fact]
    public async Task Accumulate_PassesTheContextIsoCodeToTheTranscriptParser()
    {
        // Der Parser benötigt die Sitzungssprache zur Auswahl seines Buchstabierwortschatzes.
        var spoken = new List<string>();
        var captures = Captures(("Anton", 90), Silence, Silence);
        var parser = new Mock<IFormTranscriptParser>(MockBehavior.Loose);

        await RunAsync(spoken, captures, spellOutAllowed: true, initialTranscript: "Martha", initialConfidence: 80, parser: parser, isoCode: "fr");

        parser.Verify(
            p => p.ParseSpelling("fr", It.IsAny<string>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task RemoveLastCharacter_Command_DropsTheLastBlock()
    {
        var spoken = new List<string>();
        var captures = Captures(("Berta", 90), ("zurück", 90), Silence, Silence);

        var result = await RunAsync(spoken, captures, spellOutAllowed: true, initialTranscript: "Anton", initialConfidence: 90);

        Assert.Equal("a", result.CapturedTranscript);
    }

    [Fact]
    public async Task RemoveLastBlock_Command_DropsTheLastCapturedBlock()
    {
        var spoken = new List<string>();
        var captures = Captures(("Cesar", 90), ("letzten Block löschen", 90), Silence, Silence);

        var result = await RunAsync(spoken, captures, spellOutAllowed: true, initialTranscript: "AntonBerta", initialConfidence: 90);

        Assert.Equal("ab", result.CapturedTranscript);
    }

    [Fact]
    public async Task ClearBuffer_Command_EmptiesEverything()
    {
        var spoken = new List<string>();
        var captures = Captures(("alles löschen", 90), ("Dora", 90), Silence, Silence);

        var result = await RunAsync(spoken, captures, spellOutAllowed: true, initialTranscript: "Anton", initialConfidence: 90);

        Assert.Equal("d", result.CapturedTranscript);
    }

    [Fact]
    public async Task AfterEachBlock_WhenSpellOutAllowed_ReadsBackPhonetically()
    {
        var spoken = new List<string>();
        var captures = Captures(Silence, Silence);

        await RunAsync(spoken, captures, spellOutAllowed: true, initialTranscript: "Anton", initialConfidence: 90);

        Assert.Contains(spoken, line => line.Contains("PHON[a]", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AfterEachBlock_WhenSpellOutForbidden_SuppressesCharactersWithoutSpeaking()
    {
        var spoken = new List<string>();
        var captures = Captures(Silence, Silence);
        await RunAsync(spoken, captures, spellOutAllowed: false, initialTranscript: "Anton", initialConfidence: 90);

        Assert.DoesNotContain(spoken, line => line.Contains("PHON[", StringComparison.Ordinal));
    }


    [Fact]
    public async Task RecognizerNeverRecovers_TheRealSpellingStrategySpeaksTheTruthfulSentence()
    {
        // Der echte Buchstabierablauf muss bei wiederholtem simuliertem Erkennungsausfall
        // den Fehlertext für die Sitzungssprache anfordern.
        var spoken = new List<string>();
        var captures = Captures(
            ("Anton", 90),
            RecognizerFault,
            RecognizerFault,
            RecognizerFault,
            RecognizerFault,
            RecognizerFault);

        var result = await RunAsync(spoken, captures, spellOutAllowed: true, "Martha", 90);

        // Der unvollständige Wert wird nicht als erfasstes Ergebnis zurückgegeben.
        Assert.False(result.HasCapturedTranscript);
        Assert.Contains(
            RecoverableCapturePromptTextResolver.GetRecognizerUnavailablePrompt("de", PolitenessLevel.Formal),
            spoken);
    }

    // Die minimale Konfidenz berücksichtigt nur erhaltene Zeichenblöcke mit gemeldeter Konfidenz.
    // Zurückgewiesene Eingaben, Befehle und gelöschte Blöcke tragen nicht dazu bei.
    [Fact]
    public async Task WholeSequenceConfidence_UsesOnlySurvivingValueContributions()
    {
        var spoken = new List<string>();
        var captures = Captures(
            ("Anton", 90),                   // bleibt als a erhalten
            ("Richard", 40),                 // wird im folgenden Schritt gelöscht
            ("letzten Block löschen", 10),   // löscht r und trägt keine Wertkonfidenz bei
            ("Kaufmann", 95),                // bleibt als k erhalten
            ("Quatsch", 5),                  // liefert keinen erkannten Zeichenblock
            ("Ulrich", null),                // bleibt als u ohne gemeldete Konfidenz erhalten
            ("Samuel", 85),                  // bleibt als s erhalten
            Silence,
            Silence);

        var result = await RunAsync(spoken, captures, spellOutAllowed: true, initialTranscript: "Martha", initialConfidence: 90);

        Assert.Equal("makus", result.CapturedTranscript);
        // Erhaltene Konfidenzen: m=90, a=90, k=95 und s=85; ihr Minimum ist 85.
        Assert.Equal(85, result.AggregatedConfidencePercent);

        // Zusätzliche Diagnose: Die Konfidenz wird anhand der modellierten Schwellen eingeordnet.
        // Der ausgegebene Zähler leitet sich daraus ab; ein tatsächlicher Bestätigungsschritt wird nicht ausgeführt.
        var level = SttConfidenceGate.Evaluate(
            SpellingWorkflowTestFactory.CreateContext("de"),
            result.AggregatedConfidencePercent);
        var confirmationTurns = level == SttConfidenceLevel.Sufficient ? 0 : 1;
        _output.WriteLine(
            $"confirmationRate corpus=1 confirmationTurns={confirmationTurns} completedSpellingValues=1 aggregate={result.AggregatedConfidencePercent}");
    }

    // Mit dem gelöschten Block entfällt auch dessen niedrige Konfidenz.
    [Fact]
    public async Task RemoveLastCharacter_DropsTheDeletedSegmentConfidence()
    {
        var spoken = new List<string>();
        var captures = Captures(("Berta", 40), ("zurück", 90), Silence, Silence);

        var result = await RunAsync(spoken, captures, spellOutAllowed: true, initialTranscript: "Anton", initialConfidence: 90);

        Assert.Equal("a", result.CapturedTranscript);
        Assert.Equal(90, result.AggregatedConfidencePercent);
    }

    // Eine nicht auswertbare Eingabe ergänzt keinen Block und senkt die Wertkonfidenz nicht.
    [Fact]
    public async Task RejectedTurn_DoesNotLowerTheAggregate()
    {
        var spoken = new List<string>();
        var captures = Captures(("Quatsch", 5), ("Berta", 85), Silence, Silence);

        var result = await RunAsync(spoken, captures, spellOutAllowed: true, initialTranscript: "Anton", initialConfidence: 90);

        Assert.Equal("ab", result.CapturedTranscript);
        Assert.Equal(85, result.AggregatedConfidencePercent);
    }

    // Die Konfidenz eines Löschbefehls beschreibt keinen Teil des verbleibenden Werts.
    [Fact]
    public async Task CommandTurn_DoesNotLowerTheAggregate()
    {
        var spoken = new List<string>();
        var captures = Captures(("letzten Block löschen", 10), ("Berta", 85), Silence, Silence);

        var result = await RunAsync(spoken, captures, spellOutAllowed: true, initialTranscript: "Anton", initialConfidence: 90);

        Assert.Equal("b", result.CapturedTranscript);
        Assert.Equal(85, result.AggregatedConfidencePercent);
    }

    // Ohne gemeldete Konfidenz eines erhaltenen Blocks bleibt auch das Gesamtergebnis unbekannt.
    [Fact]
    public async Task OnlyNullConfidences_YieldUnknownAggregateConfidence()
    {
        var spoken = new List<string>();
        var captures = Captures(("Anton", null), Silence, Silence);

        var result = await RunAsync(spoken, captures, spellOutAllowed: true, initialTranscript: "Martha", initialConfidence: null);

        Assert.Equal("ma", result.CapturedTranscript);
        Assert.Null(result.AggregatedConfidencePercent);
    }

    private static readonly (string? Transcript, int? Confidence) Silence = (null, null);

    /// <summary>
    /// Kennzeichnet einen simulierten Erkennungsausfall, der anders als die
    /// durch <see cref="Silence"/> dargestellte stille Aufnahme behandelt wird.
    /// </summary>
    private static readonly (string? Transcript, int? Confidence) RecognizerFault = ("__fault__", null);

    private static Queue<(string? Transcript, int? Confidence)> Captures(
        params (string? Transcript, int? Confidence)[] steps)
        => new(steps);

    [Theory]
    [InlineData("Anton Berta fertig")]
    [InlineData("Anton Berta übernehmen")]
    [InlineData("Anton Berta spracheingabe")]
    public async Task LettersAndTerminatorInOneTurn_CommitTheLetters_WithRealParser(string spokenInOneBreath)
    {
        // Bei Buchstaben und Abschlusswort im selben Transkript müssen die Buchstaben
        // vor dem Abschluss ausgewertet werden. Dieser Fall verwendet den echten Parser.
        var spoken = new List<string>();

        var result = await RunAsync(
            spoken,
            Captures(),
            spellOutAllowed: true,
            initialTranscript: spokenInOneBreath,
            initialConfidence: 90,
            useRealParser: true);

        Assert.True(result.HasCapturedTranscript, "the terminator ended the turn without committing the letters spoken with it");
        Assert.Equal("ab", result.CapturedTranscript);
    }

    [Fact]
    public async Task BareTerminator_StillFinalizesExactlyAsBefore_WithRealParser()
    {
        // Ein alleinstehendes Abschlusswort darf nicht als Buchstabenfolge zum Wert beitragen.
        var spoken = new List<string>();

        var result = await RunAsync(
            spoken,
            Captures(("fertig", 90)),
            spellOutAllowed: true,
            initialTranscript: "Anton",
            initialConfidence: 90,
            useRealParser: true);

        Assert.Equal("a", result.CapturedTranscript);
    }

    // Übernehmen und Spracheingabe beenden die Sammlung, ohne ihre Buchstaben zum Wert hinzuzufügen.
    [Theory]
    [InlineData("übernehmen")]
    [InlineData("spracheingabe")]
    public async Task SilentDoneEquivalent_FinalizesWithoutSpellingTheWord_WithRealParser(string equivalent)
    {
        var spoken = new List<string>();
        var result = await RunAsync(
            spoken,
            Captures((equivalent, 90)),
            spellOutAllowed: true,
            initialTranscript: "Anton",
            initialConfidence: 90,
            useRealParser: true);

        Assert.True(result.HasCapturedTranscript);
        Assert.Equal("a", result.CapturedTranscript);
    }

    // Buchstabieren aus wird hier als gewöhnlicher Text ausgewertet und zum Buchstabenpuffer hinzugefügt.
    [Fact]
    public async Task RemovedModeOffPhrase_IsSpelledLikeAnyWord_WithRealParser()
    {
        var spoken = new List<string>();
        var result = await RunAsync(
            spoken,
            Captures(("buchstabieren aus", 90), ("fertig", 90)),
            spellOutAllowed: true,
            initialTranscript: "Anton",
            initialConfidence: 90,
            useRealParser: true);

        Assert.Equal("abuchstabierenaus", result.CapturedTranscript);
    }

    // Nein ist hier kein Abschlusswort und bleibt buchstabierter Inhalt.
    [Fact]
    public async Task NoToken_StaysSpelledContent_WithRealParser()
    {
        var spoken = new List<string>();
        var result = await RunAsync(
            spoken,
            Captures(("nein", 90), ("fertig", 90)),
            spellOutAllowed: true,
            initialTranscript: "Anton",
            initialConfidence: 90,
            useRealParser: true);

        Assert.Equal("anein", result.CapturedTranscript);
    }

    // Nach der Nachfrage bei Stille soll die nächste Aufnahme nur das Hörsignal anfordern,
    // ohne denselben Aufnahmetext erneut anzufordern.
    [Fact]
    public async Task AfterSilenceRePrompt_NextCapture_SpeaksNoCapturePrompt()
    {
        var spoken = new List<string>();
        var presentations = new List<SpeechCapturePromptPresentation>();
        var captures = Captures(("Berta", 90), Silence, Silence);

        await RunAsync(spoken, captures, spellOutAllowed: true, initialTranscript: "Anton", initialConfidence: 90, presentations: presentations);

        Assert.Equal(3, presentations.Count);
        Assert.NotEqual(SpeechCapturePromptPresentation.ListeningCueOnly, presentations[0]);
        Assert.NotEqual(SpeechCapturePromptPresentation.ListeningCueOnly, presentations[1]);
        Assert.Equal(SpeechCapturePromptPresentation.ListeningCueOnly, presentations[2]);
    }

    // Bei unterdrücktem Buchstabenvorlesen bleibt die Aufforderung ohne Zeicheninhalt.
    // Sie wird hier nach dem Block und nach der ersten stillen Aufnahme angefordert.
    [Fact]
    public async Task AfterProtectedBlock_SpeaksContentFreeDonePrompt()
    {
        var spoken = new List<string>();
        var captures = Captures(Silence, Silence);

        await RunAsync(spoken, captures, spellOutAllowed: false, initialTranscript: "Anton", initialConfidence: 90);

        Assert.DoesNotContain(spoken, line => line.Contains("PHON[", StringComparison.Ordinal));
        Assert.Equal(2, spoken.Count(line => line == "Sagen Sie die nächsten Buchstaben, oder fertig."));
        Assert.DoesNotContain(spoken, line => line.Contains("zurück", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TheIncidentOf20260828_IsExplainableFromTheDecisionLines()
    {
        // Die aufgezeichneten Entscheidungen müssen Abschlussart und Pufferstand
        // für ein Transkript mit Buchstaben und Abschlusswort nachvollziehbar machen.
        var sink = new RecordingDialogDecisionSink();
        var spoken = new List<string>();

        var result = await RunAsync(
            spoken,
            Captures(),
            spellOutAllowed: true,
            initialTranscript: "Anton Berta fertig",
            initialConfidence: 90,
            useRealParser: true,
            decisionSink: sink);

        Assert.Equal("ab", result.CapturedTranscript);

        // Die Abschlussentscheidung zeigt, ob der Befehl allein oder mit weiteren Wörtern vorlag.
        var terminator = sink.Single(DialogDecisionNames.SpellingTerminator);
        Assert.Equal("false", RecordingDialogDecisionSink.Field(terminator, "isolated"));
        Assert.Equal("3", RecordingDialogDecisionSink.Field(terminator, "transcriptTokens"));

        // Der Pufferstand belegt die Anzahl der Zeichen beim Abschluss.
        var buffer = sink.Single(DialogDecisionNames.SpellingBuffer);
        Assert.Equal("2", RecordingDialogDecisionSink.Field(buffer, "characters"));
        // Die Buchstaben aus einer gemeinsamen Äußerung bilden einen Block.
        Assert.Equal("1", RecordingDialogDecisionSink.Field(buffer, "blocks"));
    }

    [Fact]
    public async Task WithoutADecisionSink_TheTurnIsUnchanged()
    {
        // Auch ohne Aufzeichnung der Entscheidungen muss derselbe erfasste Wert zurückgegeben werden.
        var spoken = new List<string>();

        var result = await RunAsync(
            spoken,
            Captures(),
            spellOutAllowed: true,
            initialTranscript: "Anton Berta fertig",
            initialConfidence: 90,
            useRealParser: true);

        Assert.Equal("ab", result.CapturedTranscript);
    }

    private static async Task<CharacterAccumulationResult> RunAsync(
        List<string> spoken,
        Queue<(string? Transcript, int? Confidence)> captures,
        bool spellOutAllowed,
        string initialTranscript,
        int? initialConfidence,
        Mock<IFormTranscriptParser>? parser = null,
        string isoCode = "de",
        bool useRealParser = false,
        IDialogDecisionSink? decisionSink = null,
        List<SpeechCapturePromptPresentation>? presentations = null)
    {
        parser ??= new Mock<IFormTranscriptParser>(MockBehavior.Loose);
        parser
            .Setup(p => p.ParseSpelling(It.IsAny<string?>(), It.IsAny<string>()))
            .Returns<string?, string>((_, transcript) => LetterMap.TryGetValue(transcript.Trim(), out var letter)
                ? FormTranscriptParseOutcome.Succeeded(letter)
                : FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized));

        // Der Mock erkennt nur vollständige Einträge der Testtabelle. Für kombinierte
        // Buchstaben- und Abschlussformulierungen wird der echte Parser verwendet.
        var effectiveParser = useRealParser
            ? (IFormTranscriptParser)new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver())
            : parser.Object;

        var speech = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
        speech
            .Setup(s => s.SpeakSafeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, VoiceDialogSettings, CancellationToken>((text, _, _, _) =>
            {
                spoken.Add(text);
                return Task.CompletedTask;
            });
        // Die Aufnahme enthält die für diesen Buchstabierschritt bestimmten Erkennungshinweise.
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
                (_, _, settings, onStartListening, _, _, _, _) =>
                {
                    presentations?.Add(settings.PromptPresentation);
                    onStartListening();
                    var (transcript, confidence) = captures.Dequeue();
                    if (transcript == RecognizerFault.Transcript)
                    {
                        return Task.FromException<SpeechCaptureResult>(new SpeechCaptureStreamStalledException());
                    }

                    return transcript is null
                        ? Task.FromException<SpeechCaptureResult>(new NoSpeechDetectedException())
                        : Task.FromResult(new SpeechCaptureResult(transcript, confidence));
                });
        speech.ForwardSingleAttemptCaptureToValueCapture();

        var renderer = new Mock<IPhoneticSpellOutRenderer>(MockBehavior.Loose);
        renderer
            .Setup(r => r.Render(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((value, _) => $"PHON[{value}]");

        var privacy = new Mock<IFormDialogPrivacyReadbackPolicy>(MockBehavior.Loose);
        privacy
            .Setup(p => p.IsSpellOutAllowedInConfirmationAsync(It.IsAny<string>()))
            .ReturnsAsync(spellOutAllowed);

        var evaluationTelemetry = new Mock<IEvaluationFormTelemetry>(MockBehavior.Loose).Object;
        var service = new FormVoiceCommandSpellingWorkflowService(
            new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
            AnchoredCommandFlowTestFactory.Create(),
            new SpellingWorkflowCommandResolver(
                new SpellingWorkflowLocalCommandMapper(),
                new SpellingWorkflowGlobalCommandMapper()),
            effectiveParser,
            speech.Object,
            new SpellingWorkflowCommandActionFlow(
                evaluationTelemetry,
                speech.Object,
                new SpellingWorkflowNoValuePrompt(speech.Object)),
            new SpellingWorkflowInputRejectionFlow(
                new WorkflowUnrecognizedInputAnnouncer(evaluationTelemetry, speech.Object)),
            renderer.Object,
            decisionSink);

        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose).Object;
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            new VoiceDialogSettings(),
            new Mock<IFormVoiceCommandPromptService>(MockBehavior.Loose).Object,
            new Mock<IFormVoiceCommandDialogStateTracker>(MockBehavior.Loose).Object,
            new Mock<IFormDialogRuntime>(MockBehavior.Loose).Object,
            "de");

        return await service.CaptureSpellingTranscriptAsync(
            SpellingWorkflowTestFactory.CreateContext(isoCode),
            new Mock<IFormDialogRuntime>(MockBehavior.Loose).Object,
            SpellingWorkflowTestFactory.CreateField(),
            session,
            ArmFieldLoop(new FormVoiceCommandStateTracker(telemetry, session)),
            autoHelp,
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            privacy.Object,
            initialTranscript,
            initialConfidence,
            CancellationToken.None);
    }
    private static FormVoiceCommandStateTracker ArmFieldLoop(FormVoiceCommandStateTracker tracker)
    {
        tracker.BeginFieldPrompt("main");
        tracker.BeginListening();
        return tracker;
    }

}
