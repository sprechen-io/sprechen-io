using Moq;
using Sprechen.Mda.Contracts.V1.Catalog;
using Xunit.Abstractions;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die schrittweise Datumseingabe mit vorgegebenen Transkripten und Konfidenzen.
/// Abgedeckt sind Reihenfolge, ungültige Komponenten, vollständige Datumsangaben,
/// Abbruch und erneute Erfassung nach einer Meldung ohne Sprache.
/// Geprüft werden ISO-Datum, Ausgabetexte und Konfidenz; die spätere Bestätigung ist nicht Teil dieses Tests.
/// </summary>
public sealed class DateComponentEntryWorkflowDriverTests
{
    private readonly ITestOutputHelper _output;

    public DateComponentEntryWorkflowDriverTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task GermanOrder_DayMonthYear_AssemblesIsoDate()
    {
        var run = await RunAsync("de", initialTranscript: "", "8", "1", "1980");

        Assert.True(run.Result.HasCapturedValue);
        Assert.Equal("1980-01-08", run.Result.CapturedIsoDate);
    }

    [Fact]
    public async Task EnglishOrder_MonthFirst_AssemblesIsoDate()
    {
        // Für Englisch werden Monat, Tag und Jahr erfragt. Die Zuordnung zu Komponenten
        // hält das zusammengesetzte Datum unabhängig von der Reihenfolge eindeutig.
        var run = await RunAsync("en", initialTranscript: "", "1", "8", "1980");

        Assert.Equal("1980-01-08", run.Result.CapturedIsoDate);
    }

