using Grpc.Core;
using Grpc.Core.Testing;
using Moq;
using Sprechen.Proto.Transport;
using Sprechen.Service.Grpc;
using Xunit;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Tests.Grpc.Endpoints;

public sealed class SpeechToTextEndpointTests
{
    [Fact]
    public async Task HandleAsync_WhenProviderReturnsTranscriptMapsResponse()
    {
        var language = new Mock<ISpeechToTextService>(MockBehavior.Strict);
        language.Setup(service => service.TranscribeStreamingAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new SpeechTranscript("hallo", 82));
        var endpoint = CreateEndpoint(language.Object);

        var response = await endpoint.HandleAsync(
            GrpcEndpointTestFactory.CreatePcmRequestStream(),
            GrpcEndpointTestFactory.CreateContext("SpeechToText"));

        Assert.Equal("hallo", response.Text);
        Assert.True(response.HasConfidence);
        Assert.Equal(82, response.Confidence);
        language.VerifyAll();
    }

    [Theory]
    [InlineData(SprechenProto.AudioMimeType.AudioMp3)]
    [InlineData(SprechenProto.AudioMimeType.AudioWav)]
    [InlineData(SprechenProto.AudioMimeType.AudioOggOpus)]
    [InlineData(SprechenProto.AudioMimeType.AudioFlac)]
    [InlineData(SprechenProto.AudioMimeType.AudioWebmOpus)]
    public async Task HandleAsync_WhenMimeIsNotPcm_ReturnsStableUnsupportedCodeBeforeReachingProvider(
        SprechenProto.AudioMimeType protoMime)
    {
        // Der gRPC-Endpunkt muss die Eingabe vor einem Aufruf des Sprachdienstes ablehnen.
        var language = new Mock<ISpeechToTextService>(MockBehavior.Strict);
        var endpoint = CreateEndpoint(language.Object);

        var ex = await Assert.ThrowsAsync<RpcException>(async () =>
            await endpoint.HandleAsync(
                CreateRequestStream(protoMime, "de"),
                GrpcEndpointTestFactory.CreateContext("SpeechToText")));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        Assert.True(SprechenGrpcErrorMetadata.TryGetErrorCode(ex, out var errorCode));
        Assert.Equal(SprechenProto.SprechenErrorCode.AudioInputMimeTypeUnsupported, errorCode);
        language.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenProviderThrowsInvalidOperation_MapsToProviderFailure()
    {
        var language = new Mock<ISpeechToTextService>(MockBehavior.Strict);
        language.Setup(service => service.TranscribeStreamingAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<string?>()))
            .ThrowsAsync(new InvalidOperationException(
                "Deepgram live transcription failed."));
        var endpoint = CreateEndpoint(language.Object);

        var ex = await Assert.ThrowsAsync<RpcException>(async () =>
            await endpoint.HandleAsync(
                GrpcEndpointTestFactory.CreatePcmRequestStream(),
                GrpcEndpointTestFactory.CreateContext("SpeechToText")));

        Assert.True(SprechenGrpcErrorMetadata.TryGetErrorCode(ex, out var errorCode));
        Assert.Equal(SprechenProto.SprechenErrorCode.ProviderFailure, errorCode);
    }

    [Fact]
    public async Task HandleAsync_WhenRequestCarriesCorrelationId_ForwardsItToTheSpeechService()
    {
        // Die Korrelationskennung muss den Sprachdienst erreichen, um dessen Diagnose zuordnen zu können.
        var language = new Mock<ISpeechToTextService>(MockBehavior.Strict);
        language.Setup(service => service.TranscribeStreamingAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                "turn-abc123"))
            .ReturnsAsync(new SpeechTranscript("hallo", 82));
        var endpoint = CreateEndpoint(language.Object);
        var context = TestServerCallContext.Create(
            "SpeechToText",
            null,
            DateTime.UtcNow.AddMinutes(1),
            new Metadata { { SprechenGrpcCorrelationMetadata.CorrelationIdHeaderName, "turn-abc123" } },
            CancellationToken.None,
            "127.0.0.1",
            null,
            null,
            _ => Task.CompletedTask,
            static () => new WriteOptions(),
            static _ => { });

        var response = await endpoint.HandleAsync(GrpcEndpointTestFactory.CreatePcmRequestStream(), context);

        Assert.Equal("hallo", response.Text);
        language.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenFirstChunkMarksDiagnosticSession_ForwardsTheMarkToTheSpeechService()
    {
        // Die Diagnosekennzeichnung muss aus dem ersten Audiosegment bis zum Sprachdienst gelangen.
        var language = new Mock<ISpeechToTextService>(MockBehavior.Strict);
        language.Setup(service => service.TranscribeStreamingAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<string?>(),
                true))
            .ReturnsAsync(new SpeechTranscript("hallo", 82));
        var endpoint = CreateEndpoint(language.Object);

        await endpoint.HandleAsync(
            CreateRequestStream(SprechenProto.AudioMimeType.AudioPcm, "de", diagnosticSession: true),
            GrpcEndpointTestFactory.CreateContext("SpeechToText"));

        language.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenNoChunkMarksDiagnosticSession_ForwardsNotDiagnosticToTheSpeechService()
    {
        var language = new Mock<ISpeechToTextService>(MockBehavior.Strict);
        language.Setup(service => service.TranscribeStreamingAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<string?>(),
                false))
            .ReturnsAsync(new SpeechTranscript("hallo", 82));
        var endpoint = CreateEndpoint(language.Object);

        await endpoint.HandleAsync(
            GrpcEndpointTestFactory.CreatePcmRequestStream(),
            GrpcEndpointTestFactory.CreateContext("SpeechToText"));

        language.VerifyAll();
    }

    private static SpeechToTextEndpoint CreateEndpoint(ISpeechToTextService speechToText)
    {
        var supportedLanguages = GrpcEndpointTestFactory.CreateSupportedLanguages();
        return new SpeechToTextEndpoint(
            GrpcEndpointTestFactory.CreateTranscribeSpeechDispatcher(speechToText),
            GrpcEndpointTestFactory.CreateRequestValidator(supportedLanguages.Object),
            GrpcEndpointTestFactory.CreateDiagnosticsEmitter(),
            GrpcEndpointTestFactory.CreateExceptionMapper(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SpeechGrpcService>.Instance);
    }

    private static SequenceAsyncStreamReader<SprechenProto.AudioChunk> CreateRequestStream(
        SprechenProto.AudioMimeType mime,
        string languageCode,
        bool diagnosticSession = false)
    {
        var chunk = new SprechenProto.AudioChunk
        {
            Data = Google.Protobuf.ByteString.CopyFrom(new byte[] { 0x1, 0x2 }),
            MimeType = mime,
            LanguageCode = languageCode,
        };

        if (diagnosticSession)
        {
            chunk.DiagnosticSession = true;
        }

        return new SequenceAsyncStreamReader<SprechenProto.AudioChunk>(new[] { chunk });
    }
}
