using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Speech.Runtime.Tests.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft getrennte Abläufe für Stille vor Sprachbeginn, leere Erkennungsergebnisse und
/// unpassenden Text. Stille erhält einen Hinweis; ein erstes leeres Ergebnis führt
/// zur erneuten Aufnahme, nichtleerer Text zur Prüfung seiner Handlungsabsicht.
/// </summary>
public sealed class SelectionNoInputRouteOwnershipTests
{
    private static readonly SelectionDialogInteractionSettings DefaultInteractionSettings = new(1.0d, 0, false, 85, 65);

    [Fact]
    public async Task ExecuteAsync_WhenTranscriptIsWhitespace_SilentlyReArms_WithoutPromptOrIntentHandling()
    {
        // Ein reines Leerraumergebnis trägt RecognizedAsEmpty und führt ohne neue Ansage
        // oder Prüfung einer Handlungsabsicht zur nächsten Aufnahme.
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);
        await using var worker = new SpeechRecognitionWorker(
            recognitionClient.Object,
            backoff: new RecordingBackoffPolicy(maxRetries: 1),
            maxQueueDepth: 1);

        captureClient
            .Setup(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance);
        recognitionClient
            .SetupSequence(client => client.SpeechToTextAsync(utterance, "de", It.IsAny<RecognitionProfile>(), It.IsAny<CancellationToken>(), It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new SpeechTranscript("   ", 98))
            .ReturnsAsync(new SpeechTranscript("eins", 98));

        var intentHandlerInvocations = 0;
        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, worker, textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) =>
                {
                    intentHandlerInvocations++;
                    return Task.FromResult(
                        CreateSelectionStep(SelectionDialogPhase.Launch, snapshot, transcript, spokenPrompt: null));
                },
                projectSelectedResult: (selected, _) => $"selected:{selected.Item}"));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("selected:eins", result);
        Assert.Equal(1, intentHandlerInvocations);
        Assert.Equal(
            ["announce"],
            textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray());
    }

    [Fact]
    public async Task ExecuteAsync_WhenCaptureIsGenuineSilence_RoutesToNoInput_NeverToIntentHandling()
    {
        // Stille vor einem erfassten Sprachsegment verlangt den Hinweis auf fehlende Eingabe und keine Prüfung einer Handlungsabsicht.
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);
        await using var worker = new SpeechRecognitionWorker(
            recognitionClient.Object,
            backoff: new RecordingBackoffPolicy(maxRetries: 1),
            maxQueueDepth: 1);

        captureClient
            .SetupSequence(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NoSpeechDetectedException())
            .ReturnsAsync(utterance);
        recognitionClient
            .Setup(client => client.SpeechToTextAsync(utterance, "de", It.IsAny<RecognitionProfile>(), It.IsAny<CancellationToken>(), It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new SpeechTranscript("eins", 98));

        var intentHandlerInvocations = 0;
        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, worker, textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) =>
                {
                    intentHandlerInvocations++;
                    return Task.FromResult(
                        CreateSelectionStep(SelectionDialogPhase.Launch, snapshot, transcript, spokenPrompt: null));
                },
                projectSelectedResult: (selected, _) => $"selected:{selected.Item}"));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("selected:eins", result);
        Assert.Equal(1, intentHandlerInvocations);
        Assert.Equal(
            ["announce", "no-input"],
            textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray());
    }

    [Fact]
    public async Task ExecuteAsync_WhenTranscriptIsNonBlankUnmatched_DoesNotFallThroughToNoInput()
    {
        // Nichtleerer, unbekannter Text erhält seine eigene Nichtverstehensrückmeldung, keine Meldung fehlender Eingabe.
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);
        await using var worker = new SpeechRecognitionWorker(
            recognitionClient.Object,
            backoff: new RecordingBackoffPolicy(maxRetries: 1),
            maxQueueDepth: 1);

        captureClient
            .Setup(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance);
        recognitionClient
            .SetupSequence(client => client.SpeechToTextAsync(utterance, "de", It.IsAny<RecognitionProfile>(), It.IsAny<CancellationToken>(), It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new SpeechTranscript("quark", 98))
            .ReturnsAsync(new SpeechTranscript("eins", 98));

        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, worker, textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) => Task.FromResult(
                    string.Equals(transcript, "quark", StringComparison.OrdinalIgnoreCase)
                        ? new SelectionDialogIntentStep<string>(
                            SelectionDialogPhase.Listening,
                            SelectionDialogCandidateTransition<string>.RetainAmbiguity(
                                snapshot.VisibleCandidates),
                            SelectionDialogPrompt.FromText("unrecognized"))
                        : CreateSelectionStep(SelectionDialogPhase.Launch, snapshot, transcript, spokenPrompt: null)),
                projectSelectedResult: (selected, _) => $"selected:{selected.Item}"));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("selected:eins", result);
        var spoken = textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray();
        Assert.Equal(["announce", "unrecognized"], spoken);
        Assert.DoesNotContain("no-input", spoken);
    }

    private static SelectionDialogSpeechTurnRunner CreateSpeechTurnRunner(
        ISpeechCaptureClient captureClient,
        SpeechRecognitionWorker speechRecognitionWorker,
        ITextToSpeechPlayer textToSpeechPlayer)
    {
        return new SelectionDialogSpeechTurnRunner(
            new SynchronousClientMessageBus(),
            captureClient,
            ModeAwareSpeechCapture.Required(captureClient),
            speechRecognitionWorker,
            textToSpeechPlayer,
            NullLogger<SelectionDialogSpeechTurnRunner>.Instance);
    }

    private static SelectionDialogExecutionDefinition<string, string> CreateDefinition(
        SelectionDialogSpeechTurnRunner speech,
        string isoCode,
        IReadOnlyList<string> items,
        Func<string, SelectionDialogTurnSnapshot<string>, SelectionDialogPrompt?, CancellationToken, Task<SelectionDialogIntentStep<string>>> onListeningInputAsync,
        Func<SelectionCandidate<string>, SelectionDialogTurnSnapshot<string>, string> projectSelectedResult)
    {
        var candidateMatcher = CreateCandidateMatcher();
        var initialCandidates = candidateMatcher.CreateSnapshot(items, SelectionRevision.Initial);
        var confirmationPolicy = new SelectionConfirmationTransitionPolicy<string>(
            candidateMatcher,
            DefaultInteractionSettings.SttMinConfidenceToAccept,
            DefaultInteractionSettings.SttMinConfidenceToConfirm);

        return new SelectionDialogExecutionDefinition<string, string>(
            new SelectionDialogExecutionSettings<string>(
                speech,
                isoCode,
                initialCandidates,
                DefaultInteractionSettings,
                maxRetries: 2,
                noInputTimeoutMs: 500,
                initialListeningTranscript: null,
                trace: new SpeechAssistantTraceContext(
                    null,
                    null,
                    SpeechAssistantTraceScope.SelectionDialogRuntime)),
            new SelectionDialogPromptProviders<string>(
                getEmptyItemsPromptAsync: (_, _) => Task.FromResult(SelectionDialogPrompt.FromText("none")),
                getAnnouncingPromptAsync: (_, _) => Task.FromResult(SelectionDialogPrompt.FromText("announce")),
                getNoInputPromptAsync: (_, _) => Task.FromResult(SelectionDialogPrompt.FromText("no-input")),
                getReListenPromptAsync: (_, _) => Task.FromResult(SelectionDialogPrompt.FromText("re-listen")),
                getCaptureFailedTerminalPromptAsync: (_, _) => Task.FromResult(SelectionDialogPrompt.FromText("capture-failed")),
                getRecoverableFailurePromptAsync: (_, _) => Task.FromResult(SelectionDialogPrompt.FromText("recoverable-failure")),
                getConfirmationPromptAsync: (_, _) => Task.FromResult(SelectionDialogPrompt.FromText("confirmation"))),
            new SelectionDialogOutcomeHandlers<string, string>(
                onListeningInputAsync: onListeningInputAsync,
                onConfirmInputAsync: (_, _) => Task.FromResult(
                    new SelectionDialogCommonIntentParseResult(SelectionDialogCommonIntent.Help)),
                selectedLaunch: new SelectionDialogLaunchDefinition<string, string>(
                    buildPrompt: (_, _) => SelectionDialogPrompt.FromText(null),
                    projectResult: projectSelectedResult),
                createCancelledResult: (_, _) => "cancelled"),
            confirmationPolicy);
    }

    private static SelectionDialogIntentStep<string> CreateSelectionStep(
        SelectionDialogPhase nextPhase,
        SelectionDialogTurnSnapshot<string> snapshot,
        string selectedItem,
        SelectionDialogPrompt? spokenPrompt)
    {
        var candidate = Assert.Single(
            snapshot.SelectableCandidates.Candidates,
            candidate => string.Equals(candidate.Item, selectedItem, StringComparison.Ordinal));
        Assert.Equal(snapshot.Revision, candidate.Revision);

        return new SelectionDialogIntentStep<string>(
            nextPhase,
            SelectionDialogCandidateTransition<string>.SelectUnique(
                candidate,
                SelectionCandidateResolutionKind.ExactName),
            spokenPrompt);
    }

    private static SelectionCandidateMatcher<string> CreateCandidateMatcher()
    {
        return new SelectionCandidateMatcher<string>(
            static item => item,
            static item => item,
            CompactForTest);
    }

    private static string CompactForTest(string value)
    {
        return string.Concat(value.Where(static character => !char.IsWhiteSpace(character)))
            .ToLowerInvariant();
    }
}
