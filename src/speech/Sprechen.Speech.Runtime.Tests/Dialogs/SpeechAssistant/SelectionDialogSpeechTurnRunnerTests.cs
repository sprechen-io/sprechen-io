using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Speech.Runtime.Tests.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class SelectionDialogSpeechTurnRunnerTests
{
    [Fact]
    public async Task SpeakAsync_WhenPromptContainsItems_AppliesPauseOnlyBetweenItemSegments()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var runner = CreateSpeechTurnRunner(speechClient, textToSpeechPlayer);
        var prompt = SelectionDialogPrompt.FromSequence("intro", ["item-1", "item-2"], "trailing");

        var speakTask = runner.SpeakAsync(prompt, "de", new SelectionDialogInteractionSettings(1.4d, 180, false, 85, 65), CancellationToken.None);

        await speakTask;

        var callTimestamps = textToSpeechPlayer.CallTimestamps;

        Assert.Equal(4, textToSpeechPlayer.Calls.Count);
        Assert.Equal(
            ["intro", "item-1", "item-2", "trailing"],
            textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray());
        Assert.All(textToSpeechPlayer.Calls, call => Assert.Equal(1.4d, call.SpeechRate));
        Assert.True(
            callTimestamps[2] - callTimestamps[1] >= TimeSpan.FromMilliseconds(120),
            "Expected a measurable pacing delay between numbered item announcements.");
    }

    [Fact]
    public async Task SpeakAndListenAsync_WhenBargeInEnabled_UsesPromptInterruptionMode()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var runner = CreateSpeechTurnRunner(speechClient, textToSpeechPlayer);

        await speechClient.EnqueueRecordStepAsync(async _ =>
        {
            await textToSpeechPlayer.WaitForFirstCallAsync(TimeSpan.FromSeconds(1));
            return new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);
        });
        speechClient.EnqueueTranscript("eins");

        var result = await runner.SpeakAndListenAsync(
            SelectionDialogPrompt.FromText("prompt"),
            "de",
            new SelectionDialogInteractionSettings(1.0d, 0, true, 85, 65),
            noInputTimeoutMs: 500,
            CancellationToken.None);

        Assert.Equal("eins", result.Transcript);
        Assert.Null(result.SttConfidencePercent);
        Assert.Null(result.FailureCode);
        Assert.Null(result.Recoverable);
        Assert.Equal([SpeechCaptureMode.PromptInterruption], speechClient.RecordModes);
        Assert.Equal(1, speechClient.RecordCallCount);
        Assert.Equal(1, speechClient.SpeechToTextCallCount);
        Assert.All(textToSpeechPlayer.Calls, call => Assert.Equal(1.0d, call.SpeechRate));
    }

    [Fact]
    public async Task SpeakAndListenAsync_WhenBargeInDisabled_UsesListeningMode()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var runner = CreateSpeechTurnRunner(speechClient, textToSpeechPlayer);

        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(
            SpeechAudioMimeType.Pcm,
            [Array.Empty<byte>()])));
        speechClient.EnqueueTranscript("zwei");

        var result = await runner.SpeakAndListenAsync(
            SelectionDialogPrompt.FromSequence("intro", ["item-1", "item-2"]),
            "de",
            new SelectionDialogInteractionSettings(1.0d, 0, false, 85, 65),
            noInputTimeoutMs: 500,
            CancellationToken.None);

        Assert.Equal("zwei", result.Transcript);
        Assert.Null(result.SttConfidencePercent);
        Assert.Null(result.FailureCode);
        Assert.Null(result.Recoverable);
        Assert.Equal([SpeechCaptureMode.Listening], speechClient.RecordModes);
        Assert.Equal(
            ["intro", "item-1", "item-2"],
            textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray());
        Assert.All(textToSpeechPlayer.Calls, call => Assert.Equal(1.0d, call.SpeechRate));
    }

    [Fact]
    public async Task SpeakAndListenAsync_WhenBargeInDisabledWithShortConfirmationRequested_UsesShortConfirmationMode()
    {
        // Eine Ja/Nein-Antwort nach einer Ansage muss ShortConfirmation bis zum modusbewussten Aufnahmeclient weitergeben.
        var speechClient = new ScriptedSpeechInputClient();
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var runner = CreateSpeechTurnRunner(speechClient, textToSpeechPlayer);

        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(
            SpeechAudioMimeType.Pcm,
            [Array.Empty<byte>()])));
        speechClient.EnqueueTranscript("ja");

        var result = await runner.SpeakAndListenAsync(
            SelectionDialogPrompt.FromText("Ist das richtig?"),
            "de",
            new SelectionDialogInteractionSettings(1.0d, 0, false, 85, 65),
            noInputTimeoutMs: 500,
            CancellationToken.None,
            keyterms: null,
            SpeechCaptureMode.ShortConfirmation);

        Assert.Equal("ja", result.Transcript);
        Assert.Equal([SpeechCaptureMode.ShortConfirmation], speechClient.RecordModes);
    }

    [Fact]
    public async Task SpeakAndListenAsync_WhenRecognitionKeepsReturningEmptyTranscript_SilentlyRelistensThenReturnsNull()
    {
        // Leere Erkennungsergebnisse erlauben begrenzte weitere Aufnahme im selben Zug,
        // ohne jeweils neue Ansage und Bereitschaftston; nach der Grenze erhält der Aufrufer das leere Ergebnis.
        var speechClient = new ScriptedSpeechInputClient();
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var runner = CreateSpeechTurnRunner(speechClient, textToSpeechPlayer);

        for (var i = 0; i < 3; i++)
        {
            await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(new CapturedSpeechAudio(
                SpeechAudioMimeType.Pcm,
                [Array.Empty<byte>()])));
            speechClient.EnqueueTranscript(string.Empty);
        }

        var result = await runner.SpeakAndListenAsync(
            SelectionDialogPrompt.FromText("prompt"),
            "de",
            new SelectionDialogInteractionSettings(1.0d, 0, false, 85, 65),
            noInputTimeoutMs: 500,
            CancellationToken.None);

        Assert.Null(result.Transcript);
        Assert.Null(result.FailureCode);
        Assert.Null(result.Recoverable);
        Assert.True(result.RecognizedAsEmpty);
        Assert.Equal(3, speechClient.SpeechToTextCallCount);
    }

    [Fact]
    public async Task SpeakAndListenAsync_WhenBargeInDisabledWithNonModeAwareCapture_UsesBaseListeningPath()
    {
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);
        var captureClient = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        var recognitionClient = new Mock<ISpeechRecognitionClient>(MockBehavior.Strict);
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var runner = CreateSpeechTurnRunner(captureClient.Object, recognitionClient.Object, textToSpeechPlayer);

        captureClient
            .Setup(client => client.RecordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance);
        recognitionClient
            .Setup(client => client.SpeechToTextAsync(utterance, "de", It.IsAny<RecognitionProfile>(), It.IsAny<CancellationToken>(), It.IsAny<SpeechKeyterms?>()))
            .ReturnsAsync(new SpeechTranscript("zwei", 96));

        var result = await runner.SpeakAndListenAsync(
            SelectionDialogPrompt.FromSequence("intro", ["item-1", "item-2"]),
            "de",
            new SelectionDialogInteractionSettings(1.0d, 0, false, 85, 65),
            noInputTimeoutMs: 500,
            CancellationToken.None);

        Assert.Equal("zwei", result.Transcript);
        Assert.Equal(96, result.SttConfidencePercent);
        Assert.Null(result.FailureCode);
        Assert.Null(result.Recoverable);
        captureClient.Verify(client => client.RecordAsync(It.IsAny<CancellationToken>()), Times.Once);
        recognitionClient.Verify(client => client.SpeechToTextAsync(utterance, "de", It.IsAny<RecognitionProfile>(), It.IsAny<CancellationToken>(), It.IsAny<SpeechKeyterms?>()), Times.Once);
        captureClient.VerifyNoOtherCalls();
        recognitionClient.VerifyNoOtherCalls();
        Assert.Equal(
            ["intro", "item-1", "item-2"],
            textToSpeechPlayer.Calls.Select(static call => call.Text).ToArray());
    }

    [Fact]
    public void Constructor_WhenModeAwareCaptureClientIsNull_Throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new SelectionDialogSpeechTurnRunner(
                new SynchronousClientMessageBus(),
                Mock.Of<ISpeechCaptureClient>(),
                null!,
                new SpeechRecognitionWorker(Mock.Of<ISpeechRecognitionClient>()),
                new RecordingTextToSpeechPlayer(),
                NullLogger<SelectionDialogSpeechTurnRunner>.Instance));

        Assert.Equal("modeAwareSpeechCaptureClient", exception.ParamName);
    }

    [Fact]
    public async Task ListenAsync_WhenShortConfirmationModeRequested_RecordsViaModeAwareClientWithShortConfirmation()
    {
        // Die Bestätigungsphase muss ShortConfirmation an den Aufnahmeclient weiterreichen.
        var speechClient = new ScriptedSpeechInputClient();
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var runner = CreateSpeechTurnRunner(speechClient, textToSpeechPlayer);
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(EmptyAudio()));
        speechClient.EnqueueTranscript("ja");

        var result = await runner.ListenAsync(
            "de",
            noInputTimeoutMs: 500,
            CancellationToken.None,
            keyterms: null,
            SpeechCaptureMode.ShortConfirmation);

        Assert.Equal("ja", result.Transcript);
        Assert.Equal([SpeechCaptureMode.ShortConfirmation], speechClient.RecordModes);
    }

    [Fact]
    public async Task ListenAsync_WhenDefaultMode_RoutesListeningThroughModeAwareClientWithOnsetAwareTimeout()
    {
        // Listening erhält die konfigurierte Anfangsstillefrist in der Aufnahmeschicht, keine Zeitgrenze um die ganze Aufnahme.
        var speechClient = new ScriptedSpeechInputClient();
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var runner = CreateSpeechTurnRunner(speechClient, textToSpeechPlayer);
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(EmptyAudio()));
        speechClient.EnqueueTranscript("eins");

        var result = await runner.ListenAsync("de", noInputTimeoutMs: 500, CancellationToken.None);

        Assert.Equal("eins", result.Transcript);
        Assert.Equal([SpeechCaptureMode.Listening], speechClient.RecordModes);
        Assert.Equal([(TimeSpan?)TimeSpan.FromMilliseconds(500)], speechClient.InitialSilenceTimeouts);
    }

    [Fact]
    public async Task ListenAsync_WhenConfirmationUtteranceOutlastsNoInputDeadline_IsNotCancelledAsNoInput()
    {
        // Die simulierte laufende Aufnahme überdauert die Anfangsstillefrist.
        // Eine zusätzliche Zeitgrenze um den ganzen Versuch würde sie fälschlich vor dem Ergebnis abbrechen.
        var speechClient = new ScriptedSpeechInputClient();
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var runner = CreateSpeechTurnRunner(speechClient, textToSpeechPlayer);
        var releaseCapture = new TaskCompletionSource();
        await speechClient.EnqueueRecordStepAsync(async ct =>
        {
            await releaseCapture.Task.WaitAsync(ct).ConfigureAwait(false);
            return EmptyAudio();
        });
        speechClient.EnqueueTranscript("ja");

        var listenTask = runner.ListenAsync(
            "de",
            noInputTimeoutMs: 100,
            CancellationToken.None,
            keyterms: null,
            SpeechCaptureMode.ShortConfirmation);

        await Task.Delay(400);
        releaseCapture.SetResult();
        var result = await listenTask;

        Assert.Equal("ja", result.Transcript);
        Assert.Equal([SpeechCaptureMode.ShortConfirmation], speechClient.RecordModes);
        Assert.Equal([(TimeSpan?)TimeSpan.FromMilliseconds(100)], speechClient.InitialSilenceTimeouts);
    }

    [Fact]
    public async Task SpeakAndListenAsync_WhenBargeInUtteranceOutlastsNoInputDeadline_IsNotCancelledAsNoInput()
    {
        // Auch bei Dazwischensprechen muss eine bereits laufende Äußerung die Anfangsstillefrist überdauern können; diese wird an die Aufnahmeschicht weitergegeben.
        var speechClient = new ScriptedSpeechInputClient();
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        var runner = CreateSpeechTurnRunner(speechClient, textToSpeechPlayer);
        var releaseCapture = new TaskCompletionSource();
        await speechClient.EnqueueRecordStepAsync(async ct =>
        {
            await releaseCapture.Task.WaitAsync(ct).ConfigureAwait(false);
            return EmptyAudio();
        });
        speechClient.EnqueueTranscript("eins");

        var listenTask = runner.SpeakAndListenAsync(
            SelectionDialogPrompt.FromText("prompt"),
            "de",
            new SelectionDialogInteractionSettings(1.0d, 0, true, 85, 65),
            noInputTimeoutMs: 100,
            CancellationToken.None);

        await Task.Delay(400);
        releaseCapture.SetResult();
        var result = await listenTask;

        Assert.Equal("eins", result.Transcript);
        Assert.Equal([SpeechCaptureMode.PromptInterruption], speechClient.RecordModes);
        Assert.Equal([(TimeSpan?)TimeSpan.FromMilliseconds(100)], speechClient.InitialSilenceTimeouts);
    }

    [Fact]
    public async Task ListenAsync_WhenSpeechRecognitionQueueFull_ReturnsRecoverableFailureCode()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var slowClient = new SlowSpeechRecognitionClient();
        await using var worker = new SpeechRecognitionWorker(
            slowClient,
            backoff: new RecordingBackoffPolicy(maxRetries: 1),
            maxQueueDepth: 1);
        var runner = CreateSpeechTurnRunner(speechClient, speechClient, worker, new RecordingTextToSpeechPlayer());

        var firstTask = worker.EnqueueAsync(EmptyAudio(), "de", CancellationToken.None);
        await slowClient.WaitForFirstCallAsync(TimeSpan.FromSeconds(2));
        var secondTask = worker.EnqueueAsync(EmptyAudio(), "de", CancellationToken.None);
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(EmptyAudio()));

        var result = await runner.ListenAsync("de", noInputTimeoutMs: 500, CancellationToken.None);

        Assert.Null(result.Transcript);
        Assert.Null(result.SttConfidencePercent);
        Assert.Equal(SpeechRecognitionQueueFullException.CodeValue, result.FailureCode);
        Assert.True(result.Recoverable);

        slowClient.ReleaseAll();
        await firstTask;
        await secondTask;
    }

    [Fact]
    public async Task ListenAsync_WhenSpeechRecognitionBackoffExhausted_ReturnsRecoverableFailureCode()
    {
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(EmptyAudio()));
        await using var worker = new SpeechRecognitionWorker(
            new AlwaysFailingSpeechRecognitionClient(),
            backoff: new RecordingBackoffPolicy(maxRetries: 1),
            maxQueueDepth: 1);
        var runner = CreateSpeechTurnRunner(speechClient, speechClient, worker, new RecordingTextToSpeechPlayer());

        var result = await runner.ListenAsync("de", noInputTimeoutMs: 500, CancellationToken.None);

        Assert.Null(result.Transcript);
        Assert.Null(result.SttConfidencePercent);
        Assert.Equal(SpeechRecognitionBackoffExhaustedException.CodeValue, result.FailureCode);
        Assert.True(result.Recoverable);
    }

    [Fact]
    public async Task SpeakAndListenAsync_WhenBargeInSpeechRecognitionBackoffExhausted_ReturnsRecoverableFailureCode()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        await speechClient.EnqueueRecordStepAsync(async _ =>
        {
            await textToSpeechPlayer.WaitForFirstCallAsync(TimeSpan.FromSeconds(1));
            return EmptyAudio();
        });
        await using var worker = new SpeechRecognitionWorker(
            new AlwaysFailingSpeechRecognitionClient(),
            backoff: new RecordingBackoffPolicy(maxRetries: 1),
            maxQueueDepth: 1);
        var runner = CreateSpeechTurnRunner(speechClient, speechClient, worker, textToSpeechPlayer);

        var result = await runner.SpeakAndListenAsync(
            SelectionDialogPrompt.FromText("prompt"),
            "de",
            new SelectionDialogInteractionSettings(1.0d, 0, true, 85, 65),
            noInputTimeoutMs: 500,
            CancellationToken.None);

        Assert.Null(result.Transcript);
        Assert.Null(result.SttConfidencePercent);
        Assert.Equal(SpeechRecognitionBackoffExhaustedException.CodeValue, result.FailureCode);
        Assert.True(result.Recoverable);
        Assert.Equal([SpeechCaptureMode.PromptInterruption], speechClient.RecordModes);
    }

    [Fact]
    public async Task ListenAsync_WhenCaptureIsCancelledWithoutCallerCancellation_ReturnsRecoverableInterruptedFailure()
    {
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(
            _ => Task.FromException<CapturedSpeechAudio>(new OperationCanceledException("Capture was interrupted.")));
        var runner = CreateSpeechTurnRunner(speechClient, new RecordingTextToSpeechPlayer());

        var result = await runner.ListenAsync("de", noInputTimeoutMs: 500, CancellationToken.None);

        Assert.Null(result.Transcript);
        Assert.Null(result.SttConfidencePercent);
        Assert.Equal("SPEECH_CAPTURE_INTERRUPTED", result.FailureCode);
        Assert.True(result.Recoverable);
        Assert.False(result.RecognizedAsEmpty);
    }

    [Fact]
    public async Task ListenAsync_WhenCallerCancels_PropagatesCancellation()
    {
        using var callerCts = new CancellationTokenSource();
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(_ =>
        {
            callerCts.Cancel();
            return Task.FromException<CapturedSpeechAudio>(new OperationCanceledException(callerCts.Token));
        });
        var runner = CreateSpeechTurnRunner(speechClient, new RecordingTextToSpeechPlayer());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => runner.ListenAsync("de", noInputTimeoutMs: 500, callerCts.Token));
    }

    [Fact]
    public async Task SpeakAndListenAsync_WhenBargeInCaptureIsCancelledWithoutCallerCancellation_ReturnsRecoverableInterruptedFailure()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();
        await speechClient.EnqueueRecordStepAsync(async _ =>
        {
            await textToSpeechPlayer.WaitForFirstCallAsync(TimeSpan.FromSeconds(1));
            throw new OperationCanceledException("Capture was interrupted.");
        });
        var runner = CreateSpeechTurnRunner(speechClient, textToSpeechPlayer);

        var result = await runner.SpeakAndListenAsync(
            SelectionDialogPrompt.FromText("prompt"),
            "de",
            new SelectionDialogInteractionSettings(1.0d, 0, true, 85, 65),
            noInputTimeoutMs: 500,
            CancellationToken.None);

        Assert.Null(result.Transcript);
        Assert.Null(result.SttConfidencePercent);
        Assert.Equal("SPEECH_CAPTURE_INTERRUPTED", result.FailureCode);
        Assert.True(result.Recoverable);
        Assert.False(result.RecognizedAsEmpty);
        Assert.Equal([SpeechCaptureMode.PromptInterruption], speechClient.RecordModes);
        Assert.Single(textToSpeechPlayer.Calls);
    }

    [Fact]
    public async Task SpeakAndListenAsync_WhenCallerCancelsBargeInCapture_ObservesPromptAndPropagatesCancellation()
    {
        using var callerCts = new CancellationTokenSource();
        var speechClient = new ScriptedSpeechInputClient();
        var promptStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var promptCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var textToSpeechPlayer = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeechPlayer
            .Setup(player => player.SpeakAsync("prompt", "de", 1.0d, SpokenAnnouncementKind.Prompt, It.IsAny<CancellationToken>()))
            .Callback(() => promptStarted.TrySetResult())
            .Returns(promptCompletion.Task);
        await speechClient.EnqueueRecordStepAsync(async _ =>
        {
            await promptStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            callerCts.Cancel();
            throw new OperationCanceledException(callerCts.Token);
        });
        var runner = CreateSpeechTurnRunner(speechClient, textToSpeechPlayer.Object);

        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runner.SpeakAndListenAsync(
                    SelectionDialogPrompt.FromText("prompt"),
                    "de",
                    new SelectionDialogInteractionSettings(1.0d, 0, true, 85, 65),
                    noInputTimeoutMs: 500,
                    callerCts.Token)
                .WaitAsync(TimeSpan.FromSeconds(1)));
        }
        finally
        {
            promptCompletion.TrySetException(new InvalidOperationException("Synthetic late prompt fault."));
        }

        textToSpeechPlayer.Verify(
            player => player.SpeakAsync("prompt", "de", 1.0d, SpokenAnnouncementKind.Prompt, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static SelectionDialogSpeechTurnRunner CreateSpeechTurnRunner(
        ScriptedSpeechInputClient speechClient,
        ITextToSpeechPlayer textToSpeechPlayer)
    {
        return CreateSpeechTurnRunner(speechClient, speechClient, speechClient, textToSpeechPlayer);
    }

    private static SelectionDialogSpeechTurnRunner CreateSpeechTurnRunner(
        ISpeechCaptureClient captureClient,
        ISpeechRecognitionClient recognitionClient,
        ITextToSpeechPlayer textToSpeechPlayer)
    {
        return CreateSpeechTurnRunner(
            captureClient,
            ModeAwareSpeechCapture.Required(captureClient),
            recognitionClient,
            textToSpeechPlayer);
    }

    private static SelectionDialogSpeechTurnRunner CreateSpeechTurnRunner(
        ISpeechCaptureClient captureClient,
        IModeAwareSpeechCaptureClient modeAwareSpeechCaptureClient,
        ISpeechRecognitionClient recognitionClient,
        ITextToSpeechPlayer textToSpeechPlayer)
    {
        return CreateSpeechTurnRunner(
            captureClient,
            modeAwareSpeechCaptureClient,
            new SpeechRecognitionWorker(recognitionClient),
            textToSpeechPlayer);
    }

    private static SelectionDialogSpeechTurnRunner CreateSpeechTurnRunner(
        ISpeechCaptureClient captureClient,
        IModeAwareSpeechCaptureClient modeAwareSpeechCaptureClient,
        SpeechRecognitionWorker speechRecognitionWorker,
        ITextToSpeechPlayer textToSpeechPlayer)
    {
        return new SelectionDialogSpeechTurnRunner(
            new SynchronousClientMessageBus(),
            captureClient,
            modeAwareSpeechCaptureClient,
            speechRecognitionWorker,
            textToSpeechPlayer,
            NullLogger<SelectionDialogSpeechTurnRunner>.Instance);
    }

    private static CapturedSpeechAudio EmptyAudio() =>
        new(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);
}
