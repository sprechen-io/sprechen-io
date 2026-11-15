using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class FormsCatalogLanguageChangeConfirmationRunnerTests
{
    private const string CaptureFailedTerminalText = "MODELED-CAPTURE-FAILED-TERMINAL";

    [Theory]
    [InlineData("it", "si")]
    [InlineData("it", "corretto")]
    [InlineData("fr", "oui")]
    [InlineData("es", "si")]
    public async Task ConfirmAsync_WhenTheAnswerIsGivenInTheTargetLanguage_Confirms(
        string targetIsoCode,
        string transcript)
    {
        // Die angebotene Sprache wird mit Eigenbezeichnung in dieser Sprache bestätigt; die Antwort darf ebenfalls in ihr erfolgen.
        var speechClient = new ScriptedSpeechInputClient();
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var workflow = CreateLanguageChangeWorkflow();
        var runner = CreateConfirmationRunner(speechClient, textToSpeechPlayer, workflow: workflow);
        var context = CreateTurnContext(maxRetries: 2);

        await EnqueueTranscriptAsync(speechClient, transcript);

        var result = await runner.ConfirmAsync(context, targetIsoCode, CancellationToken.None);

        Assert.Equal(FormsCatalogLanguageChangeDecision.Confirmed, result);
        var spokenCalls = textToSpeechPlayer.Calls.ToArray();
        Assert.Equal(targetIsoCode, spokenCalls[0].LanguageCode);
        Assert.Contains(workflow.GetEndonym(targetIsoCode), spokenCalls[0].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConfirmAsync_WhenTheAnswerIsGivenInTheSessionLanguage_StillConfirms()
    {
        // Auch Ja/Nein in der bisherigen Sitzungssprache bleibt zulässig. Deren Wörter
        // werden zusätzlich als Erkennungshinweise übertragen, während die Erkennung in der Zielsprache läuft.
        var speechClient = new ScriptedSpeechInputClient();
        var runner = CreateConfirmationRunner(speechClient, new RecordingTextToSpeechPlayer());
        var context = CreateTurnContext(maxRetries: 2);

        await EnqueueTranscriptAsync(speechClient, "ja");

        var result = await runner.ConfirmAsync(context, "it", CancellationToken.None);

        Assert.Equal(FormsCatalogLanguageChangeDecision.Confirmed, result);

        var request = Assert.Single(speechClient.SpeechToTextCalls);
        Assert.Equal("it", request.LanguageCode);
        Assert.NotNull(request.Keyterms);
        Assert.Contains("ja", request.Keyterms!.Terms, StringComparer.Ordinal);
        Assert.Contains("nein", request.Keyterms!.Terms, StringComparer.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_WhenLanguageTargetIsMissing_OffersSupportedLanguagesAndKeepsTargetStage()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var workflow = CreateLanguageChangeWorkflow();
        var handler = new FormsCatalogDialogLanguageChangeHandler(
            CreateSpeechTurnRunner(speechClient, new RecordingTextToSpeechPlayer()),
            CreateIntentParser(),
            workflow,
            new RecordingSpeechAssistantTrace());
        var context = CreateTurnContext(maxRetries: 1);
        var supportedLanguages = workflow.FormatSupportedLanguages(
            context.PromptContext.SupportedLanguages,
            context.IsoCode);
        var expectedPrompt = FormDialogFlowPromptTextResolver.GetMissingArgumentPrompt(
            context.IsoCode,
            supportedLanguages,
            context.PolitenessLevel);

        var result = await handler.HandleAsync(context, rawLanguageTarget: null, CancellationToken.None);

        Assert.Equal(SelectionDialogPhase.Listening, result.NextPhase);
        Assert.True(result.ExpectsLanguageTarget);
        Assert.Equal(SelectionDialogCandidateTransitionKind.Preservation, result.CandidateTransition.Kind);
        Assert.Equal(expectedPrompt, result.SpokenPrompt?.Text);
        Assert.Empty(speechClient.RecordModes);
    }

    [Fact]
    public async Task ConfirmAsync_WhenSilencePrecedesAnUnrecognizedAnswer_DoesNotConsumeRecognizedFailureBudget()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var runner = CreateConfirmationRunner(speechClient, textToSpeechPlayer);
        var context = CreateTurnContext(maxRetries: 2);

        await EnqueueSilenceAsync(speechClient);
        await EnqueueTranscriptAsync(speechClient, "vielleicht");
        await EnqueueTranscriptAsync(speechClient, "ja");

        var result = await runner.ConfirmAsync(context, "en", CancellationToken.None);

        Assert.Equal(FormsCatalogLanguageChangeDecision.Confirmed, result);
        Assert.Equal(3, speechClient.RecordCallCount);
        Assert.Equal(2, speechClient.SpeechToTextCallCount);
    }

    [Fact]
    public async Task ConfirmAsync_WhenSilenceSeparatesCaptureFailures_PreservesConsecutiveCaptureFailureBudget()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var trace = new RecordingSpeechAssistantTrace();
        var runner = CreateConfirmationRunner(speechClient, textToSpeechPlayer, trace);
        var context = CreateTurnContext(maxRetries: 2);

        await EnqueueCaptureFailureAsync(speechClient);
        await EnqueueSilenceAsync(speechClient);
        await EnqueueCaptureFailureAsync(speechClient);
        await EnqueueCaptureFailureAsync(speechClient);

        var result = await runner.ConfirmAsync(context, "en", CancellationToken.None);

        // Stille ist weder technischer Fehler noch brauchbare Antwort und setzt die Folge technischer Fehler nicht zurück.
        Assert.Equal(FormsCatalogLanguageChangeDecision.CaptureFaultBudgetExhausted, result);
        Assert.Equal(3, speechClient.RecordCallCount);
        Assert.Equal(0, speechClient.SpeechToTextCallCount);
        Assert.Equal(2, trace.Entries.Count);
    }

    [Fact]
    public async Task ConfirmAsync_WithHelpAndRepeat_PreservesConfirmationStageAndFailureBudget()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var workflow = CreateLanguageChangeWorkflow();
        var runner = CreateConfirmationRunner(speechClient, textToSpeechPlayer, workflow: workflow);
        var context = CreateTurnContext(maxRetries: 2);
        var expectedConfirmationPrompt = workflow.GetConfirmationPrompt("en", "English");

        await EnqueueTranscriptAsync(speechClient, "vielleicht");
        await EnqueueTranscriptAsync(speechClient, "hilfe");
        await EnqueueTranscriptAsync(speechClient, "wiederholen");
        await EnqueueTranscriptAsync(speechClient, "ja");

        var result = await runner.ConfirmAsync(context, "en", CancellationToken.None);

        // Hilfe und Wiederholen verbrauchen keinen weiteren Antwortversuch und erhalten das ausstehende Sprachziel.
        Assert.Equal(FormsCatalogLanguageChangeDecision.Confirmed, result);
        Assert.Equal(4, speechClient.RecordCallCount);
        Assert.Equal(4, speechClient.SpeechToTextCallCount);
        Assert.Equal(5, textToSpeechPlayer.Calls.Count);
        var spokenCalls = textToSpeechPlayer.Calls.ToArray();
        Assert.Equal(expectedConfirmationPrompt, spokenCalls[0].Text);
        Assert.Equal(expectedConfirmationPrompt, spokenCalls[1].Text);
        Assert.Equal(expectedConfirmationPrompt, spokenCalls[2].Text);
        Assert.Contains(
            FormsCatalogPromptBuilder.GetConfirmationRepeatPrompt(
                context.PromptContext,
                context.IsoCode,
                context.PolitenessLevel).Text,
            spokenCalls[3].Text,
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal(expectedConfirmationPrompt, spokenCalls[4].Text);
        Assert.All(
            speechClient.RecordModes,
            mode => Assert.Equal(SpeechCaptureMode.ShortConfirmation, mode));
    }

    [Fact]
    public async Task HandleAsync_WhenListIsRequestedDuringLanguageConfirmation_PreservesTargetUntilConfirmed()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var languageSelectionWriter = new Mock<IUserLanguageSelectionWriter>(MockBehavior.Strict);
        languageSelectionWriter
            .Setup(writer => writer.SaveSelectedLanguageAsync("en", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(new UserLanguageSettings("en", PolitenessLevel.Formal)));
        var workflow = CreateLanguageChangeWorkflow(languageSelectionWriter.Object);
        var handler = new FormsCatalogDialogLanguageChangeHandler(
            CreateSpeechTurnRunner(speechClient, textToSpeechPlayer),
            CreateIntentParser(),
            workflow,
            new RecordingSpeechAssistantTrace());
        var context = CreateTurnContext(maxRetries: 2);
        await EnqueueTranscriptAsync(speechClient, "alle anwendungen");
        await EnqueueTranscriptAsync(speechClient, "ja");

        var result = await handler.HandleAsync(context, "englisch", CancellationToken.None);

        var expectedConfirmationPrompt = workflow.GetConfirmationPrompt("en", "English");
        var spokenCalls = textToSpeechPlayer.Calls.ToArray();
        Assert.Equal(SelectionDialogPhase.Completed, result.NextPhase);
        Assert.Equal(SelectionDialogCompletionReason.RestartRequested, result.CompletionReason);
        Assert.Equal("en", result.RestartIsoCode);
        Assert.Equal(SelectionDialogCandidateTransitionKind.TerminalClearance, result.CandidateTransition.Kind);
        Assert.Null(result.SpokenPrompt);
        Assert.Equal(5, spokenCalls.Length);
        Assert.Equal(expectedConfirmationPrompt, spokenCalls[0].Text);
        Assert.Contains(spokenCalls, call => call.Text.Contains("Alpha", StringComparison.Ordinal));
        Assert.Contains(spokenCalls, call => call.Text.Contains("Beta", StringComparison.Ordinal));
        Assert.Equal(expectedConfirmationPrompt, spokenCalls[^1].Text);
        languageSelectionWriter.Verify(
            writer => writer.SaveSelectedLanguageAsync("en", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenMaxRetriesIsZero_FirstCaptureFailureIsTerminal()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var handler = new FormsCatalogDialogLanguageChangeHandler(
            CreateSpeechTurnRunner(speechClient, new RecordingTextToSpeechPlayer()),
            CreateIntentParser(),
            CreateLanguageChangeWorkflow(),
            new RecordingSpeechAssistantTrace());
        var context = CreateTurnContext(maxRetries: 0);
        await EnqueueCaptureFailureAsync(speechClient);

        var result = await handler.HandleAsync(context, "englisch", CancellationToken.None);

        Assert.Equal(SelectionDialogPhase.Completed, result.NextPhase);
        Assert.Equal(SelectionDialogCompletionReason.CaptureFaultBudgetExhausted, result.CompletionReason);
        Assert.Equal(1, speechClient.RecordCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenCaptureFailureBudgetIsExhausted_ReturnsModeledCaptureFailureAttribution()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var trace = new RecordingSpeechAssistantTrace();
        var workflow = CreateLanguageChangeWorkflow();
        var handler = new FormsCatalogDialogLanguageChangeHandler(
            CreateSpeechTurnRunner(speechClient, textToSpeechPlayer),
            CreateIntentParser(),
            workflow,
            trace);
        var context = CreateTurnContext(maxRetries: 2);

        await EnqueueCaptureFailureAsync(speechClient);
        await EnqueueCaptureFailureAsync(speechClient);
        await EnqueueCaptureFailureAsync(speechClient);

        var result = await handler.HandleAsync(context, "englisch", CancellationToken.None);

        Assert.Equal(SelectionDialogPhase.Completed, result.NextPhase);
        Assert.Equal(
            SelectionDialogCompletionReason.CaptureFaultBudgetExhausted,
            result.CompletionReason);
        Assert.Equal(
            SelectionDialogCandidateTransitionKind.TerminalClearance,
            result.CandidateTransition.Kind);
        Assert.Equal(CaptureFailedTerminalText, result.SpokenPrompt?.Text);
        Assert.Equal(2, speechClient.RecordCallCount);
        Assert.Equal(2, trace.Entries.Count);
    }

    [Theory]
    [InlineData("nein")]
    [InlineData("vielleicht")]
    public async Task HandleAsync_WithNegativeOrUnrecognizedConfirmation_DoesNotAttributeCaptureFailure(
        string transcript)
    {
        var speechClient = new ScriptedSpeechInputClient();
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var trace = new RecordingSpeechAssistantTrace();
        var handler = new FormsCatalogDialogLanguageChangeHandler(
            CreateSpeechTurnRunner(speechClient, textToSpeechPlayer),
            CreateIntentParser(),
            CreateLanguageChangeWorkflow(),
            trace);
        var context = CreateTurnContext(maxRetries: 1);

        await EnqueueTranscriptAsync(speechClient, transcript);

        var result = await handler.HandleAsync(context, "englisch", CancellationToken.None);

        Assert.Equal(SelectionDialogPhase.Listening, result.NextPhase);
        Assert.Null(result.CompletionReason);
        Assert.Equal(
            SelectionDialogCandidateTransitionKind.Preservation,
            result.CandidateTransition.Kind);
        Assert.NotNull(result.SpokenPrompt);
        Assert.DoesNotContain(
            CaptureFailedTerminalText,
            result.SpokenPrompt!.Text,
            StringComparison.Ordinal);
        Assert.Empty(trace.Entries);
    }

    internal static FormsCatalogLanguageChangeConfirmationRunner CreateConfirmationRunner(
        ScriptedSpeechInputClient speechClient,
        RecordingTextToSpeechPlayer textToSpeechPlayer,
        RecordingSpeechAssistantTrace? trace = null,
        FormsCatalogLanguageChangeWorkflow? workflow = null)
        => new(
            CreateSpeechTurnRunner(speechClient, textToSpeechPlayer),
            CreateIntentParser(),
            workflow ?? CreateLanguageChangeWorkflow(),
            trace ?? new RecordingSpeechAssistantTrace());

    private static SelectionDialogSpeechTurnRunner CreateSpeechTurnRunner(
        ScriptedSpeechInputClient speechClient,
        RecordingTextToSpeechPlayer textToSpeechPlayer)
        => new(
            new NoOpClientMessageBus(),
            speechClient,
            speechClient,
            new SpeechRecognitionWorker(speechClient),
            textToSpeechPlayer,
            NullLogger<SelectionDialogSpeechTurnRunner>.Instance);

    internal static FormsCatalogLanguageChangeWorkflow CreateLanguageChangeWorkflow(
        IUserLanguageSelectionWriter? languageSelectionWriter = null)
        => new(
            languageSelectionWriter ?? Mock.Of<IUserLanguageSelectionWriter>(),
            new LanguageChangeTargetResolver(VoiceInterfaceLanguageCatalog.CreateDefault()));

    private static FormsCatalogIntentParser CreateIntentParser()
        => new(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);

    private static FormsCatalogDialogTurnContext CreateTurnContext(int maxRetries)
    {
        var catalog = FormsCatalogIntentTestCatalog.Create();
        catalog.SelectionDialog.Prompts =
        [
            new CatalogVoicePrompt
            {
                PromptId = CatalogDialogPromptIds.AppCaptureFailedTerminal,
                Text = new LocalizedText { ["de"] = CaptureFailedTerminalText }
            },
            new CatalogVoicePrompt
            {
                PromptId = CatalogDialogPromptIds.AppConfirmationRepeat,
                Text = new LocalizedText { ["de"] = "Bitte sagen Sie ja oder nein." }
            },
            new CatalogVoicePrompt
            {
                PromptId = CatalogDialogPromptIds.AppListIntro,
                Text = new LocalizedText { ["de"] = "Verfügbare Anwendungen: {{count}}." }
            },
            new CatalogVoicePrompt
            {
                PromptId = CatalogDialogPromptIds.AppListItem,
                Text = new LocalizedText { ["de"] = "Nummer {{number}}: {{name}}." }
            },
            new CatalogVoicePrompt
            {
                PromptId = CatalogDialogPromptIds.AppListSelectPrompt,
                Text = new LocalizedText { ["de"] = "Sagen Sie die Nummer oder den Namen." }
            }
        ];

        return CreateTurnContext(catalog, maxRetries, "de", PolitenessLevel.Formal);
    }

    internal static FormsCatalogDialogTurnContext CreateTurnContext(
        FormsCatalog catalog,
        int maxRetries,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var applications = new List<ApplicationInfo>
        {
            new("alpha", "Alpha", "Alpha description", ["target"]),
            new("beta", "Beta", "Beta description", ["other"])
        };
        var matcher = new ApplicationMatcher().CandidateMatcher;
        var candidates = matcher.CreateSnapshot(applications, SelectionRevision.Initial);
        var promptContext = CatalogPromptContext.CreateForCatalog(catalog);

        return new FormsCatalogDialogTurnContext(
            CreateIntentHandler(matcher),
            catalog,
            "language-change-confirmation-test",
            new FormsCatalogDialogTurnSelection(
                new SelectionDialogTurnSnapshot<ApplicationInfo>(
                    candidates,
                    candidates,
                    activeAmbiguity: null,
                    selectedCandidate: null,
                    selectedResolutionKind: null),
                lastPrompt: null),
            new FormsCatalogDialogPromptRuntime(
                promptContext,
                new SelectionDialogInteractionSettings(1.0d, 0, AllowBargeIn: false, 85, 65),
                politenessLevel,
                isoCode,
                noInputTimeoutMs: 500,
                maxRetries));
    }

    private static SelectionDialogCommonIntentHandler<ApplicationInfo> CreateIntentHandler(
        SelectionCandidateMatcher<ApplicationInfo> matcher)
        => new(
            matcher,
            buildListPrompt: _ => SelectionDialogPrompt.FromText("list"),
            buildAmbiguityPrompt: _ => SelectionDialogPrompt.FromText("ambiguous"),
            buildCancelPrompt: () => SelectionDialogPrompt.FromText("cancel"),
            buildHelpPrompt: () => SelectionDialogPrompt.FromText("help"),
            buildInvalidNumberPrompt: _ => SelectionDialogPrompt.FromText("invalid"),
            buildNoMatchPrompt: _ => SelectionDialogPrompt.FromText("no match"),
            transitionAfterSelectionAsync: (selected, resolutionKind, _) => Task.FromResult(
                new SelectionDialogIntentStep<ApplicationInfo>(
                    SelectionDialogPhase.Launch,
                    SelectionDialogCandidateTransition<ApplicationInfo>.SelectUnique(selected, resolutionKind),
                    spokenPrompt: null)));

    internal static ValueTask EnqueueTranscriptAsync(
        ScriptedSpeechInputClient speechClient,
        string transcript)
    {
        speechClient.EnqueueTranscript(transcript, confidencePercent: 95);
        return speechClient.EnqueueRecordStepAsync(
            _ => Task.FromResult(new CapturedSpeechAudio(
                SpeechAudioMimeType.Pcm,
                [Array.Empty<byte>()])));
    }

    private static ValueTask EnqueueSilenceAsync(ScriptedSpeechInputClient speechClient)
        => speechClient.EnqueueRecordStepAsync(
            _ => Task.FromException<CapturedSpeechAudio>(new NoSpeechDetectedException()));

    private static ValueTask EnqueueCaptureFailureAsync(ScriptedSpeechInputClient speechClient)
        => speechClient.EnqueueRecordStepAsync(
            _ => Task.FromException<CapturedSpeechAudio>(
                new OperationCanceledException("Synthetic capture interruption.")));
}
