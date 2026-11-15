using Sprechen.Speech.ModelRuntime.PromptTemplates;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Immutable;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Evaluation;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.TestDoubles;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft das schrittweise Sammeln mehrerer Auswahlwerte und dessen Abschluss.
/// Optionsfälle verwenden überwiegend einen Parser-Mock; Fälle mit zusammengesetzten
/// Äußerungen nutzen den echten Textparser. Sprachaufnahme und Ausgabe sind simuliert.
/// </summary>
public sealed class SetAccumulationStrategyTests
{
    private static readonly ImmutableArray<FormChoiceOption> ContactOptions =
    [
        new FormChoiceOption("email", "E-Mail"),
        new FormChoiceOption("phone", "Telefon"),
        new FormChoiceOption("sms", "SMS"),
    ];

    [Fact]
    public async Task PickOne_ThenRePrompt_ThenPickAnother_BothInSet_FinalizesOnDone()
    {
        var run = await RunAsync(Captures(("phone", 88), ("fertig", 95)), initialTranscript: "email", initialConfidence: 90);

        Assert.True(run.Result.HasCapturedTranscript);
        // Die kanonische Darstellung sortiert die gesammelten Werte und trennt sie mit |.
        Assert.Equal("email|phone", run.Result.CapturedTranscript);
        // Nur Beiträge zur Auswahl bestimmen deren minimale Konfidenz.
        Assert.Equal(88, run.Result.AggregatedConfidencePercent);
        // Die Aufforderung zwischen Auswahlschritten nennt übernehmen als Abschlussbefehl.
        Assert.Contains(run.CapturePrompts, prompt => prompt.Contains("übernehmen", StringComparison.OrdinalIgnoreCase));
        // Auch fertig kennzeichnet eine nichtleere Auswahl als ausdrücklich übernommen.
        Assert.True(run.Result.FinalizedByExplicitAccept);
    }

    [Fact]
    public async Task NamingAnotherOption_AddsToTheSet_InsteadOfReplacing()
    {
        var run = await RunAsync(Captures(("phone", 90), ("sms", 90), ("fertig", 90)), initialTranscript: "email", initialConfidence: 90);

        Assert.Equal("email|phone|sms", run.Result.CapturedTranscript);
    }

    [Fact]
    public async Task NonEmptySilence_AfterRePrompt_AlsoFinalizesTheSet()
    {
        // Zwei simulierte stille Aufnahmen schließen die bereits nichtleere Auswahl ab.
        var run = await RunAsync(Captures(Silence, Silence), initialTranscript: "email", initialConfidence: 90);

        Assert.Equal("email", run.Result.CapturedTranscript);
        Assert.Contains(run.Spoken, line => line.Contains("übernehmen", StringComparison.OrdinalIgnoreCase));
        // Ein Abschluss durch Stille setzt keine ausdrückliche Übernahme voraus;
        // die nachfolgende Bestätigung darf deshalb nicht übersprungen werden.
        Assert.False(run.Result.FinalizedByExplicitAccept);
    }

    [Theory]
    [InlineData("fertig")]
    [InlineData("absenden")]
    [InlineData("ich bin fertig")]
    public async Task DonePhrase_RoutedThroughSharedResolver_FinalizesTheSet(string donePhrase)
    {
        // Die gemeinsame Befehlsauswertung akzeptiert auch alternative Abschlussformulierungen.
        var run = await RunAsync(Captures((donePhrase, 99)), initialTranscript: "email", initialConfidence: 90);

        Assert.True(run.Result.HasCapturedTranscript);
        Assert.Equal("email", run.Result.CapturedTranscript);
        run.FormTelemetry.Verify(
            t => t.TrackRepairAttempt(It.IsAny<EvaluationSession>(), It.IsAny<string>(), SetWorkflowRepairEventCodes.UnrecognizedSelection),
            Times.Never);
    }

