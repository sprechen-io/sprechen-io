using Grpc.Core;
using Sprechen.Proto;
using Moq;
using Sprechen.Proto.Transport;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using SpeechGrpcClient = Sprechen.Proto.Speech.SpeechClient;
using ProtoSpeechTranscript = Sprechen.Proto.SpeechTranscript;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechRecognitionClientTests
{
    [Fact]
    public async Task SpeechToTextAsync_WhenGrpcFails_ThrowsSpeechErrorExceptionWithMappedCode()
    {
        var trailers = SprechenGrpcErrorMetadata.CreateTrailers(SprechenErrorCode.ProviderFailure);
        var rpcException = new RpcException(new Status(StatusCode.Internal, "down"), trailers);
        Metadata? capturedHeaders = null;

        var requestWriter = new CollectingClientStreamWriter<AudioChunk>();
        var call = new AsyncClientStreamingCall<AudioChunk, ProtoSpeechTranscript>(
            requestWriter,
            Task.FromException<ProtoSpeechTranscript>(rpcException),
            Task.FromResult(new Metadata()),
            () => rpcException.Status,
            () => rpcException.Trailers,
            () => { });

        var grpcClient = new Mock<SpeechGrpcClient>(MockBehavior.Strict, new NoOpCallInvoker());
        grpcClient
            .Setup(client => client.SpeechToText(
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Callback<Metadata?, DateTime?, CancellationToken>((headers, _, _) => capturedHeaders = headers)
            .Returns(call);

        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);
        var trace = new BrowserDebugConsoleSpeechClientTrace(
            browserDebugConsole,
            new RecordingLogger<BrowserDebugConsoleSpeechClientTrace>());

        var sut = new SpeechRecognitionClient(grpcClient.Object, trace);
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2, 3 }]);

        var exception = await Assert.ThrowsAsync<SpeechErrorException>(() => sut.SpeechToTextAsync(utterance, "de", Sprechen.Speech.App.Contracts.Services.RecognitionProfile.Conversational, CancellationToken.None));

        Assert.Equal(SpeechErrorCode.ProviderFailure, exception.ErrorCode);
        Assert.Equal("down", exception.Message);
        Assert.True(requestWriter.Completed);
        Assert.Single(requestWriter.Messages);
        Assert.Equal("de", requestWriter.Messages[0].LanguageCode);
        Assert.True(SprechenGrpcCorrelationMetadata.TryGetCorrelationId(capturedHeaders, out var correlationId));
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
        Assert.Contains(
            browserDebugConsole.Entries,
            entry => entry.Contains("speech-recognition: cid=", StringComparison.Ordinal));
        Assert.Contains(
            browserDebugConsole.Entries,
            entry => entry.Contains("speech-to-text failed errorCode=ProviderFailure statusCode=Internal", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SpeechToTextAsync_WhenTranscriptIsMissing_ReturnsEmptyTranscript()
    {
        var requestWriter = new CollectingClientStreamWriter<AudioChunk>();
        var call = new AsyncClientStreamingCall<AudioChunk, ProtoSpeechTranscript>(
            requestWriter,
            Task.FromResult(new ProtoSpeechTranscript()),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });

        var grpcClient = new Mock<SpeechGrpcClient>(MockBehavior.Strict, new NoOpCallInvoker());
        grpcClient
            .Setup(client => client.SpeechToText(
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(call);

        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);
        var trace = new BrowserDebugConsoleSpeechClientTrace(
            browserDebugConsole,
            new RecordingLogger<BrowserDebugConsoleSpeechClientTrace>());

        var sut = new SpeechRecognitionClient(grpcClient.Object, trace);
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2, 3 }]);

        var transcript = await sut.SpeechToTextAsync(utterance, "de", Sprechen.Speech.App.Contracts.Services.RecognitionProfile.Conversational, CancellationToken.None);

        Assert.Equal(string.Empty, transcript.Text);
        Assert.Null(transcript.ConfidencePercent);
        Assert.True(requestWriter.Completed);
        Assert.Contains(
            browserDebugConsole.Entries,
            entry => entry.Contains("speech-recognition: cid=", StringComparison.Ordinal)
                && entry.Contains("speech-to-text result hasText=False confidence=<null>", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SpeechToTextAsync_WhenTranscriptIsReturned_DoesNotWriteItsContentToAnyTraceSink()
    {
        const string PrivateTranscript = "DE89 3704 0044 0532 0130 00";
        var requestWriter = new CollectingClientStreamWriter<AudioChunk>();
        var call = new AsyncClientStreamingCall<AudioChunk, ProtoSpeechTranscript>(
            requestWriter,
            Task.FromResult(new ProtoSpeechTranscript { Text = PrivateTranscript, Confidence = 97 }),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });

        var grpcClient = new Mock<SpeechGrpcClient>(MockBehavior.Strict, new NoOpCallInvoker());
        grpcClient
            .Setup(client => client.SpeechToText(
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(call);

        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);
        var logger = new RecordingLogger<BrowserDebugConsoleSpeechClientTrace>();
        var trace = new BrowserDebugConsoleSpeechClientTrace(browserDebugConsole, logger);
        var sut = new SpeechRecognitionClient(grpcClient.Object, trace);

        var transcript = await sut.SpeechToTextAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2, 3 }]),
            "de",
            Sprechen.Speech.App.Contracts.Services.RecognitionProfile.Conversational,
            CancellationToken.None);

        Assert.Equal(PrivateTranscript, transcript.Text);
        Assert.Equal(97, transcript.ConfidencePercent);
        Assert.DoesNotContain(browserDebugConsole.Entries, entry => entry.Contains(PrivateTranscript, StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains(PrivateTranscript, StringComparison.Ordinal));
        Assert.Contains(
            browserDebugConsole.Entries,
            entry => entry.Contains("speech-to-text result hasText=True confidence=97", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SpeechToTextAsync_WhenUtteranceCarriesTurnId_SendsItAsCorrelationHeader()
    {
        // Die TurnId als Korrelationskennung übernehmen, damit Aufnahmebericht und Erkennungsanfrage zugeordnet werden können.
        Metadata? capturedHeaders = null;
        var requestWriter = new CollectingClientStreamWriter<AudioChunk>();
        var call = new AsyncClientStreamingCall<AudioChunk, ProtoSpeechTranscript>(
            requestWriter,
            Task.FromResult(new ProtoSpeechTranscript()),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });

        var grpcClient = new Mock<SpeechGrpcClient>(MockBehavior.Strict, new NoOpCallInvoker());
        grpcClient
            .Setup(client => client.SpeechToText(
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Callback<Metadata?, DateTime?, CancellationToken>((headers, _, _) => capturedHeaders = headers)
            .Returns(call);

        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);
        var trace = new BrowserDebugConsoleSpeechClientTrace(
            browserDebugConsole,
            new RecordingLogger<BrowserDebugConsoleSpeechClientTrace>());

        var sut = new SpeechRecognitionClient(grpcClient.Object, trace);
        var turnId = Guid.NewGuid().ToString("N");
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2, 3 }], turnId);

        await sut.SpeechToTextAsync(utterance, "de", Sprechen.Speech.App.Contracts.Services.RecognitionProfile.Conversational, CancellationToken.None);

        Assert.True(SprechenGrpcCorrelationMetadata.TryGetCorrelationId(capturedHeaders, out var correlationId));
        Assert.Equal(turnId, correlationId);
    }

    [Fact]
    public async Task SpeechToTextAsync_WhenUtteranceCarriesNoTurnId_MintsTheCorrelationIdAsBefore()
    {
        Metadata? capturedHeaders = null;
        var requestWriter = new CollectingClientStreamWriter<AudioChunk>();
        var call = new AsyncClientStreamingCall<AudioChunk, ProtoSpeechTranscript>(
            requestWriter,
            Task.FromResult(new ProtoSpeechTranscript()),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });

        var grpcClient = new Mock<SpeechGrpcClient>(MockBehavior.Strict, new NoOpCallInvoker());
        grpcClient
            .Setup(client => client.SpeechToText(
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Callback<Metadata?, DateTime?, CancellationToken>((headers, _, _) => capturedHeaders = headers)
            .Returns(call);

        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);
        var trace = new BrowserDebugConsoleSpeechClientTrace(
            browserDebugConsole,
            new RecordingLogger<BrowserDebugConsoleSpeechClientTrace>());

        var sut = new SpeechRecognitionClient(grpcClient.Object, trace);
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2, 3 }]);

        await sut.SpeechToTextAsync(utterance, "de", Sprechen.Speech.App.Contracts.Services.RecognitionProfile.Conversational, CancellationToken.None);

        Assert.True(SprechenGrpcCorrelationMetadata.TryGetCorrelationId(capturedHeaders, out var correlationId));
        Assert.True(Guid.TryParseExact(correlationId, "N", out _), $"expected a new correlation id, got: {correlationId}");
    }

    [Fact]
    public async Task SpeechToTextAsync_WhenSessionIsDiagnostic_MarksOnlyTheFirstChunk()
    {
        var requestWriter = new CollectingClientStreamWriter<AudioChunk>();
        var call = new AsyncClientStreamingCall<AudioChunk, ProtoSpeechTranscript>(
            requestWriter,
            Task.FromResult(new ProtoSpeechTranscript()),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });

        var grpcClient = new Mock<SpeechGrpcClient>(MockBehavior.Strict, new NoOpCallInvoker());
        grpcClient
            .Setup(client => client.SpeechToText(
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(call);

        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);
        var trace = new BrowserDebugConsoleSpeechClientTrace(
            browserDebugConsole,
            new RecordingLogger<BrowserDebugConsoleSpeechClientTrace>());

        var operatingMode = new FixedDiagnosticOperatingMode { IsDiagnostic = true };
        var sut = new SpeechRecognitionClient(grpcClient.Object, trace, diagnosticOperatingMode: operatingMode);
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 }]);

        await sut.SpeechToTextAsync(utterance, "de", Sprechen.Speech.App.Contracts.Services.RecognitionProfile.Conversational, CancellationToken.None);

        Assert.Equal(2, requestWriter.Messages.Count);
        Assert.True(requestWriter.Messages[0].DiagnosticSession);
        Assert.False(requestWriter.Messages[1].DiagnosticSession);
    }

    [Fact]
    public async Task SpeechToTextAsync_WithoutDiagnosticMode_LeavesEveryChunkUnmarked()
    {
        var requestWriter = new CollectingClientStreamWriter<AudioChunk>();
        var call = new AsyncClientStreamingCall<AudioChunk, ProtoSpeechTranscript>(
            requestWriter,
            Task.FromResult(new ProtoSpeechTranscript()),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });

        var grpcClient = new Mock<SpeechGrpcClient>(MockBehavior.Strict, new NoOpCallInvoker());
        grpcClient
            .Setup(client => client.SpeechToText(
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(call);

        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);
        var trace = new BrowserDebugConsoleSpeechClientTrace(
            browserDebugConsole,
            new RecordingLogger<BrowserDebugConsoleSpeechClientTrace>());

        var sut = new SpeechRecognitionClient(grpcClient.Object, trace);
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 }]);

        await sut.SpeechToTextAsync(utterance, "de", Sprechen.Speech.App.Contracts.Services.RecognitionProfile.Conversational, CancellationToken.None);

        Assert.Equal(2, requestWriter.Messages.Count);
        Assert.All(requestWriter.Messages, message => Assert.False(message.DiagnosticSession));
    }
}