    [Fact]
    public async Task OutOfRangeDay_ReasksOnlyTheDay_WithReason()
    {
        var run = await RunAsync("de", initialTranscript: "", "35", "8", "1", "1980");

        Assert.Equal("1980-01-08", run.Result.CapturedIsoDate);
        // Der Hinweis muss den gültigen Bereich des Tages nennen.
        Assert.Contains(run.Prompts, p => p.Contains("1 und 31", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task BareTwoDigitYear_ReasksForFourDigits()
    {
        var run = await RunAsync("de", initialTranscript: "", "8", "1", "80", "1980");

        Assert.Equal("1980-01-08", run.Result.CapturedIsoDate);
        Assert.Contains(run.Prompts, p => p.Contains("vierstellig", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ImpossibleAssembledDate_ReasksDay_PreservingMonthAndYear()
    {
        // Der 31. Februar ist ungültig. Der Tag wird erneut erfragt, Monat und Jahr bleiben erhalten.
        var run = await RunAsync("de", initialTranscript: "", "31", "2", "2023", "28");

        Assert.Equal("2023-02-28", run.Result.CapturedIsoDate);
        Assert.Contains(run.Prompts, p => p.Contains("gibt es nicht", StringComparison.OrdinalIgnoreCase));
        // Drei ursprüngliche Komponenten und der korrigierte Tag ergeben vier Erfassungen.
        Assert.Equal(4, run.CapturesConsumed);
    }

    [Fact]
    public async Task FullDateEscape_OnEntryUtterance_ShortCircuits()
    {
        var run = await RunAsync("de", initialTranscript: "achter Januar neunzehnhundertachtzig");

        Assert.Equal("1980-01-08", run.Result.CapturedIsoDate);
        Assert.Equal(0, run.CapturesConsumed);
    }

    [Fact]
    public async Task FullDateEscape_MidSubDialog_IsAccepted()
    {
        var run = await RunAsync("en", initialTranscript: "", "January eighth nineteen eighty");

        Assert.Equal("1980-01-08", run.Result.CapturedIsoDate);
    }

    [Fact]
    public async Task EveryTurn_CarriesTheExitCue()
    {
        var run = await RunAsync("de", initialTranscript: "", "8", "1", "1980");

        Assert.NotEmpty(run.Prompts);
        Assert.All(run.Prompts, p => Assert.Contains("abbrechen", p, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Silence_ReasksTheSlot_NeverStrands()
    {
        // Eine simulierte Meldung ohne Sprache lässt die Komponenteneingabe weiterlaufen.
        var run = await RunAsync("de", initialTranscript: "", null, "8", "1", "1980");

        Assert.True(run.Result.HasCapturedValue);
        Assert.Equal("1980-01-08", run.Result.CapturedIsoDate);
    }

    [Fact]
    public async Task CancelCommand_AbortsBackToFieldEntry()
    {
        var run = await RunAsync("de", initialTranscript: "", "abbrechen");

        Assert.True(run.Result.Aborted);
        Assert.False(run.Result.HasCapturedValue);
    }

    // Die Konfidenz ergibt sich aus dem Minimum der erhalten gebliebenen Komponenten.
    // Fehlversuche und später ersetzte Eingaben tragen nicht dazu bei.
    [Fact]
    public async Task Assembly_AggregatesTheMinimumSurvivingSlotConfidence()
    {
        var run = await RunAsync(
            "de",
            initialTranscript: "",
            initialConfidence: null,
            ("8", 90),
            ("1", 70),
            ("1980", 85));

        Assert.Equal("1980-01-08", run.Result.CapturedIsoDate);
        Assert.Equal(70, run.Result.AggregatedConfidencePercent);

        // Die Konfidenzstufe wird zusätzlich ausgegeben, aber nicht als Sollwert der Berechnung verwendet.
        var level = SttConfidenceGate.Evaluate(
            SpellingWorkflowTestFactory.CreateContext("de"),
            run.Result.AggregatedConfidencePercent);
        _output.WriteLine(
            $"gateBand corpus=1 aggregate={run.Result.AggregatedConfidencePercent} level={level} note=band-pressure-not-skipped-confirmation");
    }

    // Eine vollständige Datumsangabe ersetzt zuvor erfasste Komponenten samt deren Konfidenzen.
    [Fact]
    public async Task MidDialogEscape_DropsPriorSlotContributions_AggregateIsTheEscapeConfidenceAlone()
    {
        var run = await RunAsync(
            "de",
            initialTranscript: "",
            initialConfidence: null,
            ("8", 40),
            ("achter januar neunzehnhundertachtzig", 88));

        Assert.Equal("1980-01-08", run.Result.CapturedIsoDate);
        Assert.Equal(88, run.Result.AggregatedConfidencePercent);
    }

    // Wird bereits die erste Eingabe als vollständiges Datum erkannt, gilt ihre eigene Konfidenz.
    [Fact]
    public async Task InitialEscape_ContributesItsOwnConfidence()
    {
        var run = await RunAsync(
            "de",
            initialTranscript: "achter januar neunzehnhundertachtzig",
            initialConfidence: 62);

        Assert.Equal("1980-01-08", run.Result.CapturedIsoDate);
        Assert.Equal(62, run.Result.AggregatedConfidencePercent);
    }

    // Eine erste Eingabe ohne vollständiges Datum geht nicht in das Aggregat ein,
    // weil die Komponenten anschließend neu erfragt werden.
    [Fact]
    public async Task NonEscapeInitialTurn_DoesNotContributeItsConfidence()
    {
        var run = await RunAsync(
            "de",
            initialTranscript: "8",
            initialConfidence: 95,
            ("8", null),
            ("1", null),
            ("1980", null));

        Assert.Equal("1980-01-08", run.Result.CapturedIsoDate);
        Assert.Null(run.Result.AggregatedConfidencePercent);
    }

    // Fehlgeschlagene Versuche tragen keine Konfidenz bei. Die folgenden Tests prüfen
    // Werte außerhalb des Bereichs, unerkannte Eingaben und beide Fälle ungültiger Jahresangaben.
    [Theory]
    [InlineData("35", 10)]     // Außerhalb des gültigen Tagesbereichs.
    [InlineData("Quatsch", 5)] // Nicht als Komponente erkannt.
    public async Task OutOfRangeAndNotRecognizedReasks_DoNotContributeTheirConfidence(string failedAttempt, int failedConfidence)
    {
        var run = await RunAsync(
            "de",
            initialTranscript: "",
            initialConfidence: null,
            (failedAttempt, failedConfidence),
            ("8", 90),
            ("1", 90),
            ("1980", 90));

        Assert.Equal("1980-01-08", run.Result.CapturedIsoDate);
        Assert.Equal(90, run.Result.AggregatedConfidencePercent);
    }

    [Fact]
    public async Task YearNotFourDigitsReask_DoesNotContributeItsConfidence()
    {
        var run = await RunAsync(
            "de",
            initialTranscript: "",
            initialConfidence: null,
            ("8", 90),
            ("1", 90),
            ("80", 10),
            ("1980", 85));

        Assert.Equal("1980-01-08", run.Result.CapturedIsoDate);
        Assert.Equal(85, run.Result.AggregatedConfidencePercent);
    }

    // Bei italienischer Eingabe wird die zusammenhängende Jahreszahl erneut erfragt.
    // Nur die anschließende Eingabe getrennter Ziffern trägt ihre Konfidenz bei.
    [Fact]
    public async Task YearUncertainReask_DoesNotContributeItsConfidence()
    {
        var run = await RunAsync(
            "it",
            initialTranscript: "",
            initialConfidence: null,
            ("8", 90),
            ("1", 90),
            ("1980", 10),
            ("1 9 8 0", 85));

        Assert.Equal("1980-01-08", run.Result.CapturedIsoDate);
        Assert.Equal(85, run.Result.AggregatedConfidencePercent);
    }

    // Beim ungültigen 31. Februar werden Tag und Tageskonfidenz ersetzt; Monat und Jahr bleiben erhalten.
    [Fact]
    public async Task ImpossibleDateDiscard_DropsTheOldDayConfidence()
    {
        var run = await RunAsync(
            "de",
            initialTranscript: "",
            initialConfidence: null,
            ("31", 40),
            ("2", 90),
            ("2023", 90),
            ("28", 85));

        Assert.Equal("2023-02-28", run.Result.CapturedIsoDate);
        Assert.Equal(85, run.Result.AggregatedConfidencePercent);
    }

    // Ohne gemeldete Konfidenz der erhaltenen Komponenten bleibt das Aggregat unbekannt.
    // Die spätere Bestätigungsentscheidung wird hier nicht geprüft.
    [Fact]
    public async Task OnlyNullConfidences_YieldUnknownAggregateConfidence()
    {
        var run = await RunAsync(
            "de",
            initialTranscript: "",
            initialConfidence: null,
            ("8", null),
            ("1", null),
            ("1980", null));

        Assert.Equal("1980-01-08", run.Result.CapturedIsoDate);
        Assert.Null(run.Result.AggregatedConfidencePercent);
    }

    private static FormFieldSnapshot CreateDateField()
        => new(
            Identity: new FormFieldIdentitySnapshot(FieldId: "birthdate", VariableId: "birthdate", FieldGroupId: "main"),
            Presentation: new FormFieldPresentationSnapshot(
                FieldGroupTitle: "Main",
                Label: "Birthdate",
                HelpText: null,
                Examples: [],
                Options: []),
            Interaction: new FormFieldInteractionSnapshot(
                CodeStyle: false,
                InputMode: InputMode.Digits,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: ValueTypeKinds.Date,
                SpeakValueInConfirmation: true));

    private static Task<(DateComponentEntryResult Result, List<string> Prompts, int CapturesConsumed)> RunAsync(
        string iso,
        string initialTranscript,
        params string?[] steps)
        => RunAsync(
            iso,
            initialTranscript,
            initialConfidence: null,
            steps.Select<string?, (string? Transcript, int? Confidence)>(transcript => (transcript, 95)).ToArray());

    private static async Task<(DateComponentEntryResult Result, List<string> Prompts, int CapturesConsumed)> RunAsync(
        string iso,
        string initialTranscript,
        int? initialConfidence,
        params (string? Transcript, int? Confidence)[] steps)
    {
        var prompts = new List<string>();
        var captures = new Queue<(string? Transcript, int? Confidence)>(steps);
        var consumed = 0;

        var parser = new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver());

        var speech = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
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
                    prompts.Add(prompt);
                    onStartListening();
                    var (transcript, confidence) = captures.Count > 0 ? captures.Dequeue() : (null, null);
                    consumed++;
                    return transcript is null
                        ? Task.FromException<SpeechCaptureResult>(new NoSpeechDetectedException())
                        : Task.FromResult(new SpeechCaptureResult(transcript, confidence));
                });

        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose).Object;
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            new VoiceDialogSettings(),
            new Mock<IFormVoiceCommandPromptService>(MockBehavior.Loose).Object,
            new Mock<IFormVoiceCommandDialogStateTracker>(MockBehavior.Loose).Object,
            new Mock<IFormDialogRuntime>(MockBehavior.Loose).Object,
            iso);

        var driver = new DateComponentEntryWorkflowDriver(speech.Object, parser, parser);

        var result = await driver.RunAsync(
            SpellingWorkflowTestFactory.CreateContext(iso),
            CreateDateField(),
            session,
            ArmFieldLoop(new FormVoiceCommandStateTracker(telemetry, session)),
            autoHelp,
            initialTranscript,
            initialConfidence,
            CancellationToken.None);

        return (result, prompts, consumed);
    }
    private static FormVoiceCommandStateTracker ArmFieldLoop(FormVoiceCommandStateTracker tracker)
    {
        tracker.BeginFieldPrompt("main");
        tracker.BeginListening();
        return tracker;
    }

}