    [Theory]
    [InlineData("abbrechen")]
    [InlineData("hilfe")]
    [InlineData("xyz")]
    public async Task UnrecognizedOrGlobalCommandInLoop_IsHonouredViaSharedRejectionPath_NotSwallowed(string transcript)
    {
        // Die hier unaktivierten Wörter abbrechen und hilfe werden wie eine unbekannte
        // Auswahl behandelt. Aktivierte globale Befehle sind davon zu unterscheiden.
        var run = await RunAsync(Captures((transcript, 90), ("fertig", 95)), initialTranscript: "email", initialConfidence: 90);

        Assert.Equal("email", run.Result.CapturedTranscript);
        // Geprüft werden die Fehlermeldungen an die Telemetrie und ein angeforderter Ausgabetext.
        run.FormTelemetry.Verify(
            t => t.TrackConstraintViolation(
                It.IsAny<EvaluationSession>(),
                "contactChannels",
                FormVoiceCommandFieldErrorCodes.UnrecognizedInput,
                It.IsAny<string>(),
                TelemetryErrorTriggers.SystemRecognition),
            Times.AtLeastOnce);
        run.FormTelemetry.Verify(
            t => t.TrackRepairAttempt(It.IsAny<EvaluationSession>(), "contactChannels", SetWorkflowRepairEventCodes.UnrecognizedSelection),
            Times.AtLeastOnce);
        Assert.NotEmpty(run.Spoken);
    }

    [Fact]
    public async Task RepeatInLoop_ReadsBackCurrentSelection_NotRoutedToRejection()
    {
        // Wiederholen fordert einen Text mit der bisherigen Auswahl an, ohne den Wert zu ändern.
        var run = await RunAsync(Captures(("wiederholen", 90), ("fertig", 95)), initialTranscript: "email", initialConfidence: 90);

        Assert.Equal("email", run.Result.CapturedTranscript);
        Assert.Contains(run.Spoken, line => line.Contains("E-Mail", StringComparison.OrdinalIgnoreCase));
        run.FormTelemetry.Verify(
            t => t.TrackRepairAttempt(It.IsAny<EvaluationSession>(), "contactChannels", SetWorkflowRepairEventCodes.ReadbackSelection),
            Times.AtLeastOnce);
        run.FormTelemetry.Verify(
            t => t.TrackRepairAttempt(It.IsAny<EvaluationSession>(), It.IsAny<string>(), SetWorkflowRepairEventCodes.UnrecognizedSelection),
            Times.Never);
    }

    [Fact]
    public async Task MixedUtterance_OptionsAndDoneInOneTurn_SelectsAllAndCompletes_WithRealParser()
    {
        // Der echte Parser muss die Optionen übernehmen, bevor der Abschlussbefehl
        // in derselben Äußerung ausgewertet wird.
        var run = await RunAsync(
            Captures(),
            initialTranscript: "E-Mail, sms, fertig.",
            initialConfidence: 92,
            useRealParser: true);

        Assert.True(run.Result.HasCapturedTranscript);
        Assert.Equal("email|sms", run.Result.CapturedTranscript);
    }

    [Fact]
    public async Task CommaSeparatedOptions_WithoutDone_SelectBoth_WithRealParser()
    {
        // Kommas trennen mehrere Optionen innerhalb desselben Transkripts.
        var run = await RunAsync(
            Captures(("fertig", 95)),
            initialTranscript: "E-Mail, Telefon",
            initialConfidence: 90,
            useRealParser: true);

        Assert.Equal("email|phone", run.Result.CapturedTranscript);
    }

    [Fact]
    public async Task CommaLessOptionAndDone_SelectsTheRidingOptionAndCompletes_WithRealParser()
    {
        // Auch ohne Komma muss vor dem Abschlusswort stehendes SMS als Option erhalten bleiben.
        var run = await RunAsync(
            Captures(),
            initialTranscript: "Sms fertig",
            initialConfidence: 93,
            useRealParser: true);

        Assert.True(run.Result.HasCapturedTranscript);
        Assert.Equal("sms", run.Result.CapturedTranscript);
    }

    [Fact]
    public async Task CommaLessMultipleOptionsAndDone_SelectsAllAndCompletes_WithRealParser()
    {
        // Nach Entfernen des Abschlussworts müssen alle verbleibenden Wörter
        // vollständig auf die benachbarten Optionsbezeichnungen passen.
        var run = await RunAsync(
            Captures(),
            initialTranscript: "E mail sms fertig",
            initialConfidence: 90,
            useRealParser: true);

        Assert.True(run.Result.HasCapturedTranscript);
        Assert.Equal("email|sms", run.Result.CapturedTranscript);
    }

