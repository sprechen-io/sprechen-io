using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Annahmehinweise bei Einrichtung, Auswahl und Formularaufnahme. Erfolgreiche
/// Standardaufnahmen fordern einen Hinweis an. Fehlende Sprache und ausdrücklich
/// verschobene oder unterdrückte Rückmeldung dürfen den allgemeinen Hinweis nicht auslösen.
/// </summary>
public sealed class InputAcceptedCueInvocationTests
{
    private static CapturedSpeechAudio Utterance() =>
        new(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);

    private static Mock<ISpeechCaptureRecordingIndicator> CreateRecordingIndicatorSpy()
    {
        var indicator = new Mock<ISpeechCaptureRecordingIndicator>();
        indicator
            .Setup(i => i.BeginRecordingAsync())
            .Returns(new ValueTask<IAsyncDisposable>(Mock.Of<IAsyncDisposable>()));
        indicator
            .Setup(i => i.EnsureStoppedAsync())
            .Returns(ValueTask.CompletedTask);
        indicator
            .Setup(i => i.PlayInputAcceptedCueAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        return indicator;
    }

    [Fact]
    public async Task WakeSetup_WhenSpeechCaptured_PlaysInputAcceptedCueOnce()
    {
        var indicator = CreateRecordingIndicatorSpy();
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(Utterance()));
        speechClient.EnqueueTranscript("ja");
        var worker = new SpeechRecognitionWorker(speechClient);
        var interactor = new WakeWordSetupSpeechInteractor(
            speechClient,
            worker,
            new RecordingTextToSpeechPlayer(),
            new NoOpEarconPlayer(),
            indicator.Object,
            NullLogger<WakeWordSetupSpeechInteractor>.Instance);

        var result = await interactor.PromptSpeechToTextAsync("Bitte sprechen", "de", CancellationToken.None);

        Assert.Equal("ja", result.Transcript);
        indicator.Verify(i => i.PlayInputAcceptedCueAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WakeSetup_WhenNoSpeech_DoesNotPlayInputAcceptedCue()
    {
        var indicator = CreateRecordingIndicatorSpy();
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(_ => throw new NoSpeechDetectedException());
        var worker = new SpeechRecognitionWorker(speechClient);
        var interactor = new WakeWordSetupSpeechInteractor(
            speechClient,
            worker,
            new RecordingTextToSpeechPlayer(),
            new NoOpEarconPlayer(),
            indicator.Object,
            NullLogger<WakeWordSetupSpeechInteractor>.Instance);

        var result = await interactor.PromptSpeechToTextAsync("Bitte sprechen", "de", CancellationToken.None);

        Assert.Null(result.Transcript);
        indicator.Verify(i => i.PlayInputAcceptedCueAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SelectionListening_WhenSpeechCaptured_PlaysInputAcceptedCueOnce()
    {
        var indicator = CreateRecordingIndicatorSpy();
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(Utterance()));
        speechClient.EnqueueTranscript("eins");
        var turn = CreateListeningTurn(indicator.Object, speechClient);

        var result = await turn.ListenAsync("de", noInputTimeoutMs: 500, CancellationToken.None);

        Assert.Equal("eins", result.Transcript);
        indicator.Verify(i => i.PlayInputAcceptedCueAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SelectionListening_WhenNoSpeech_DoesNotPlayInputAcceptedCue()
    {
        var indicator = CreateRecordingIndicatorSpy();
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(_ => throw new NoSpeechDetectedException());
        var turn = CreateListeningTurn(indicator.Object, speechClient);

        var result = await turn.ListenAsync("de", noInputTimeoutMs: 500, CancellationToken.None);

        Assert.Null(result.Transcript);
        indicator.Verify(i => i.PlayInputAcceptedCueAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SelectionConfirm_RunsInShortConfirmationMode_AndDelegatesLookBackToCaptureLayer()
    {
        // Die Bestätigung übergibt ShortConfirmation ohne eigenen Startcursor; die Cursorwahl bleibt bei der Aufnahmeschicht.
        var indicator = CreateRecordingIndicatorSpy();
        var speechClient = new ScriptedSpeechInputClient { LiveCaptureCursor = new SampleCursor(7) };
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(Utterance()));
        speechClient.EnqueueTranscript("ja");
        var turn = CreateListeningTurn(indicator.Object, speechClient);

        var result = await turn.ListenAsync(
            "de",
            noInputTimeoutMs: 500,
            CancellationToken.None,
            captureMode: SpeechCaptureMode.ShortConfirmation);

        Assert.Equal("ja", result.Transcript);
        Assert.Contains(SpeechCaptureMode.ShortConfirmation, speechClient.RecordModes);
        Assert.Empty(speechClient.StartCursors);
    }

    [Fact]
    public async Task SelectionListening_FieldValueTurn_DoesNotSnapshotCallSiteCursor()
    {
        // Auch Listening überlässt die Startcursorwahl der Aufnahmeschicht.
        var indicator = CreateRecordingIndicatorSpy();
        var speechClient = new ScriptedSpeechInputClient { LiveCaptureCursor = new SampleCursor(7) };
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(Utterance()));
        speechClient.EnqueueTranscript("eins");
        var turn = CreateListeningTurn(indicator.Object, speechClient);

        var result = await turn.ListenAsync("de", noInputTimeoutMs: 500, CancellationToken.None);

        Assert.Equal("eins", result.Transcript);
        Assert.Empty(speechClient.StartCursors);
    }

    private static SelectionDialogListeningTurn CreateListeningTurn(
        ISpeechCaptureRecordingIndicator indicator,
        ScriptedSpeechInputClient speechClient)
    {
        var transcriber = new SelectionDialogSpeechTranscriber(new SpeechRecognitionWorker(speechClient));
        var failureMapper = new SelectionDialogRecoverableSpeechFailureMapper(NullLogger.Instance);
        return new SelectionDialogListeningTurn(
            indicator,
            speechClient,
            transcriber,
            failureMapper,
            NullLogger.Instance);
    }

    [Fact]
    public async Task SelectionBargeIn_WhenSpeechCaptured_PlaysInputAcceptedCueOnce()
    {
        var indicator = CreateRecordingIndicatorSpy();
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(Utterance()));
        speechClient.EnqueueTranscript("zwei");
        var turn = CreateBargeInTurn(indicator.Object, speechClient);

        var result = await turn.SpeakAndListenAsync(
            SelectionDialogPrompt.FromText("prompt"),
            "de",
            new SelectionDialogInteractionSettings(1.0d, 0, true, 85, 65),
            noInputTimeoutMs: 500,
            CancellationToken.None);

        Assert.Equal("zwei", result.Transcript);
        indicator.Verify(i => i.PlayInputAcceptedCueAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SelectionBargeIn_WhenNoSpeech_DoesNotPlayInputAcceptedCue()
    {
        var indicator = CreateRecordingIndicatorSpy();
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(_ => throw new NoSpeechDetectedException());
        var turn = CreateBargeInTurn(indicator.Object, speechClient);

        var result = await turn.SpeakAndListenAsync(
            SelectionDialogPrompt.FromText("prompt"),
            "de",
            new SelectionDialogInteractionSettings(1.0d, 0, true, 85, 65),
            noInputTimeoutMs: 500,
            CancellationToken.None);

        Assert.Null(result.Transcript);
        indicator.Verify(i => i.PlayInputAcceptedCueAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static SelectionDialogBargeInTurn CreateBargeInTurn(
        ISpeechCaptureRecordingIndicator indicator,
        ScriptedSpeechInputClient speechClient)
    {
        var transcriber = new SelectionDialogSpeechTranscriber(new SpeechRecognitionWorker(speechClient));
        var failureMapper = new SelectionDialogRecoverableSpeechFailureMapper(NullLogger.Instance);
        var promptSpeaker = new SelectionDialogPromptSpeaker(new RecordingTextToSpeechPlayer(), NullLogger.Instance);
        return new SelectionDialogBargeInTurn(
            indicator,
            speechClient,
            promptSpeaker,
            transcriber,
            failureMapper,
            NullLogger.Instance);
    }

    [Fact]
    public async Task FormCaptureBargeIn_WhenSpeechCaptured_PlaysInputAcceptedCueOnce()
    {
        var indicator = CreateRecordingIndicatorSpy();
        var recordingService = new Mock<IFormVoiceCommandRecordingService>();
        recordingService
            .Setup(r => r.RecordUntilSpeechAsync(
                SpeechCaptureMode.PromptInterruption,
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Utterance());
        recordingService
            .Setup(r => r.RecordUntilSpeechAsync(
                SpeechCaptureMode.PromptInterruption,
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<SampleCursor?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Utterance());
        var runner = CreateFormRunner(indicator.Object, recordingService.Object);

        await runner.CaptureAsync(CreateRequest(allowBargeIn: true), CancellationToken.None);

        indicator.Verify(i => i.PlayInputAcceptedCueAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FormCaptureBargeIn_WhenNoSpeech_DoesNotPlayInputAcceptedCue()
    {
        var indicator = CreateRecordingIndicatorSpy();
        var recordingService = new Mock<IFormVoiceCommandRecordingService>();
        recordingService
            .Setup(r => r.RecordUntilSpeechAsync(
                SpeechCaptureMode.PromptInterruption,
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NoSpeechDetectedException());
        recordingService
            .Setup(r => r.RecordUntilSpeechAsync(
                SpeechCaptureMode.PromptInterruption,
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<SampleCursor?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NoSpeechDetectedException());
        var runner = CreateFormRunner(indicator.Object, recordingService.Object);

        await Assert.ThrowsAsync<NoSpeechDetectedException>(
            () => runner.CaptureAsync(CreateRequest(allowBargeIn: true), CancellationToken.None));

        indicator.Verify(i => i.PlayInputAcceptedCueAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FormCaptureAfterPrompt_WhenSpeechCaptured_PlaysInputAcceptedCueOnce()
    {
        var indicator = CreateRecordingIndicatorSpy();
        var recordingService = new Mock<IFormVoiceCommandRecordingService>();
        recordingService
            .Setup(r => r.SnapshotLiveCaptureCursor())
            .Returns((SampleCursor?)null);
        recordingService
            .Setup(r => r.RecordUntilSpeechAsync(
                SpeechCaptureMode.Listening,
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<SampleCursor?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Utterance());
        var runner = CreateFormRunner(indicator.Object, recordingService.Object);

        await runner.CaptureAsync(CreateRequest(allowBargeIn: false), CancellationToken.None);

        indicator.Verify(i => i.PlayInputAcceptedCueAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(SpeechInputAcceptanceFeedback.PostInterpretation)]
    [InlineData(SpeechInputAcceptanceFeedback.CueFree)]
    public async Task FormCaptureAfterPrompt_WhenFeedbackHasAnotherOwner_DoesNotPlayGenericCue(
        SpeechInputAcceptanceFeedback feedback)
    {
        var indicator = CreateRecordingIndicatorSpy();
        var recordingService = new Mock<IFormVoiceCommandRecordingService>();
        recordingService
            .Setup(r => r.RecordUntilSpeechAsync(
                SpeechCaptureMode.Listening,
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<SampleCursor?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Utterance());
        var runner = CreateFormRunner(indicator.Object, recordingService.Object);

        await runner.CaptureAsync(
            CreateRequest(allowBargeIn: false, inputAcceptanceFeedback: feedback),
            CancellationToken.None);

        indicator.Verify(i => i.PlayInputAcceptedCueAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FormCaptureListeningCueOnly_BeginsRecordingWithoutReplacementSpeech()
    {
        var sequence = new List<string>();
        var indicator = CreateRecordingIndicatorSpy();
        indicator
            .Setup(i => i.BeginRecordingAsync())
            .Callback(() => sequence.Add("listening"))
            .Returns(new ValueTask<IAsyncDisposable>(Mock.Of<IAsyncDisposable>()));
        var recordingService = new Mock<IFormVoiceCommandRecordingService>(MockBehavior.Strict);
        recordingService
            .Setup(r => r.RecordUntilSpeechAsync(
                SpeechCaptureMode.Listening,
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<SampleCursor?>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => sequence.Add("record"))
            .ReturnsAsync(Utterance());
        var runner = CreateFormRunner(
            indicator.Object,
            recordingService.Object,
            message => sequence.Add($"tts:{message}"));

        await runner.CaptureAsync(
            CreateRequest(
                allowBargeIn: false,
                promptPresentation: SpeechCapturePromptPresentation.ListeningCueOnly,
                inputAcceptanceFeedback: SpeechInputAcceptanceFeedback.CueFree),
            CancellationToken.None);

        Assert.Equal(["listening", "record"], sequence);
        indicator.Verify(i => i.PlayInputAcceptedCueAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FormCaptureAfterPrompt_WhenNoSpeech_DoesNotPlayInputAcceptedCue()
    {
        var indicator = CreateRecordingIndicatorSpy();
        var recordingService = new Mock<IFormVoiceCommandRecordingService>();
        recordingService
            .Setup(r => r.SnapshotLiveCaptureCursor())
            .Returns((SampleCursor?)null);
        recordingService
            .Setup(r => r.RecordUntilSpeechAsync(
                SpeechCaptureMode.Listening,
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<SampleCursor?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NoSpeechDetectedException());
        var runner = CreateFormRunner(indicator.Object, recordingService.Object);

        await Assert.ThrowsAsync<NoSpeechDetectedException>(
            () => runner.CaptureAsync(CreateRequest(allowBargeIn: false), CancellationToken.None));

        indicator.Verify(i => i.PlayInputAcceptedCueAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FormCaptureAfterPrompt_DoesNotSnapshotCallSiteCursor_AndDelegatesLookBackToCaptureLayer()
    {
        // Der Bereitschaftshinweis muss vor der Aufnahme angefordert werden. Der strikte
        // Testzugang erlaubt keinen eigenen SnapshotLiveCaptureCursor-Aufruf; Startcursor bleibt null.
        var sequence = new List<string>();
        SampleCursor? capturedStartCursor = new SampleCursor(99);

        var indicator = new Mock<ISpeechCaptureRecordingIndicator>();
        indicator
            .Setup(i => i.BeginRecordingAsync())
            .Callback(() => sequence.Add("cue"))
            .Returns(new ValueTask<IAsyncDisposable>(Mock.Of<IAsyncDisposable>()));
        indicator
            .Setup(i => i.PlayInputAcceptedCueAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var recordingService = new Mock<IFormVoiceCommandRecordingService>(MockBehavior.Strict);
        recordingService
            .Setup(r => r.RecordUntilSpeechAsync(
                SpeechCaptureMode.Listening,
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<SampleCursor?>(),
                It.IsAny<CancellationToken>()))
            .Callback((SpeechCaptureMode _, VoiceDialogSettings _, SampleCursor? startCursor, CancellationToken _) =>
            {
                sequence.Add("record");
                capturedStartCursor = startCursor;
            })
            .ReturnsAsync(Utterance());

        var runner = CreateFormRunner(indicator.Object, recordingService.Object);

        await runner.CaptureAsync(CreateRequest(allowBargeIn: false), CancellationToken.None);

        Assert.Equal(["cue", "record"], sequence);
        Assert.Null(capturedStartCursor);
    }

    private static FormVoiceCommandPromptCaptureRunner CreateFormRunner(
        ISpeechCaptureRecordingIndicator indicator,
        IFormVoiceCommandRecordingService recordingService,
        Action<string>? onSpoken = null)
    {
        var bargeInSignals = new Mock<ISpeechBargeInSignals>();
        bargeInSignals
            .Setup(s => s.SubscribeToBargeIn(It.IsAny<Func<ValueTask>>()))
            .Returns(Mock.Of<IDisposable>());
        var promptPlayback = new FormVoiceCommandPromptPlayback(
            new RecordingTextToSpeechPlayer(onSpoken),
            NullLogger.Instance);
        return new FormVoiceCommandPromptCaptureRunner(
            Mock.Of<IEvaluationSessionTelemetry>(),
            indicator,
            bargeInSignals.Object,
            recordingService,
            promptPlayback);
    }

    private static FormVoiceCommandCaptureRequest CreateRequest(
        bool allowBargeIn,
        SpeechCapturePromptPresentation promptPresentation = SpeechCapturePromptPresentation.Spoken,
        SpeechInputAcceptanceFeedback inputAcceptanceFeedback = SpeechInputAcceptanceFeedback.Standard) =>
        new(
            "Bitte sprechen",
            "de",
            new FormVoiceCommandCaptureSettings(
                new VoiceDialogSettings { AllowBargeIn = allowBargeIn },
                0,
                promptPresentation,
                inputAcceptanceFeedback),
            static () => { },
            null);
}
