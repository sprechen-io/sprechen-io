using Grpc.Core;
using Moq;
using Sprechen.Proto.Transport;
using Sprechen.Service.Grpc;
using Sprechen.Service.Options;
using Sprechen.Service.Tests.Grpc;
using Xunit;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Tests.Grpc.Endpoints;

public sealed class TextToSpeechToStreamEndpointTests
{
    [Fact]
    public async Task HandleAsync_WhenProviderCompletesWritesAudio()
    {
        var language = new Mock<ITextToSpeechService>(MockBehavior.Strict);
        var audio = new byte[] { 0x1, 0x2 };
        language.Setup(service => service.SynthesizeAsync(
                "hallo",
                AudioMimeType.Pcm,
                1.0f,
                "de",
                TextToSpeechPolitenessLevel.Unspecified,
                It.IsAny<CancellationToken>()))
            .Returns(SingleChunk(audio));
        var endpoint = CreateEndpoint(language.Object);
        var writer = new RecordingServerStreamWriter<SprechenProto.AudioResponse>();

        await endpoint.HandleAsync(CreateRequest(), writer, GrpcEndpointTestFactory.CreateContext("TextToSpeechToStream"));

        var response = Assert.Single(writer.Items);
        Assert.Equal(SprechenProto.AudioMimeType.AudioPcm, response.MimeType);
        Assert.Equal(Google.Protobuf.ByteString.CopyFrom(audio), response.Data);
        language.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenClientCancelsDoesNotThrow()
    {
        var language = new Mock<ITextToSpeechService>(MockBehavior.Strict);
        var audio = new byte[5000];
        language.Setup(service => service.SynthesizeAsync(
                "hallo",
                AudioMimeType.Pcm,
                1.0f,
                "de",
                TextToSpeechPolitenessLevel.Unspecified,
                It.IsAny<CancellationToken>()))
            .Returns(SingleChunk(audio));
        using var cts = new CancellationTokenSource();
        var endpoint = CreateEndpoint(language.Object);
        var writer = new CancellingServerStreamWriter<SprechenProto.AudioResponse>(cts, cancelAfterWrites: 1);

        await endpoint.HandleAsync(
            CreateRequest(),
            writer,
            GrpcEndpointTestFactory.CreateContext("TextToSpeechToStream", cts.Token));

        Assert.Single(writer.Items);
        language.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenProviderThrowsRpcExceptionMapsProviderFailure()
    {
        var language = new Mock<ITextToSpeechService>(MockBehavior.Strict);
        language.Setup(service => service.SynthesizeAsync(
                "hallo",
                AudioMimeType.Pcm,
                1.0f,
                "de",
                TextToSpeechPolitenessLevel.Unspecified,
                It.IsAny<CancellationToken>()))
            .Returns(ThrowingSynthesis(new RpcException(new Status(StatusCode.NotFound, "provider missing"))));
        var endpoint = CreateEndpoint(language.Object);

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            endpoint.HandleAsync(
                CreateRequest(),
                new RecordingServerStreamWriter<SprechenProto.AudioResponse>(),
                GrpcEndpointTestFactory.CreateContext("TextToSpeechToStream")));

        Assert.Equal(StatusCode.Internal, ex.StatusCode);
        Assert.True(SprechenGrpcErrorMetadata.TryGetErrorCode(ex, out var errorCode));
        Assert.Equal(SprechenProto.SprechenErrorCode.ProviderFailure, errorCode);
        language.VerifyAll();
    }

    private static TextToSpeechToStreamEndpoint CreateEndpoint(ITextToSpeechService language)
    {
        var supportedLanguages = GrpcEndpointTestFactory.CreateSupportedLanguages();
        return new TextToSpeechToStreamEndpoint(
            GrpcEndpointTestFactory.CreateSynthesizeSpeechDispatcher(language),
            GrpcEndpointTestFactory.CreateRequestValidator(supportedLanguages.Object),
            GrpcEndpointTestFactory.CreateExceptionMapper(),
            Microsoft.Extensions.Options.Options.Create(new SpeechProviderOptions()),
            new TextToSpeechAudioStreamWriter(
                Microsoft.Extensions.Options.Options.Create(new AudioPacingOptions()),
                new SpeechSynthesisDiagnosticsEmitter(
                    Microsoft.Extensions.Options.Options.Create(new SpeechProviderOptions()),
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<SpeechGrpcService>.Instance),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<SpeechGrpcService>.Instance),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SpeechGrpcService>.Instance);
    }

    private static SprechenProto.TextRequest CreateRequest()
    {
        return new SprechenProto.TextRequest
        {
            Text = "hallo",
            LanguageCode = "de",
            DesiredMimeType = SprechenProto.AudioMimeType.AudioPcm
        };
    }

    private static async IAsyncEnumerable<AudioSynthesisChunk> SingleChunk(byte[] data)
    {
        await Task.Yield();
        yield return new AudioSynthesisChunk(data, AudioMimeType.Pcm, sampleRateHz: 24_000);
    }

    private static async IAsyncEnumerable<AudioSynthesisChunk> ThrowingSynthesis(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        await Task.Yield();
        yield return new AudioSynthesisChunk(Array.Empty<byte>(), AudioMimeType.Pcm);
        throw exception;
    }
}