    [Fact]
    public async Task CommaLessUtteranceWithUnmatchedRemainder_SelectsNothingFromIt_WithRealParser()
    {
        // Der unbekannte Rest blabla verhindert eine teilweise Übernahme dieser Äußerung.
        // Erst der spätere gültige Wert darf zur Auswahl beitragen.
        var run = await RunAsync(
            Captures(("email", 90), ("fertig", 95)),
            initialTranscript: "Sms blabla fertig",
            initialConfidence: 90,
            useRealParser: true);

        Assert.Equal("email", run.Result.CapturedTranscript);
        run.FormTelemetry.Verify(
            t => t.TrackRepairAttempt(It.IsAny<EvaluationSession>(), "contactChannels", SetWorkflowRepairEventCodes.NoValue),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task MultiWordLabelContainingTheDoneWord_IsSelectedWhole_NotDecomposed_WithRealParser()
    {
        // Fine ist hier sowohl Abschlusswort als auch Bestandteil der Optionsbezeichnung.
        // Die vollständige Bezeichnung Fine settimana muss als Option erhalten bleiben.
        var run = await RunAsync(
            Captures(("fine", 95)),
            initialTranscript: "Fine settimana",
            initialConfidence: 90,
            useRealParser: true,
            isoCode: "it",
            options:
            [
                new FormChoiceOption("weekend", "Fine settimana"),
                new FormChoiceOption("news", "Notizie"),
            ]);

        Assert.True(run.Result.HasCapturedTranscript);
        Assert.Equal("weekend", run.Result.CapturedTranscript);
    }

    [Fact]
    public async Task RejectedTurnLowConfidence_DoesNotPoisonTheAggregatedConfidence()
    {
        // Die zurückgewiesene Eingabe trägt keinen Auswahlwert bei und darf deshalb
        // die minimale Konfidenz der Auswahl nicht senken.
        var run = await RunAsync(
            Captures(("xyz", 20), ("fertig", 95)),
            initialTranscript: "email",
            initialConfidence: 90);

        Assert.Equal("email", run.Result.CapturedTranscript);
        Assert.Equal(90, run.Result.AggregatedConfidencePercent);
    }

    [Fact]
    public async Task DoneOnEmpty_SpeaksNoneSelectedYet_NeverRendersEmptySelectedList_AndDoesNotComplete()
    {
        // Bei einer erforderlichen Auswahl wird ein vorzeitiger Abschluss abgewiesen.
        // Der angeforderte Text muss den leeren Zustand benennen, ohne eine leere Liste auszugeben.
        var run = await RunAsync(Captures(("email", 90), ("fertig", 95)), initialTranscript: "fertig", initialConfidence: null);

        Assert.Equal("email", run.Result.CapturedTranscript);
        Assert.Contains(run.Spoken, line => line.Contains("Noch nichts gewählt", StringComparison.Ordinal));
        Assert.Contains(run.Spoken, line => line.Contains("E-Mail", StringComparison.Ordinal));
        foreach (var line in run.Spoken.Concat(run.CapturePrompts))
        {
            Assert.DoesNotContain("Gewählt: .", line, StringComparison.Ordinal);
            Assert.DoesNotContain("Gewählt:.", line, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task RepeatedDoneOnEmpty_ClimbsTheAutoHelpLadder_InsteadOfLoopingForever()
    {
        // Wiederholtes Abschließen ohne Auswahl soll die automatische Feldhilfe auslösen.
        var run = await RunAsync(
            Captures(("fertig", 95), ("fertig", 95), ("email", 90), ("fertig", 95)),
            initialTranscript: "fertig",
            initialConfidence: null);

        Assert.Equal("email", run.Result.CapturedTranscript);
        run.HelpPromptService.Verify(
            s => s.SpeakHelpForFocusedFieldAsync(
                It.IsAny<IFormDialogRuntime>(),
                It.IsAny<IFormVoiceCommandDialogStateTracker>(),
                It.IsAny<string>(),
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task EmptySetDone_SpeaksNoValueCue_AndTracksRepair_NotSilent()
    {
        // Ein Abschluss ohne erforderliche Auswahl meldet NoValue und lässt eine weitere Eingabe zu.
        var run = await RunAsync(Captures(("email", 90), ("fertig", 95)), initialTranscript: "fertig", initialConfidence: null);

        Assert.Equal("email", run.Result.CapturedTranscript);
        Assert.NotEmpty(run.Spoken);
        run.FormTelemetry.Verify(
            t => t.TrackRepairAttempt(It.IsAny<EvaluationSession>(), "contactChannels", SetWorkflowRepairEventCodes.NoValue),
            Times.AtLeastOnce);
        run.FormTelemetry.Verify(
            t => t.TrackRepairAttempt(It.IsAny<EvaluationSession>(), It.IsAny<string>(), SetWorkflowRepairEventCodes.UnrecognizedSelection),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(null)]
    public async Task DoneOnEmpty_OptionalSet_CompletesWithEmptySelection_InsteadOfRefusing(int? minSelected)
    {
        // Ohne Mindestanzahl darf eine leere Auswahl abgeschlossen werden.
        // Dieser Fall benötigt keine weitere Aufnahme.
        var run = await RunAsync(
            Captures(),
            initialTranscript: "fertig",
            initialConfidence: 95,
            minSelected: minSelected);

        Assert.True(run.Result.HasCapturedTranscript);
        // Eine leere Auswahl wird kanonisch als leere Zeichenfolge dargestellt.
        Assert.Equal(string.Empty, run.Result.CapturedTranscript);
        // Auch die leere optionale Auswahl bleibt ohne Kennzeichen für ausdrückliche Übernahme.
        Assert.False(run.Result.FinalizedByExplicitAccept);
        // Der zulässige leere Abschluss darf keine NoValue-Meldung auslösen.
        Assert.DoesNotContain(run.Spoken, line => line.Contains("Noch nichts gewählt", StringComparison.Ordinal));
        run.FormTelemetry.Verify(
            t => t.TrackRepairAttempt(It.IsAny<EvaluationSession>(), It.IsAny<string>(), SetWorkflowRepairEventCodes.NoValue),
            Times.Never);
    }

    [Fact]
    public async Task DoneOnEmpty_RequiredSet_MinimumOne_StillRefusesWithNoneSelectedYet()
    {
        // Bei Mindestanzahl eins bleibt der leere Abschluss unzulässig.
        var run = await RunAsync(
            Captures(("email", 90), ("fertig", 95)),
            initialTranscript: "fertig",
            initialConfidence: 95,
            minSelected: 1);

        Assert.Equal("email", run.Result.CapturedTranscript);
        Assert.Contains(run.Spoken, line => line.Contains("Noch nichts gewählt", StringComparison.Ordinal));
        run.FormTelemetry.Verify(
            t => t.TrackRepairAttempt(It.IsAny<EvaluationSession>(), "contactChannels", SetWorkflowRepairEventCodes.NoValue),
            Times.AtLeastOnce);
    }

    [Theory]
    [InlineData("de", "übernehmen")]
    [InlineData("en", "accept")]
    [InlineData("fr", "accepter")]
    [InlineData("es", "aceptar")]
    [InlineData("it", "accettare")]
    public void AcceptWord_InSetContext_ResolvesToAcceptSelection(string isoCode, string acceptWord)
    {
        // Die Übernahmeformulierungen müssen auch im Auswahlablauf durch die gemeinsame
        // Befehlsauswertung auf AcceptSelection abgebildet werden.
        var resolver = new SetWorkflowCommandResolver(new SetWorkflowGlobalCommandMapper());

        var resolved = resolver.TryResolve(
            SpellingWorkflowTestFactory.CreateContext(isoCode),
            acceptWord,
            out var command,
            out var isIsolated);

        Assert.True(resolved);
        Assert.Equal(SetWorkflowCommand.AcceptSelection, command);
        Assert.True(isIsolated);
    }

    [Fact]
    public async Task AcceptWord_OnNonEmptySelection_FinalizesWithExplicitAcceptFlag()
    {
        var run = await RunAsync(Captures(("übernehmen", 95)), initialTranscript: "email", initialConfidence: 90);

        Assert.True(run.Result.HasCapturedTranscript);
        Assert.Equal("email", run.Result.CapturedTranscript);
        Assert.True(run.Result.FinalizedByExplicitAccept);
    }

    [Fact]
    public async Task RidingAccept_OptionsAndAcceptInOneBreath_HarvestsThenFinalizesOnce_WithFlag()
    {
        // Optionen und Übernahmebefehl im selben Transkript werden mit dem echten Parser ausgewertet.
        // Die Optionen müssen im zurückgegebenen Wert enthalten sein.
        var run = await RunAsync(
            Captures(),
            initialTranscript: "E-Mail, sms, übernehmen.",
            initialConfidence: 92,
            useRealParser: true);

        Assert.True(run.Result.HasCapturedTranscript);
        Assert.Equal("email|sms", run.Result.CapturedTranscript);
        Assert.True(run.Result.FinalizedByExplicitAccept);
    }

    [Fact]
    public async Task CommaLessOptionAndAccept_RecoversTheRidingOption_WithRealParser()
    {
        // Auch ohne Komma werden SMS und der folgende Übernahmebefehl getrennt ausgewertet.
        var run = await RunAsync(
            Captures(),
            initialTranscript: "Sms übernehmen",
            initialConfidence: 93,
            useRealParser: true);

        Assert.True(run.Result.HasCapturedTranscript);
        Assert.Equal("sms", run.Result.CapturedTranscript);
        Assert.True(run.Result.FinalizedByExplicitAccept);
    }

    [Fact]
    public async Task ExtendThenAccept_NameMoreOptionsAtTheConfirmTurn_ThenAccept_AllInSet()
    {
        var run = await RunAsync(
            Captures(("sms", 90), ("übernehmen", 95)),
            initialTranscript: "email",
            initialConfidence: 90);

        Assert.Equal("email|sms", run.Result.CapturedTranscript);
        Assert.True(run.Result.FinalizedByExplicitAccept);
        Assert.Contains(run.CapturePrompts, prompt => prompt.Contains("übernehmen", StringComparison.OrdinalIgnoreCase));
    }

    // Die Aufnahme erhält die Optionsbezeichnungen als Erkennungshinweise
    // für die aktuelle Auswahl.
    [Fact]
    public async Task SetCapture_FeedsOptionDisplayNamesAsKeyterms_OnEachAccumulateTurn()
    {
        SpeechKeyterms? capturedKeyterms = null;
        var formTelemetry = new Mock<IEvaluationFormTelemetry>(MockBehavior.Loose);
        var parser = new Mock<IFormTranscriptParser>(MockBehavior.Loose);
        parser
            .Setup(p => p.ParseChoice(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<ImmutableArray<FormChoiceOption>>(), true))
            .Returns<string?, string, ImmutableArray<FormChoiceOption>, bool>((_, transcript, _, _) =>
                ContactOptions.Any(option => string.Equals(option.Code, transcript.Trim(), StringComparison.OrdinalIgnoreCase))
                    ? FormTranscriptParseOutcome.Succeeded(transcript.Trim().ToLowerInvariant())
                    : FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized));

        var speech = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
        // Die simulierte Folgeaufnahme liefert fertig und beendet dadurch die Sammlung.
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
                (_, _, _, onStartListening, _, _, keyterms, _) =>
                {
                    capturedKeyterms = keyterms;
                    onStartListening();
                    return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("fertig", null));
                });
        speech.ForwardSingleAttemptCaptureToValueCapture();

        var service = new FormVoiceCommandSetWorkflowService(
            new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
            AnchoredCommandFlowTestFactory.Create(),
            new SetWorkflowCommandResolver(new SetWorkflowGlobalCommandMapper()),
            new SetWorkflowCommandActionFlow(formTelemetry.Object),
            new SetWorkflowInputRejectionFlow(
                new WorkflowUnrecognizedInputAnnouncer(formTelemetry.Object, speech.Object)),
            parser.Object,
            speech.Object);

        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose).Object;
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            new VoiceDialogSettings(),
            new Mock<IFormVoiceCommandPromptService>(MockBehavior.Loose).Object,
            new Mock<IFormVoiceCommandDialogStateTracker>(MockBehavior.Loose).Object,
            new Mock<IFormDialogRuntime>(MockBehavior.Loose).Object,
            "de");

        await service.CaptureSetTranscriptAsync(
            SpellingWorkflowTestFactory.CreateContext("de"),
            new Mock<IFormDialogRuntime>(MockBehavior.Loose).Object,
            CreateSetField(minSelected: 1, ContactOptions),
            session,
            ArmFieldLoop(new FormVoiceCommandStateTracker(telemetry, session)),
            autoHelp,
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            initialTranscript: "email",
            initialConfidencePercent: null,
            CancellationToken.None);

        Assert.NotNull(capturedKeyterms);
        // Die Optionsbezeichnungen stehen zuerst. Hinzu kommen das Verbindungswort und
        // die Auswahlbefehle; die serverseitigen Basisbegriffe werden nicht ergänzt.
        Assert.Equal(new[] { "E-Mail", "Telefon", "SMS" }, capturedKeyterms!.Terms.Take(3));
        Assert.Contains("und", capturedKeyterms.Terms);
        Assert.Contains("übernehmen", capturedKeyterms.Terms);
        Assert.DoesNotContain("abbrechen", capturedKeyterms.Terms);
        Assert.Equal(KeytermComposition.WithoutServerBase, capturedKeyterms.Composition);
    }


    [Fact]
    public async Task RecognizerNeverRecovers_TheRealSetStrategySpeaksTheTruthfulSentence()
    {
        // Der echte Auswahlablauf muss bei wiederholtem Erkennungsausfall
        // den passenden Fehlertext beim Sprachausgabedienst anfordern.
        var run = await RunAsync(
            Captures(RecognizerFault, RecognizerFault, RecognizerFault, RecognizerFault, RecognizerFault),
            initialTranscript: "email",
            initialConfidence: 90);

        // Ohne abgeschlossene Erkennung wird kein erfasster Auswahlwert zurückgegeben.
        Assert.False(run.Result.HasCapturedTranscript);
        Assert.Contains(
            RecoverableCapturePromptTextResolver.GetRecognizerUnavailablePrompt("de", PolitenessLevel.Formal),
            run.Spoken);
    }

    // Eine zurückgewiesene Anfangseingabe trägt keinen Wert bei und wird
    // bei der Konfidenz der späteren Auswahl nicht berücksichtigt.
    [Fact]
    public async Task RejectedInitialTurn_DoesNotContributeItsConfidence()
    {
        var run = await RunAsync(
            Captures(("email", 90), ("fertig", 95)),
            initialTranscript: "murmur",
            initialConfidence: 30);

        Assert.Equal("email", run.Result.CapturedTranscript);
        Assert.Equal(90, run.Result.AggregatedConfidencePercent);
    }

    // Auch ein anfänglicher Wiederholungsbefehl trägt keinen Auswahlwert bei.
    // Die Telemetrieprüfung unterscheidet ihn von einer unbekannten Eingabe.
    [Fact]
    public async Task CommandOnlyInitialTurn_DoesNotContributeItsConfidence()
    {
        var run = await RunAsync(
            Captures(("email", 90), ("fertig", 95)),
            initialTranscript: "wiederholen",
            initialConfidence: 30);

        Assert.Equal("email", run.Result.CapturedTranscript);
        Assert.Equal(90, run.Result.AggregatedConfidencePercent);
        run.FormTelemetry.Verify(
            t => t.TrackRepairAttempt(
                It.IsAny<EvaluationSession>(),
                It.IsAny<string>(),
                SetWorkflowRepairEventCodes.ReadbackSelection),
            Times.Once);
    }

    // Eine erneut genannte Option ergänzt keinen Wert und senkt daher nicht die Konfidenz.
    [Fact]
    public async Task DuplicateOptionTurn_DoesNotContributeItsConfidence()
    {
        var run = await RunAsync(
            Captures(("email", 30), ("fertig", 95)),
            initialTranscript: "email",
            initialConfidence: 90);

        Assert.Equal("email", run.Result.CapturedTranscript);
        Assert.Equal(90, run.Result.AggregatedConfidencePercent);
    }

    // Ein tatsächlich beitragender Anfangswert geht dagegen in die minimale Konfidenz ein.
    [Fact]
    public async Task ContributingInitialTurn_FoldsItsConfidenceLikeAnyContributingTurn()
    {
        var run = await RunAsync(
            Captures(("phone", 88), ("fertig", 95)),
            initialTranscript: "email",
            initialConfidence: 60);

        Assert.Equal("email|phone", run.Result.CapturedTranscript);
        Assert.Equal(60, run.Result.AggregatedConfidencePercent);
    }

    private static readonly (string? Transcript, int? Confidence) Silence = (null, null);

    /// <summary>
    /// Kennzeichnet einen simulierten Erkennungsausfall, den der Ablauf anders
    /// als die durch <see cref="Silence"/> dargestellte stille Aufnahme behandelt.
    /// </summary>
    private static readonly (string? Transcript, int? Confidence) RecognizerFault = ("__fault__", null);

    private static Queue<(string? Transcript, int? Confidence)> Captures(
        params (string? Transcript, int? Confidence)[] steps)
        => new(steps);

    private static async Task<(
        CharacterAccumulationResult Result,
        List<string> Spoken,
        List<string> CapturePrompts,
        Mock<IEvaluationFormTelemetry> FormTelemetry,
        Mock<IFormVoiceCommandPromptService> HelpPromptService)> RunAsync(
        Queue<(string? Transcript, int? Confidence)> captures,
        string initialTranscript,
        int? initialConfidence,
        bool useRealParser = false,
        int? minSelected = 1,
        string isoCode = "de",
        ImmutableArray<FormChoiceOption>? options = null)
    {
        var effectiveOptions = options ?? ContactOptions;
        var spoken = new List<string>();
        var capturePrompts = new List<string>();
        var formTelemetry = new Mock<IEvaluationFormTelemetry>(MockBehavior.Loose);

        var parser = new Mock<IFormTranscriptParser>(MockBehavior.Loose);
        parser
            .Setup(p => p.ParseChoice(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<ImmutableArray<FormChoiceOption>>(), true))
            .Returns<string?, string, ImmutableArray<FormChoiceOption>, bool>((_, transcript, _, _) =>
                effectiveOptions.Any(option => string.Equals(option.Code, transcript.Trim(), StringComparison.OrdinalIgnoreCase))
                    ? FormTranscriptParseOutcome.Succeeded(transcript.Trim().ToLowerInvariant())
                    : FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized));

        // Der echte Parser prüft die Zerlegung zusammengesetzter Transkripte.
        // Der Mock erkennt dagegen ausschließlich vollständige Optionscodes.
        var effectiveParser = useRealParser
            ? new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver())
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
                (prompt, _, _, onStartListening, _, _, _, _) =>
                {
                    capturePrompts.Add(prompt);
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
        // Die einzelne Aufnahme wird an die oben eingerichtete Folge vorgegebener Ergebnisse weitergeleitet.

        var service = new FormVoiceCommandSetWorkflowService(
            new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
            AnchoredCommandFlowTestFactory.Create(),
            new SetWorkflowCommandResolver(new SetWorkflowGlobalCommandMapper()),
            new SetWorkflowCommandActionFlow(formTelemetry.Object),
            new SetWorkflowInputRejectionFlow(
                new WorkflowUnrecognizedInputAnnouncer(formTelemetry.Object, speech.Object)),
            effectiveParser,
            speech.Object);

        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose).Object;
        var helpPromptService = new Mock<IFormVoiceCommandPromptService>(MockBehavior.Loose);
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            new VoiceDialogSettings(),
            helpPromptService.Object,
            new Mock<IFormVoiceCommandDialogStateTracker>().Object,
            new Mock<IFormDialogRuntime>(MockBehavior.Loose).Object,
            isoCode);

        var result = await service.CaptureSetTranscriptAsync(
            SpellingWorkflowTestFactory.CreateContext(isoCode),
            new Mock<IFormDialogRuntime>(MockBehavior.Loose).Object,
            CreateSetField(minSelected, effectiveOptions),
            session,
            ArmFieldLoop(new FormVoiceCommandStateTracker(telemetry, session)),
            autoHelp,
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            initialTranscript,
            initialConfidence,
            CancellationToken.None);

        return (result, spoken, capturePrompts, formTelemetry, helpPromptService);
    }

    private static FormFieldSnapshot CreateSetField(int? minSelected = 1, ImmutableArray<FormChoiceOption>? options = null)
        => new(
            Identity: new FormFieldIdentitySnapshot(FieldId: "contactChannels", VariableId: "contactChannels", FieldGroupId: "main"),
            Presentation: new FormFieldPresentationSnapshot(
                FieldGroupTitle: "Main",
                Label: "Kontaktkanaele",
                HelpText: null,
                Examples: [],
                Options: options ?? ContactOptions),
            Interaction: new FormFieldInteractionSnapshot(
                CodeStyle: false,
                InputMode: InputMode.Voice,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: ValueTypeKinds.Set,
                SpeakValueInConfirmation: true)
            {
                MinSelected = minSelected,
                MaxSelected = 3
            });
    private static FormVoiceCommandStateTracker ArmFieldLoop(FormVoiceCommandStateTracker tracker)
    {
        tracker.BeginFieldPrompt("main");
        tracker.BeginListening();
        return tracker;
    }

}
