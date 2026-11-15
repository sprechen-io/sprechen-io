using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Speech.Runtime.Tests.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class SelectionDialogRuntimeTests
{
    private static readonly SelectionDialogInteractionSettings DefaultInteractionSettings = new(1.0d, 0, false, 85, 65);

    [Fact]
    public void Constructor_PreservesTypedTranscriptTextAndConfidence()
    {
        var settings = new SelectionDialogExecutionSettings<string>(
            CreateSpeechTurnRunner(
                Mock.Of<ISpeechCaptureClient>(),
                Mock.Of<ISpeechRecognitionClient>(),
                new RecordingTextToSpeechPlayer()),
            "de",
            CreateInitialCandidateSnapshot(["Alpha"]),
            DefaultInteractionSettings,
            maxRetries: 2,
            noInputTimeoutMs: 500,
            initialListeningTranscript: new SpeechTranscript(" Alpha ", ConfidencePercent: 93));

        Assert.Equal("Alpha", settings.InitialListeningTranscript);
        Assert.Equal(93, settings.InitialListeningSpeechTranscript?.ConfidencePercent);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoItems_SpeaksEmptyItemsPromptAndReturnsCancelled()
    {
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        SelectionDialogTurnSnapshot<string>? emptyItemsTurn = null;
        var definition = CreateDefinition(
            CreateSpeechTurnRunner(captureClient.Object, recognitionClient.Object, textToSpeechPlayer),
            "de",
            [],
            projectSelectedResult: (_, _) => "selected",
            onEmptyItemsPromptSnapshot: snapshot => emptyItemsTurn = snapshot);
        var runtime = new SelectionDialogRuntime<string, string>(definition);

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("cancelled", result);
        Assert.NotNull(emptyItemsTurn);
        Assert.Same(definition.Settings.InitialCandidates, emptyItemsTurn.InitialCandidates);
        Assert.Same(definition.Settings.InitialCandidates, emptyItemsTurn.VisibleCandidates);
        Assert.Equal(SelectionRevision.Initial, emptyItemsTurn.Revision);
        Assert.Collection(
            textToSpeechPlayer.Calls,
            call =>
            {
                Assert.Equal("none", call.Text);
                Assert.Equal("de", call.LanguageCode);
            });
        captureClient.VerifyNoOtherCalls();
        recognitionClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_WhenSingleItemHandlerExists_UsesSingleItemPath()
    {
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var trace = new RecordingSpeechAssistantTrace();
        SelectionCandidate<string>? singleCandidate = null;
        SelectionDialogTurnSnapshot<string>? singleTurn = null;
        var definition = CreateDefinition(
            CreateSpeechTurnRunner(captureClient.Object, recognitionClient.Object, textToSpeechPlayer),
            "en",
            ["item-1"],
            projectSelectedResult: (_, _) => "selected",
            projectSingleItemResult: (candidate, snapshot) =>
            {
                singleCandidate = candidate;
                singleTurn = snapshot;
                return $"single:{candidate.Item}";
            },
            speechAssistantTrace: trace,
            correlationId: "single-launch",
            buildSingleItemPrompt: (candidate, _) => SelectionDialogPrompt.FromText(
                $"single-launch:{candidate.Item}"));
        var runtime = new SelectionDialogRuntime<string, string>(definition);

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("single:item-1", result);
        Assert.NotNull(singleTurn);
        Assert.Same(definition.Settings.InitialCandidates, singleTurn.InitialCandidates);
        Assert.Same(definition.Settings.InitialCandidates, singleTurn.VisibleCandidates);
        Assert.Same(definition.Settings.InitialCandidates[0], singleCandidate);
        Assert.Equal(SelectionRevision.Initial, Assert.IsType<SelectionCandidate<string>>(singleCandidate).Revision);
        var launchCall = Assert.Single(textToSpeechPlayer.Calls);
        Assert.Equal("single-launch:item-1", launchCall.Text);
        Assert.Equal(
            [
                "catalog-selection event=candidates_matched candidateIds=[item-1]",
                "catalog-selection event=candidate_chosen candidateId=item-1",
                "catalog-selection event=prompt_scheduled promptId=selection.launch candidateId=item-1 promptSha256=" +
                ComputePromptSha256(launchCall.Text),
                "catalog-selection event=selection_launch candidateId=item-1"
            ],
            trace.Entries
                .Where(static entry => entry.Message.StartsWith("catalog-selection event=", StringComparison.Ordinal))
                .Select(static entry => entry.Message)
                .ToArray());
        captureClient.VerifyNoOtherCalls();
        recognitionClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_WhenListeningLaunchesSelection_ProjectsSelectedResultAfterLaunchPrompt()
    {
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);

        captureClient
            .Setup(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance);
        recognitionClient
            .Setup(client => client.SpeechToTextAsync(utterance, "de", It.IsAny<RecognitionProfile>(), It.IsAny<CancellationToken>(), It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new SpeechTranscript("eins", 98));

        SelectionDialogTurnSnapshot<string>? announcingTurn = null;
        SelectionDialogTurnSnapshot<string>? listeningTurn = null;
        SelectionDialogTurnSnapshot<string>? selectedTurn = null;
        var definition = CreateDefinition(
            CreateSpeechTurnRunner(captureClient.Object, recognitionClient.Object, textToSpeechPlayer),
            "de",
            ["eins", "zwei"],
            onListeningInputAsync: (transcript, snapshot, _, _) =>
            {
                listeningTurn = snapshot;
                return Task.FromResult(
                    CreateSelectionStep(SelectionDialogPhase.Launch, snapshot, transcript, spokenPrompt: null));
            },
            projectSelectedResult: (selected, snapshot) =>
            {
                selectedTurn = snapshot;
                return $"selected:{selected.Item}";
            },
            onAnnouncingPromptSnapshot: snapshot => announcingTurn = snapshot,
            buildSelectedPrompt: (selected, _) => SelectionDialogPrompt.FromText(
                $"launch:{selected.Item}"));
        var runtime = new SelectionDialogRuntime<string, string>(definition);

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("selected:eins", result);
        Assert.NotNull(announcingTurn);
        Assert.NotNull(listeningTurn);
        Assert.NotNull(selectedTurn);
        Assert.Same(definition.Settings.InitialCandidates, announcingTurn.VisibleCandidates);
        Assert.Same(announcingTurn.VisibleCandidates, listeningTurn.VisibleCandidates);
        Assert.Same(listeningTurn.VisibleCandidates, selectedTurn.VisibleCandidates);
        Assert.Same(definition.Settings.InitialCandidates[0], selectedTurn.SelectedCandidate);
        Assert.Equal(SelectionRevision.Initial, selectedTurn.Revision);
        Assert.Collection(
            textToSpeechPlayer.Calls,
            call =>
            {
                Assert.Equal("announce", call.Text);
                Assert.Equal("de", call.LanguageCode);
            },
            call =>
            {
                Assert.Equal("launch:eins", call.Text);
                Assert.Equal("de", call.LanguageCode);
            });
    }

    [Fact]
    public async Task ExecuteAsync_WhenLaunchPlaybackStarts_PromptEvidenceAlreadyExists()
    {
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var trace = new RecordingSpeechAssistantTrace();
        var textToSpeechPlayer = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeechPlayer
            .Setup(player => player.SpeakAsync(
                "launch:eins",
                "de",
                DefaultInteractionSettings.SpeechRate,
                SpokenAnnouncementKind.Prompt,
                It.IsAny<CancellationToken>()))
            .Callback(() => Assert.Contains(
                trace.Entries,
                entry => entry.Message ==
                    "catalog-selection event=prompt_scheduled promptId=selection.launch candidateId=eins promptSha256=" +
                    ComputePromptSha256("launch:eins")))
            .Returns(Task.CompletedTask);

        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, recognitionClient.Object, textToSpeechPlayer.Object),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) => Task.FromResult(
                    CreateSelectionStep(SelectionDialogPhase.Launch, snapshot, transcript, spokenPrompt: null)),
                projectSelectedResult: (selected, _) => $"selected:{selected.Item}",
                initialListeningTranscript: new SpeechTranscript("eins", ConfidencePercent: 98),
                speechAssistantTrace: trace,
                correlationId: "launch-order",
                buildSelectedPrompt: (selected, _) => SelectionDialogPrompt.FromText(
                    $"launch:{selected.Item}")));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("selected:eins", result);
        Assert.Equal(
            "catalog-selection event=selection_launch candidateId=eins",
            trace.Entries[^1].Message);
        textToSpeechPlayer.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenLaunchPlaybackFails_DoesNotProjectOrTraceLaunch()
    {
        var trace = new RecordingSpeechAssistantTrace();
        var textToSpeechPlayer = new ThrowingTextToSpeechPlayer();
        var resultProjectionCalled = false;
        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(
                    Mock.Of<ISpeechCaptureClient>(),
                    Mock.Of<ISpeechRecognitionClient>(),
                    textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) => Task.FromResult(
                    CreateSelectionStep(SelectionDialogPhase.Launch, snapshot, transcript, spokenPrompt: null)),
                projectSelectedResult: (selected, _) =>
                {
                    resultProjectionCalled = true;
                    return $"selected:{selected.Item}";
                },
                initialListeningTranscript: new SpeechTranscript("eins", ConfidencePercent: 98),
                speechAssistantTrace: trace,
                correlationId: "launch-tts-failure",
                buildSelectedPrompt: (_, _) => SelectionDialogPrompt.FromText("launch")));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => runtime.ExecuteAsync(CancellationToken.None));

        Assert.Equal(1, textToSpeechPlayer.AttemptCount);
        Assert.False(resultProjectionCalled);
        Assert.Contains(
            trace.Entries,
            entry => entry.Message.Contains("promptId=selection.launch", StringComparison.Ordinal));
        Assert.DoesNotContain(
            trace.Entries,
            entry => entry.Message.Contains("event=selection_launch", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WhenLaunchResultProjectionFails_DoesNotTraceLaunch()
    {
        var trace = new RecordingSpeechAssistantTrace();
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(
                    Mock.Of<ISpeechCaptureClient>(),
                    Mock.Of<ISpeechRecognitionClient>(),
                    textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) => Task.FromResult(
                    CreateSelectionStep(SelectionDialogPhase.Launch, snapshot, transcript, spokenPrompt: null)),
                projectSelectedResult: (_, _) => throw new InvalidOperationException("Synthetic projection failure."),
                initialListeningTranscript: new SpeechTranscript("eins", ConfidencePercent: 98),
                speechAssistantTrace: trace,
                correlationId: "launch-result-failure",
                buildSelectedPrompt: (_, _) => SelectionDialogPrompt.FromText("launch")));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => runtime.ExecuteAsync(CancellationToken.None));

        Assert.Equal("launch", Assert.Single(textToSpeechPlayer.Calls).Text);
        Assert.DoesNotContain(
            trace.Entries,
            entry => entry.Message.Contains("event=selection_launch", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WhenLaunchStepCarriesSpokenPrompt_RejectsBeforePlaybackAndProjection()
    {
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var resultProjectionCalled = false;
        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(
                    Mock.Of<ISpeechCaptureClient>(),
                    Mock.Of<ISpeechRecognitionClient>(),
                    textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) => Task.FromResult(
                    CreateSelectionStep(
                        SelectionDialogPhase.Launch,
                        snapshot,
                        transcript,
                        SelectionDialogPrompt.FromText("duplicate-launch"))),
                projectSelectedResult: (_, _) =>
                {
                    resultProjectionCalled = true;
                    return "selected";
                },
                initialListeningTranscript: new SpeechTranscript("eins", ConfidencePercent: 98),
                buildSelectedPrompt: (_, _) => SelectionDialogPrompt.FromText("canonical-launch")));

        await Assert.ThrowsAsync<SelectionCandidateStateException>(
            () => runtime.ExecuteAsync(CancellationToken.None));

        Assert.Empty(textToSpeechPlayer.Calls);
        Assert.False(resultProjectionCalled);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCallerCancelsDuringLaunchPlayback_DoesNotProjectOrTraceLaunch()
    {
        using var cancellation = new CancellationTokenSource();
        var trace = new RecordingSpeechAssistantTrace();
        var resultProjectionCalled = false;
        var textToSpeechPlayer = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeechPlayer
            .Setup(player => player.SpeakAsync(
                "launch",
                "de",
                DefaultInteractionSettings.SpeechRate,
                SpokenAnnouncementKind.Prompt,
                cancellation.Token))
            .Callback(cancellation.Cancel)
            .Returns(Task.CompletedTask);
        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(
                    Mock.Of<ISpeechCaptureClient>(),
                    Mock.Of<ISpeechRecognitionClient>(),
                    textToSpeechPlayer.Object),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) => Task.FromResult(
                    CreateSelectionStep(SelectionDialogPhase.Launch, snapshot, transcript, spokenPrompt: null)),
                projectSelectedResult: (_, _) =>
                {
                    resultProjectionCalled = true;
                    return "selected";
                },
                initialListeningTranscript: new SpeechTranscript("eins", ConfidencePercent: 98),
                speechAssistantTrace: trace,
                correlationId: "launch-cancelled",
                buildSelectedPrompt: (_, _) => SelectionDialogPrompt.FromText("launch")));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => runtime.ExecuteAsync(cancellation.Token));

        Assert.False(resultProjectionCalled);
        Assert.DoesNotContain(
            trace.Entries,
            entry => entry.Message.Contains("event=selection_launch", StringComparison.Ordinal));
        textToSpeechPlayer.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenInitialListeningTranscriptLaunchesSelection_SkipsAnnouncementAndCapture()
    {
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, recognitionClient.Object, textToSpeechPlayer),
                "de",
                ["prefetched", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) => Task.FromResult(
                    CreateSelectionStep(SelectionDialogPhase.Launch, snapshot, transcript, spokenPrompt: null)),
                projectSelectedResult: (selected, _) => $"selected:{selected.Item}",
                initialListeningTranscript: new SpeechTranscript("prefetched", ConfidencePercent: 98)));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("selected:prefetched", result);
        Assert.Empty(textToSpeechPlayer.Calls);
        captureClient.VerifyNoOtherCalls();
        recognitionClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_WhenInitialListeningTranscriptUnhandled_FallsBackToAnnouncementAndMicrophoneListening()
    {
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);

        captureClient
            .Setup(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance);
        recognitionClient
            .Setup(client => client.SpeechToTextAsync(utterance, "de", It.IsAny<RecognitionProfile>(), It.IsAny<CancellationToken>(), It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new SpeechTranscript("eins", 98));

        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, recognitionClient.Object, textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) =>
                {
                    if (string.Equals(transcript, "repeat", StringComparison.Ordinal))
                    {
                        return Task.FromResult(
                            new SelectionDialogIntentStep<string>(
                                SelectionDialogPhase.Listening,
                                SelectionDialogCandidateTransition<string>.Preserve(),
                                spokenPrompt: null));
                    }

                    return Task.FromResult(
                        CreateSelectionStep(SelectionDialogPhase.Launch, snapshot, transcript, spokenPrompt: null));
                },
                projectSelectedResult: (selected, _) => $"selected:{selected.Item}",
                initialListeningTranscript: new SpeechTranscript("repeat", ConfidencePercent: 98)));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("selected:eins", result);
        Assert.Collection(
            textToSpeechPlayer.Calls,
            call =>
            {
                Assert.Equal("announce", call.Text);
                Assert.Equal("de", call.LanguageCode);
            });
    }

    [Fact]
    public async Task ExecuteAsync_WhenSpeechTurnReturnsRecoverableFailure_SpeaksFailureCueWithoutConsumingNoInputBudget()
    {
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var trace = new RecordingSpeechAssistantTrace();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);
        await using var worker = new SpeechRecognitionWorker(
            recognitionClient.Object,
            backoff: new RecordingBackoffPolicy(maxRetries: 1),
            maxQueueDepth: 1);

        captureClient
            .SetupSequence(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance)
            .ReturnsAsync(utterance);
        recognitionClient
            .SetupSequence(client => client.SpeechToTextAsync(utterance, "de", It.IsAny<RecognitionProfile>(), It.IsAny<CancellationToken>(), It.IsAny<SpeechKeyterms?>()))
            .ThrowsAsync(new InvalidOperationException("synthetic failure"))
            .ReturnsAsync(new SpeechTranscript("eins", 98));

        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, worker, textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) => Task.FromResult(
                    CreateSelectionStep(SelectionDialogPhase.Launch, snapshot, transcript, spokenPrompt: null)),
                projectSelectedResult: (selected, _) => $"selected:{selected.Item}",
                speechAssistantTrace: trace,
                correlationId: "selection-test"));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("selected:eins", result);
        Assert.Equal(
            ["announce", "recoverable-failure"],
            textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray());
        Assert.Contains(
            trace.Entries,
            entry =>
                entry.Level == SpeechAssistantTraceLevel.Warning
                && entry.Message.Contains(SpeechRecognitionBackoffExhaustedException.CodeValue, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WhenRecoverableFailureOccurs_SpeaksCooperativeRetryCopyAndRepromptsWithoutDeadEnd()
    {
        // Verwendet die echte Katalogrückmeldung für einen behebbaren Erfassungsfehler.
        // Der erste Fehler soll eine erneute Nachfrage ermöglichen, statt die Auswahl zu beenden.
        var cooperativeCopy = RecoverableCapturePromptTextResolver.GetRecoverableFailurePrompt("de");
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
            .ReturnsAsync(utterance)
            .ReturnsAsync(utterance);
        recognitionClient
            .SetupSequence(client => client.SpeechToTextAsync(utterance, "de", It.IsAny<RecognitionProfile>(), It.IsAny<CancellationToken>(), It.IsAny<SpeechKeyterms?>()))
            .ThrowsAsync(new InvalidOperationException("synthetic failure"))
            .ReturnsAsync(new SpeechTranscript("eins", 98));

        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, worker, textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) => Task.FromResult(
                    CreateSelectionStep(SelectionDialogPhase.Launch, snapshot, transcript, spokenPrompt: null)),
                projectSelectedResult: (selected, _) => $"selected:{selected.Item}",
                recoverableFailurePromptText: cooperativeCopy));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("selected:eins", result);
        Assert.Equal(
            ["announce", cooperativeCopy],
            textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray());
        foreach (var alarming in new[] { "technisch", "technical", "Problem", "Fehler", "error" })
        {
            Assert.DoesNotContain(alarming, cooperativeCopy, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WhenRecoverableFailureThenSilencePersists_KeepsRepromptingNeverTimesOut()
    {
        // Technische Fehler und Stille haben getrennte Regeln. Nach einem Fehler und
        // zwei stillen Versuchen muss eine gültige Antwort noch zur Auswahl führen.
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
            .ReturnsAsync(utterance)
            .ThrowsAsync(new NoSpeechDetectedException())
            .ThrowsAsync(new NoSpeechDetectedException())
            .ReturnsAsync(utterance);
        recognitionClient
            .SetupSequence(client => client.SpeechToTextAsync(utterance, "de", It.IsAny<RecognitionProfile>(), It.IsAny<CancellationToken>(), It.IsAny<SpeechKeyterms?>()))
            .ThrowsAsync(new InvalidOperationException("synthetic failure"))
            .ReturnsAsync(new SpeechTranscript("eins", 98));

        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, worker, textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) => Task.FromResult(
                    CreateSelectionStep(SelectionDialogPhase.Launch, snapshot, transcript, spokenPrompt: null)),
                projectSelectedResult: (selected, _) => $"selected:{selected.Item}"));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("selected:eins", result);
        Assert.Equal(
            ["announce", "recoverable-failure", "no-input", "no-input"],
            textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray());
        Assert.DoesNotContain("capture-failed", textToSpeechPlayer.Calls.Select(static call => call.Text));
    }

    [Fact]
    public async Task ExecuteAsync_WhenRecognizedPromptlessTurnKeepsListening_SpeaksDedicatedReListenPrompt()
    {
        // Ein erkanntes Kommando ohne eigene Rückmeldung ist kein Fall fehlender Eingabe.
        // Die erneute Aufnahme benötigt deshalb den eigenen Wartehinweis statt der Stilleansage.
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
            .ReturnsAsync(new SpeechTranscript("weiter", 98))
            .ReturnsAsync(new SpeechTranscript("eins", 98));

        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, worker, textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) => Task.FromResult(
                    string.Equals(transcript, "weiter", StringComparison.OrdinalIgnoreCase)
                        ? new SelectionDialogIntentStep<string>(
                            SelectionDialogPhase.Listening,
                            SelectionDialogCandidateTransition<string>.RetainAmbiguity(
                                snapshot.VisibleCandidates),
                            spokenPrompt: null)
                        : CreateSelectionStep(SelectionDialogPhase.Launch, snapshot, transcript, spokenPrompt: null)),
                projectSelectedResult: (selected, _) => $"selected:{selected.Item}"));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("selected:eins", result);
        var spoken = textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray();
        Assert.Equal("announce", spoken[0]);
        Assert.Equal("re-listen", spoken[1]);
        Assert.DoesNotContain("no-input", spoken);
    }

    [Fact]
    public async Task ExecuteAsync_WhenListeningRecoverableFailuresPersist_TerminatesWithCaptureFaultReason()
    {
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var trace = new RecordingSpeechAssistantTrace();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);
        await using var worker = new SpeechRecognitionWorker(
            recognitionClient.Object,
            backoff: new RecordingBackoffPolicy(maxRetries: 1),
            maxQueueDepth: 1);
        SelectionDialogTurnSnapshot<string>? terminalSnapshot = null;

        captureClient
            .Setup(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance);
        recognitionClient
            .SetupSequence(client => client.SpeechToTextAsync(utterance, "de", It.IsAny<RecognitionProfile>(), It.IsAny<CancellationToken>(), It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new SpeechTranscript("ambiguous", 98))
            .ThrowsAsync(new InvalidOperationException("synthetic failure"))
            .ThrowsAsync(new InvalidOperationException("synthetic failure"));

        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, worker, textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (_, snapshot, _, _) => Task.FromResult(
                    new SelectionDialogIntentStep<string>(
                        SelectionDialogPhase.Listening,
                        SelectionDialogCandidateTransition<string>.RetainAmbiguity(
                            snapshot.VisibleCandidates),
                        SelectionDialogPrompt.FromText("ambiguous"))),
                createCancelledResult: (reason, snapshot) =>
                {
                    terminalSnapshot = snapshot;
                    return $"cancelled:{reason}";
                },
                speechAssistantTrace: trace,
                correlationId: "capture-terminal"));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        // MaxRetries 2 begrenzt hier technische Fehler: Nach dem zweiten folgt der Fehlerabschluss.
        // Stille eröffnet ein neues Aufnahmefenster und verbraucht dieses Budget nicht.
        Assert.Equal("cancelled:CaptureFaultBudgetExhausted", result);
        Assert.Equal(
            ["announce", "ambiguous", "recoverable-failure", "capture-failed"],
            textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray());
        Assert.NotNull(terminalSnapshot);
        Assert.Null(terminalSnapshot.ActiveAmbiguity);
        Assert.Null(terminalSnapshot.SelectedCandidate);
        Assert.Equal(
            [
                "catalog-selection event=prompt_scheduled promptId=selection.announce promptSha256=" +
                ComputePromptSha256("announce"),
                "catalog-selection event=candidates_matched candidateIds=[eins,zwei]",
                "catalog-selection event=prompt_scheduled promptId=selection.listening promptSha256=" +
                ComputePromptSha256("ambiguous"),
                "catalog-selection event=prompt_scheduled promptId=selection.listening promptSha256=" +
                ComputePromptSha256("recoverable-failure"),
                "catalog-selection event=prompt_scheduled promptId=selection.cancelled promptSha256=" +
                ComputePromptSha256("capture-failed"),
                "catalog-selection event=selection_cancelled reason=CaptureFaultBudgetExhausted"
            ],
            trace.Entries
                .Where(static entry => entry.Message.StartsWith("catalog-selection event=", StringComparison.Ordinal))
                .Select(static entry => entry.Message)
                .ToArray());
    }

    [Fact]
    public async Task ExecuteAsync_WhenConfirmationRecoverableFailuresPersist_TerminatesWithCaptureFaultReason()
    {
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);
        await using var worker = new SpeechRecognitionWorker(
            recognitionClient.Object,
            backoff: new RecordingBackoffPolicy(maxRetries: 1),
            maxQueueDepth: 1);
        SelectionDialogTurnSnapshot<string>? terminalSnapshot = null;

        captureClient
            .Setup(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance);
        recognitionClient
            .SetupSequence(client => client.SpeechToTextAsync(utterance, "de", It.IsAny<RecognitionProfile>(), It.IsAny<CancellationToken>(), It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new SpeechTranscript("eins", 98))
            .ThrowsAsync(new InvalidOperationException("synthetic failure"))
            .ThrowsAsync(new InvalidOperationException("synthetic failure"));

        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, worker, textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) => Task.FromResult(
                    CreateSelectionStep(
                        SelectionDialogPhase.Confirming,
                        snapshot,
                        transcript,
                        spokenPrompt: null)),
                createCancelledResult: (reason, snapshot) =>
                {
                    terminalSnapshot = snapshot;
                    return $"cancelled:{reason}";
                }));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        // Auch während der Bestätigung muss der zweite technische Fehler das begrenzte Fehlerbudget beenden.
        Assert.Equal("cancelled:CaptureFaultBudgetExhausted", result);
        Assert.Equal(
            ["announce", "confirmation-initial:eins", "recoverable-failure", "capture-failed"],
            textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray());
        Assert.NotNull(terminalSnapshot);
        Assert.Null(terminalSnapshot.ActiveAmbiguity);
        Assert.Null(terminalSnapshot.SelectedCandidate);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRecoverableFailureCounterResetsAfterCapture_DoesNotPrematurelyTerminate()
    {
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
            .ThrowsAsync(new InvalidOperationException("synthetic failure"))
            .ReturnsAsync(new SpeechTranscript("ack", 98))
            .ThrowsAsync(new InvalidOperationException("synthetic failure"))
            .ReturnsAsync(new SpeechTranscript("eins", 98));

        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, worker, textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) => string.Equals(transcript, "ack", StringComparison.Ordinal)
                    ? Task.FromResult(new SelectionDialogIntentStep<string>(
                        SelectionDialogPhase.Listening,
                        SelectionDialogCandidateTransition<string>.Preserve(),
                        SelectionDialogPrompt.FromText("ack-prompt")))
                    : Task.FromResult(CreateSelectionStep(
                        SelectionDialogPhase.Launch,
                        snapshot,
                        transcript,
                        spokenPrompt: null)),
                projectSelectedResult: (selected, _) => $"selected:{selected.Item}"));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        // Ein brauchbares Transkript zwischen technischen Fehlern setzt deren Zähler zurück;
        // der zweite einzelne Fehler darf deshalb noch keinen Fehlerabschluss erzeugen.
        Assert.Equal("selected:eins", result);
        Assert.Equal(
            ["announce", "recoverable-failure", "ack-prompt", "recoverable-failure"],
            textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray());
    }

    [Fact]
    public async Task ExecuteAsync_WhenListeningSilenceExceedsRetryBudget_KeepsRepromptingAndNeverTimesOut()
    {
        // Drei stille Versuche bei MaxRetries 2 müssen jeweils eine neue Frage erlauben;
        // das technische Fehlerbudget darf dadurch nicht erschöpft werden.
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);

        captureClient
            .SetupSequence(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NoSpeechDetectedException())
            .ThrowsAsync(new NoSpeechDetectedException())
            .ThrowsAsync(new NoSpeechDetectedException())
            .ReturnsAsync(utterance);
        recognitionClient
            .Setup(client => client.SpeechToTextAsync(utterance, "de", It.IsAny<RecognitionProfile>(), It.IsAny<CancellationToken>(), It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new SpeechTranscript("eins", 98));

        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, recognitionClient.Object, textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) => Task.FromResult(
                    CreateSelectionStep(SelectionDialogPhase.Launch, snapshot, transcript, spokenPrompt: null)),
                projectSelectedResult: (selected, _) => $"selected:{selected.Item}"));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("selected:eins", result);
        Assert.Equal(
            ["announce", "no-input", "no-input", "no-input"],
            textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray());
        Assert.DoesNotContain("capture-failed", textToSpeechPlayer.Calls.Select(static call => call.Text));
    }

    [Fact]
    public async Task ExecuteAsync_WhenConfirmationIsMetWithSilence_NeverTracesFabricatedDecisionInputs()
    {
        // Ohne Spracherkennung liegt kein Konfidenzwert vor. Eine spätere Bestätigung
        // darf dafür nicht einen erfundenen Messwert 0 anzeigen.
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var trace = new RecordingSpeechAssistantTrace();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);

        captureClient
            .SetupSequence(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance)
            .ThrowsAsync(new NoSpeechDetectedException())
            .ReturnsAsync(utterance);
        recognitionClient
            .SetupSequence(client => client.SpeechToTextAsync(
                utterance,
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new SpeechTranscript("eins", 98))
            .ReturnsAsync(new SpeechTranscript("ja", 96));

        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, recognitionClient.Object, textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) => Task.FromResult(
                    CreateSelectionStep(
                        SelectionDialogPhase.Confirming,
                        snapshot,
                        transcript,
                        spokenPrompt: null)),
                projectSelectedResult: (selected, _) => $"selected:{selected.Item}",
                speechAssistantTrace: trace,
                correlationId: "selection-silence"));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("selected:eins", result);
        var confirmationLines = trace.Entries
            .Select(static entry => entry.Message)
            .Where(static message => message.StartsWith("catalog-selection event=confirmation_", StringComparison.Ordinal))
            .ToArray();
        Assert.NotEmpty(confirmationLines);
        Assert.All(
            confirmationLines,
            message => Assert.DoesNotContain(" confidence=0 ", message, StringComparison.Ordinal));
        Assert.All(
            confirmationLines,
            message => Assert.Contains(" confidence=", message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WhenSilenceRepeatsInExplicitStage_PreservesStageAndRepairBudget()
    {
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);

        captureClient
            .SetupSequence(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance)
            .ReturnsAsync(utterance)
            .ThrowsAsync(new NoSpeechDetectedException())
            .ThrowsAsync(new NoSpeechDetectedException())
            .ThrowsAsync(new NoSpeechDetectedException())
            .ReturnsAsync(utterance);
        recognitionClient
            .SetupSequence(client => client.SpeechToTextAsync(
                utterance,
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new SpeechTranscript("eins", 98))
            .ReturnsAsync(new SpeechTranscript("ja", 64))
            .ReturnsAsync(new SpeechTranscript("eins", 65));

        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, recognitionClient.Object, textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) => Task.FromResult(
                    CreateSelectionStep(
                        SelectionDialogPhase.Confirming,
                        snapshot,
                        transcript,
                        spokenPrompt: null)),
                projectSelectedResult: (selected, _) => $"selected:{selected.Item}"));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("selected:eins", result);
        Assert.Equal(
            [
                "announce",
                "confirmation-initial:eins",
                "confirmation-explicit:1:eins",
                "confirmation-repeat:eins",
                "confirmation-repeat:eins",
                "confirmation-repeat:eins"
            ],
            textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray());
        Assert.DoesNotContain(
            "confirmation-repair-exhausted:eins",
            textToSpeechPlayer.Calls.Select(static call => call.Text));
    }

    [Fact]
    public async Task ExecuteAsync_WhenHelpThenRepeatDuringConfirmation_SpeaksContextualHelpThenReplaysStagePrompt()
    {
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);

        captureClient
            .Setup(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance);
        recognitionClient
            .SetupSequence(client => client.SpeechToTextAsync(
                utterance,
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new SpeechTranscript("eins", 98))
            .ReturnsAsync(new SpeechTranscript("hilfe", 98))
            .ReturnsAsync(new SpeechTranscript("wiederholen", 98))
            .ReturnsAsync(new SpeechTranscript("ja", 85));

        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, recognitionClient.Object, textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) => Task.FromResult(
                    CreateSelectionStep(
                        SelectionDialogPhase.Confirming,
                        snapshot,
                        transcript,
                        spokenPrompt: null)),
                projectSelectedResult: (selected, _) => $"selected:{selected.Item}"));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("selected:eins", result);
        Assert.Equal(
            [
                "announce",
                "confirmation-initial:eins",
                "confirmation-help:Initial:1:eins",
                "confirmation-initial:eins"
            ],
            textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray());
    }

    [Fact]
    public async Task ExecuteAsync_WhenListIsRequestedDuringConfirmation_SpeaksInitialInventoryThenReplaysStagePrompt()
    {
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);
        SelectionConfirmationPromptRequest<string>? listRequest = null;

        captureClient
            .Setup(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance);
        recognitionClient
            .SetupSequence(client => client.SpeechToTextAsync(
                utterance,
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new SpeechTranscript("refine", 98))
            .ReturnsAsync(new SpeechTranscript("zwei", 98))
            .ReturnsAsync(new SpeechTranscript("liste", 98))
            .ReturnsAsync(new SpeechTranscript("ja", 85));

        var definition = CreateDefinition(
            CreateSpeechTurnRunner(captureClient.Object, recognitionClient.Object, textToSpeechPlayer),
            "de",
            ["eins", "zwei", "drei"],
            onListeningInputAsync: (transcript, snapshot, _, _) =>
            {
                if (string.Equals(transcript, "refine", StringComparison.Ordinal))
                {
                    var refined = snapshot.VisibleCandidates.Revise(
                        snapshot.Revision.Next(),
                        snapshot.VisibleCandidates.Where(
                            candidate => !string.Equals(candidate.Item, "eins", StringComparison.Ordinal)));
                    return Task.FromResult(
                        new SelectionDialogIntentStep<string>(
                            SelectionDialogPhase.Listening,
                            SelectionDialogCandidateTransition<string>.ReplaceVisible(refined),
                            SelectionDialogPrompt.FromText("refined")));
                }

                return Task.FromResult(
                    CreateSelectionStep(
                        SelectionDialogPhase.Confirming,
                        snapshot,
                        transcript,
                        spokenPrompt: null));
            },
            projectSelectedResult: (selected, _) => $"selected:{selected.Item}",
            onConfirmationPromptRequest: request =>
            {
                if (request.Kind == SelectionConfirmationPromptKind.ActiveStageList)
                {
                    listRequest = request;
                }
            });
        var runtime = new SelectionDialogRuntime<string, string>(definition);

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("selected:zwei", result);
        var retainedListRequest = Assert.IsType<SelectionConfirmationPromptRequest<string>>(listRequest);
        Assert.Same(definition.Settings.InitialCandidates, retainedListRequest.RetainedCandidates);
        Assert.Equal(
            ["1:eins", "2:zwei", "3:drei"],
            retainedListRequest.RetainedCandidates
                .Select(candidate => $"{candidate.StableNumber}:{candidate.DisplayName}")
                .ToArray());
        Assert.Equal(
            [
                "announce",
                "refined",
                "confirmation-initial:zwei",
                "confirmation-list:1:eins|2:zwei|3:drei confirmation-initial:zwei"
            ],
            textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray());
    }

    [Theory]
    [InlineData(65)]
    [InlineData(68)]
    [InlineData(84)]
    [InlineData(85)]
    [InlineData(100)]
    public async Task ExecuteAsync_WhenInitialBoundConfirmationYesMeetsConfirmThreshold_Launches(
        int confirmationConfidence)
    {
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);

        captureClient
            .Setup(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance);
        recognitionClient
            .SetupSequence(client => client.SpeechToTextAsync(
                utterance,
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new SpeechTranscript("eins", 98))
            .ReturnsAsync(new SpeechTranscript("ja", confirmationConfidence));

        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, recognitionClient.Object, textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) => Task.FromResult(
                    CreateSelectionStep(
                        SelectionDialogPhase.Confirming,
                        snapshot,
                        transcript,
                        spokenPrompt: null)),
                projectSelectedResult: (selected, _) => $"selected:{selected.Item}"));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("selected:eins", result);
        Assert.Equal(
            ["announce", "confirmation-initial:eins"],
            textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray());
    }

    [Fact]
    public async Task ExecuteAsync_WhenInitialBoundYesIsBelowThreshold_ClearerBoundYesAtThresholdLaunches()
    {
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);

        captureClient
            .Setup(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance);
        recognitionClient
            .SetupSequence(client => client.SpeechToTextAsync(
                utterance,
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new SpeechTranscript("eins", 98))
            .ReturnsAsync(new SpeechTranscript("ja", 64))
            .ReturnsAsync(new SpeechTranscript("ja", 65));

        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, recognitionClient.Object, textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) => Task.FromResult(
                    CreateSelectionStep(
                        SelectionDialogPhase.Confirming,
                        snapshot,
                        transcript,
                        spokenPrompt: null)),
                projectSelectedResult: (selected, _) => $"selected:{selected.Item}"));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("selected:eins", result);
        Assert.Equal(
            [
                "announce",
                "confirmation-initial:eins",
                "confirmation-explicit:1:eins"
            ],
            textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray());
    }

    [Fact]
    public async Task ExecuteAsync_WhenInitialConfirmationYesIsLow_RequiresExactCandidateTarget()
    {
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var trace = new RecordingSpeechAssistantTrace();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);

        captureClient
            .Setup(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance);
        recognitionClient
            .SetupSequence(client => client.SpeechToTextAsync(
                utterance,
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new SpeechTranscript("eins", 98))
            .ReturnsAsync(new SpeechTranscript("ja", 64))
            .ReturnsAsync(new SpeechTranscript("eins", 65));

        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, recognitionClient.Object, textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) => Task.FromResult(
                    CreateSelectionStep(
                        SelectionDialogPhase.Confirming,
                        snapshot,
                        transcript,
                        spokenPrompt: null)),
                projectSelectedResult: (selected, _) => $"selected:{selected.Item}",
                speechAssistantTrace: trace,
                correlationId: "selection-repair"));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("selected:eins", result);
        Assert.Equal(
            [
                "announce",
                "confirmation-initial:eins",
                "confirmation-explicit:1:eins"
            ],
            textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray());
        Assert.Equal(
            [
                "catalog-selection event=prompt_scheduled promptId=selection.announce promptSha256=" +
                ComputePromptSha256("announce"),
                "catalog-selection event=candidates_matched candidateIds=[eins]",
                "catalog-selection event=candidate_chosen candidateId=eins",
                "catalog-selection event=prompt_scheduled promptId=selection.confirmation.initial candidateId=eins stage=Initial promptSha256=" +
                ComputePromptSha256("confirmation-initial:eins"),
                "catalog-selection event=confirmation_repair_requested candidateId=eins stage=ExplicitAuthorization confidence=64 intent=ConfirmYes competingEvidence=False",
                "catalog-selection event=prompt_scheduled promptId=selection.confirmation.explicitAuthorization candidateId=eins stage=ExplicitAuthorization promptSha256=" +
                ComputePromptSha256("confirmation-explicit:1:eins"),
                "catalog-selection event=confirmation_accepted candidateId=eins stage=ExplicitAuthorization confidence=65 intent=OpenCandidate competingEvidence=False",
                "catalog-selection event=selection_launch candidateId=eins"
            ],
            trace.Entries
                .Where(static entry => entry.Message.StartsWith("catalog-selection event=", StringComparison.Ordinal))
                .Select(static entry => entry.Message)
                .ToArray());
    }

    [Fact]
    public async Task ExecuteAsync_WhenInitialConfirmationYesHasMissingConfidence_RetriesThenHighYesLaunches()
    {
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);

        captureClient
            .Setup(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance);
        recognitionClient
            .SetupSequence(client => client.SpeechToTextAsync(
                utterance,
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new SpeechTranscript("eins", 98))
            .ReturnsAsync(new SpeechTranscript("ja", ConfidencePercent: null))
            .ReturnsAsync(new SpeechTranscript("ja", 98));

        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, recognitionClient.Object, textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) => Task.FromResult(
                    CreateSelectionStep(
                        SelectionDialogPhase.Confirming,
                        snapshot,
                        transcript,
                        spokenPrompt: null)),
                projectSelectedResult: (selected, _) => $"selected:{selected.Item}"));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("selected:eins", result);
        Assert.Equal(
            [
                "announce",
                "confirmation-initial:eins",
                "confirmation-verification-unavailable:eins"
            ],
            textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray());
    }

    [Fact]
    public async Task ExecuteAsync_WhenConfirmationConfidenceIsMissingTwice_UsesVerificationTerminal()
    {
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);

        captureClient
            .Setup(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance);
        recognitionClient
            .SetupSequence(client => client.SpeechToTextAsync(
                utterance,
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new SpeechTranscript("eins", 98))
            .ReturnsAsync(new SpeechTranscript("ja", ConfidencePercent: null))
            .ReturnsAsync(new SpeechTranscript("ja", ConfidencePercent: null));

        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, recognitionClient.Object, textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) => Task.FromResult(
                    CreateSelectionStep(
                        SelectionDialogPhase.Confirming,
                        snapshot,
                        transcript,
                        spokenPrompt: null)),
                createCancelledResult: (reason, _) => $"cancelled:{reason}"));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("cancelled:ConfirmationVerificationFailed", result);
        Assert.Equal(
            [
                "announce",
                "confirmation-initial:eins",
                "confirmation-verification-unavailable:eins",
                "confirmation-verification-failed:eins"
            ],
            textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray());
        recognitionClient.Verify(
            client => client.SpeechToTextAsync(
                utterance,
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<SpeechKeyterms?>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task ExecuteAsync_WhenWrongExplicitTargetsSpanFreshCandidates_UsesRepairTerminal()
    {
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);

        captureClient
            .Setup(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance);
        recognitionClient
            .SetupSequence(client => client.SpeechToTextAsync(
                utterance,
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new SpeechTranscript("eins", 98))
            .ReturnsAsync(new SpeechTranscript("ja", 64))
            .ReturnsAsync(new SpeechTranscript("falsch", 98))
            .ReturnsAsync(new SpeechTranscript("zwei", 98))
            .ReturnsAsync(new SpeechTranscript("ja", 64))
            .ReturnsAsync(new SpeechTranscript("falsch", 98));

        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, recognitionClient.Object, textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) => Task.FromResult(
                    CreateSelectionStep(
                        SelectionDialogPhase.Confirming,
                        snapshot,
                        transcript,
                        spokenPrompt: null)),
                createCancelledResult: (reason, _) => $"cancelled:{reason}"));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("cancelled:ConfirmationRepairExhausted", result);
        Assert.Equal(
            [
                "announce",
                "confirmation-initial:eins",
                "confirmation-explicit:1:eins",
                "confirmation-not-opened:eins",
                "confirmation-initial:zwei",
                "confirmation-explicit:2:zwei",
                "confirmation-repair-exhausted:zwei"
            ],
            textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray());
    }

    [Fact]
    public async Task ExecuteAsync_WhenConfirmationIsCancelled_UsesUserCancelledReason()
    {
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);

        captureClient
            .Setup(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance);
        recognitionClient
            .SetupSequence(client => client.SpeechToTextAsync(
                utterance,
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new SpeechTranscript("eins", 98))
            .ReturnsAsync(new SpeechTranscript("abbrechen", 98));

        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, recognitionClient.Object, textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (transcript, snapshot, _, _) => Task.FromResult(
                    CreateSelectionStep(
                        SelectionDialogPhase.Confirming,
                        snapshot,
                        transcript,
                        spokenPrompt: null)),
                createCancelledResult: (reason, _) => $"cancelled:{reason}"));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("cancelled:UserCancelled", result);
        Assert.Equal(
            [
                "announce",
                "confirmation-initial:eins",
                "confirmation-cancel:eins"
            ],
            textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray());
    }

    [Theory]
    [InlineData(SelectionDialogCompletionReason.UserCancelled)]
    [InlineData(SelectionDialogCompletionReason.RestartRequested)]
    [InlineData(SelectionDialogCompletionReason.CaptureFaultBudgetExhausted)]
    public async Task ExecuteAsync_WhenListeningStepAppliesSpokenCompletion_TracesTerminalPromptFromAppliedState(
        SelectionDialogCompletionReason completionReason)
    {
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var trace = new RecordingSpeechAssistantTrace();
        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, recognitionClient.Object, textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (_, _, _, _) => Task.FromResult(
                    new SelectionDialogIntentStep<string>(
                        SelectionDialogPhase.Completed,
                        SelectionDialogCandidateTransition<string>.ClearTerminal(),
                        SelectionDialogPrompt.FromText("terminal-prompt"),
                        completionReason)),
                createCancelledResult: (reason, _) => $"cancelled:{reason}",
                initialListeningTranscript: new SpeechTranscript("terminal-command", ConfidencePercent: 98),
                speechAssistantTrace: trace,
                correlationId: "terminal-prompt"));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal($"cancelled:{completionReason}", result);
        Assert.Equal(["terminal-prompt"], textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray());
        Assert.Equal(
            [
                "catalog-selection event=prompt_scheduled promptId=selection.cancelled promptSha256=" +
                ComputePromptSha256("terminal-prompt"),
                $"catalog-selection event=selection_cancelled reason={completionReason}"
            ],
            trace.Entries
                .Where(static entry => entry.Message.StartsWith("catalog-selection event=", StringComparison.Ordinal))
                .Select(static entry => entry.Message)
                .ToArray());
    }

    [Fact]
    public async Task ExecuteAsync_TracesAppliedSelectionAndScheduledAnnouncementWithoutTranscriptLeak()
    {
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var trace = new RecordingSpeechAssistantTrace();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);

        captureClient
            .Setup(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance);
        recognitionClient
            .Setup(client => client.SpeechToTextAsync(utterance, "de", It.IsAny<RecognitionProfile>(), It.IsAny<CancellationToken>(), It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new SpeechTranscript("private recognized phrase", 98));

        var runtime = new SelectionDialogRuntime<string, string>(
            CreateDefinition(
                CreateSpeechTurnRunner(captureClient.Object, recognitionClient.Object, textToSpeechPlayer),
                "de",
                ["eins", "zwei"],
                onListeningInputAsync: (_, snapshot, _, _) => Task.FromResult(
                    CreateSelectionStep(SelectionDialogPhase.Launch, snapshot, "eins", spokenPrompt: null)),
                projectSelectedResult: (selected, _) => $"selected:{selected.Item}",
                speechAssistantTrace: trace,
                correlationId: "selection-test"));

        var result = await runtime.ExecuteAsync(CancellationToken.None);

        Assert.Equal("selected:eins", result);

        var evidenceEntries = trace.Entries
            .Where(static entry => entry.Message.StartsWith("catalog-selection event=", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(
            [
                "catalog-selection event=prompt_scheduled promptId=selection.announce promptSha256=" +
                ComputePromptSha256("announce"),
                "catalog-selection event=candidates_matched candidateIds=[eins]",
                "catalog-selection event=candidate_chosen candidateId=eins",
                "catalog-selection event=selection_launch candidateId=eins"
            ],
            evidenceEntries.Select(static entry => entry.Message).ToArray());
        Assert.All(evidenceEntries, entry =>
        {
            Assert.Equal(SpeechAssistantTraceScope.SelectionDialogRuntime, entry.Scope);
            Assert.Equal(SpeechAssistantTraceLevel.Debug, entry.Level);
        });

        // Das eingespeiste private Transkript darf in keiner hier erfassten Diagnosezeile erscheinen.
        Assert.DoesNotContain(
            trace.Entries,
            entry => entry.Message.Contains("private recognized phrase", StringComparison.Ordinal));
    }

    private static string ComputePromptSha256(string text)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

    private static SelectionDialogSpeechTurnRunner CreateSpeechTurnRunner(
        ISpeechCaptureClient captureClient,
        ISpeechRecognitionClient recognitionClient,
        ITextToSpeechPlayer textToSpeechPlayer)
    {
        return CreateSpeechTurnRunner(
            captureClient,
            new SpeechRecognitionWorker(recognitionClient),
            textToSpeechPlayer);
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
        Func<string, SelectionDialogTurnSnapshot<string>, SelectionDialogPrompt?, CancellationToken, Task<SelectionDialogIntentStep<string>>>? onListeningInputAsync = null,
        Func<SelectionCandidate<string>, SelectionDialogTurnSnapshot<string>, string>? projectSelectedResult = null,
        Func<SelectionCandidate<string>, SelectionDialogTurnSnapshot<string>, string>? projectSingleItemResult = null,
        Func<string, CancellationToken, Task<SelectionDialogCommonIntentParseResult>>? onConfirmInputAsync = null,
        Func<SelectionDialogCompletionReason, SelectionDialogTurnSnapshot<string>, string>? createCancelledResult = null,
        SpeechTranscript? initialListeningTranscript = null,
        ISpeechAssistantTrace? speechAssistantTrace = null,
        string? correlationId = null,
        string recoverableFailurePromptText = "recoverable-failure",
        string captureFailedTerminalPromptText = "capture-failed",
        Action<SelectionDialogTurnSnapshot<string>>? onEmptyItemsPromptSnapshot = null,
        Action<SelectionDialogTurnSnapshot<string>>? onAnnouncingPromptSnapshot = null,
        Action<SelectionConfirmationPromptRequest<string>>? onConfirmationPromptRequest = null,
        Func<SelectionCandidate<string>, SelectionDialogTurnSnapshot<string>, SelectionDialogPrompt>? buildSelectedPrompt = null,
        Func<SelectionCandidate<string>, SelectionDialogTurnSnapshot<string>, SelectionDialogPrompt>? buildSingleItemPrompt = null)
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
                initialListeningTranscript: initialListeningTranscript,
                trace: new SpeechAssistantTraceContext(
                    speechAssistantTrace,
                    correlationId,
                    SpeechAssistantTraceScope.SelectionDialogRuntime)),
            new SelectionDialogPromptProviders<string>(
                getEmptyItemsPromptAsync: (snapshot, _) =>
                {
                    onEmptyItemsPromptSnapshot?.Invoke(snapshot);
                    return Task.FromResult(SelectionDialogPrompt.FromText("none"));
                },
                getAnnouncingPromptAsync: (snapshot, _) =>
                {
                    onAnnouncingPromptSnapshot?.Invoke(snapshot);
                    return Task.FromResult(SelectionDialogPrompt.FromText("announce"));
                },
                getNoInputPromptAsync: (_, _) => Task.FromResult(SelectionDialogPrompt.FromText("no-input")),
                getReListenPromptAsync: (_, _) => Task.FromResult(SelectionDialogPrompt.FromText("re-listen")),
                getCaptureFailedTerminalPromptAsync: (_, _) => Task.FromResult(
                    SelectionDialogPrompt.FromText(captureFailedTerminalPromptText)),
                getRecoverableFailurePromptAsync: (_, _) => Task.FromResult(
                    SelectionDialogPrompt.FromText(recoverableFailurePromptText)),
                getConfirmationPromptAsync: (request, _) =>
                {
                    onConfirmationPromptRequest?.Invoke(request);
                    return Task.FromResult(CreateConfirmationPrompt(request));
                }),
            new SelectionDialogOutcomeHandlers<string, string>(
                onListeningInputAsync: onListeningInputAsync ?? ((_, _, _, _) => Task.FromResult(
                    new SelectionDialogIntentStep<string>(
                        SelectionDialogPhase.Completed,
                        SelectionDialogCandidateTransition<string>.ClearTerminal(),
                        spokenPrompt: null))),
                onConfirmInputAsync: onConfirmInputAsync ?? ParseConfirmationInputAsync,
                selectedLaunch: new SelectionDialogLaunchDefinition<string, string>(
                    buildPrompt: buildSelectedPrompt ?? ((_, _) => SelectionDialogPrompt.FromText(null)),
                    projectResult: projectSelectedResult ?? ((_, _) => "selected")),
                createCancelledResult: createCancelledResult ?? ((_, _) => "cancelled"),
                singleItemLaunch: projectSingleItemResult is null
                    ? null
                    : new SelectionDialogLaunchDefinition<string, string>(
                        buildPrompt: buildSingleItemPrompt ?? ((_, _) => SelectionDialogPrompt.FromText(null)),
                        projectResult: projectSingleItemResult)),
            confirmationPolicy);
    }

    private static SelectionDialogPrompt CreateConfirmationPrompt(
        SelectionConfirmationPromptRequest<string> request)
    {
        var candidate = request.Candidate;
        var text = request.Kind switch
        {
            SelectionConfirmationPromptKind.InitialConfirmation =>
                $"confirmation-initial:{candidate.DisplayName}",
            SelectionConfirmationPromptKind.LowConfidenceConfirmation =>
                $"confirmation-low:{candidate.DisplayName}",
            SelectionConfirmationPromptKind.ExplicitAuthorization =>
                $"confirmation-explicit:{candidate.StableNumber}:{candidate.DisplayName}",
            SelectionConfirmationPromptKind.VerificationUnavailable =>
                $"confirmation-verification-unavailable:{candidate.DisplayName}",
            SelectionConfirmationPromptKind.NotOpenedReselect =>
                $"confirmation-not-opened:{candidate.DisplayName}",
            SelectionConfirmationPromptKind.Cancellation =>
                $"confirmation-cancel:{candidate.DisplayName}",
            SelectionConfirmationPromptKind.ContextualHelp =>
                $"confirmation-help:{request.Stage}:{candidate.StableNumber}:{candidate.DisplayName}",
            SelectionConfirmationPromptKind.NoInputReminder =>
                $"confirmation-repeat:{candidate.DisplayName}",
            SelectionConfirmationPromptKind.ActiveStage =>
                request.ActiveStagePrompt?.Text
                ?? throw new InvalidOperationException("An active-stage test prompt is required."),
            SelectionConfirmationPromptKind.ActiveStageList =>
                $"confirmation-list:{string.Join(
                    "|",
                    request.RetainedCandidates.Select(
                        retained => $"{retained.StableNumber}:{retained.DisplayName}"))} " +
                (request.ActiveStagePrompt?.Text
                 ?? throw new InvalidOperationException("An active-stage list test prompt is required.")),
            SelectionConfirmationPromptKind.VerificationFailedTerminal =>
                $"confirmation-verification-failed:{candidate.DisplayName}",
            SelectionConfirmationPromptKind.RepairExhaustedTerminal =>
                $"confirmation-repair-exhausted:{candidate.DisplayName}",
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Kind, "Unknown test prompt kind.")
        };
        return SelectionDialogPrompt.FromText(text);
    }

    private static Task<SelectionDialogCommonIntentParseResult> ParseConfirmationInputAsync(
        string transcript,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = CompactForTest(transcript);
        var result = normalized switch
        {
            "ja" or "yes" => new SelectionDialogCommonIntentParseResult(
                SelectionDialogCommonIntent.ConfirmYes),
            "nein" or "no" => new SelectionDialogCommonIntentParseResult(
                SelectionDialogCommonIntent.ConfirmNo),
            "abbrechen" or "cancel" => new SelectionDialogCommonIntentParseResult(
                SelectionDialogCommonIntent.Cancel),
            "hilfe" or "help" => new SelectionDialogCommonIntentParseResult(
                SelectionDialogCommonIntent.Help),
            "wiederholen" or "repeat" => new SelectionDialogCommonIntentParseResult(
                SelectionDialogCommonIntent.Repeat),
            "liste" or "list" => new SelectionDialogCommonIntentParseResult(
                SelectionDialogCommonIntent.List),
            _ when int.TryParse(normalized, out var number) => new SelectionDialogCommonIntentParseResult(
                SelectionDialogCommonIntent.SelectByNumber,
                Number: number),
            _ => new SelectionDialogCommonIntentParseResult(
                SelectionDialogCommonIntent.OpenCandidate,
                TargetName: transcript.Trim())
        };
        return Task.FromResult(result);
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

    private static SelectionCandidateSnapshot<string> CreateInitialCandidateSnapshot(
        IReadOnlyList<string> items)
    {
        return CreateCandidateMatcher().CreateSnapshot(items, SelectionRevision.Initial);
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
