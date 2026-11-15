using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Proto.Transport;
using Sprechen.Service.Grpc;
using Sprechen.Service.Options;
using Sprechen.Service.Tests.Grpc;
using Xunit;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Tests.Grpc.Endpoints;

public sealed class TextToSpeechAudioStreamWriterTests
{
    [Fact]
    public async Task WriteAsync_WhenAudioChunksArriveWritesResponsesAndReturnsCounts()
    {
        var sut = CreateSut();
        var writer = new RecordingServerStreamWriter<SprechenProto.AudioResponse>();

        var result = await sut.WriteAsync(
            AudioChunks(new AudioSynthesisChunk(new byte[] { 0x1, 0x2, 0x3 }, AudioMimeType.Pcm, sampleRateHz: 16_000)),
            writer,
            SpeechProvider.Deepgram,
            "de",
            SprechenProto.AudioMimeType.AudioPcm,
            AudioMimeType.Pcm,
            "hello",
            speechRate: 1.0f,
            traceId: "trace",
            ct: CancellationToken.None);

        Assert.Equal(1, result.ChunkCount);
        Assert.Equal(3, result.ByteCount);
        Assert.Equal(16_000, result.FirstSampleRateHz);
        var response = Assert.Single(writer.Items);
        Assert.Equal(SprechenProto.AudioMimeType.AudioPcm, response.MimeType);
        Assert.Equal(16_000, response.SampleRateHz);
    }

    [Fact]
    public async Task WriteAsync_WhenNoAudioThrowsStableProviderFailure()
    {
        var sut = CreateSut();

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            sut.WriteAsync(
                AudioChunks(new AudioSynthesisChunk(Array.Empty<byte>(), AudioMimeType.Pcm)),
                new RecordingServerStreamWriter<SprechenProto.AudioResponse>(),
                SpeechProvider.Deepgram,
                "de",
                SprechenProto.AudioMimeType.AudioPcm,
                AudioMimeType.Pcm,
                "hello",
                speechRate: 1.0f,
                traceId: "trace",
                ct: CancellationToken.None));

        Assert.Equal(StatusCode.Internal, ex.StatusCode);
        Assert.True(SprechenGrpcErrorMetadata.TryGetErrorCode(ex, out var errorCode));
        Assert.Equal(SprechenProto.SprechenErrorCode.ProviderFailure, errorCode);
    }

    private static TextToSpeechAudioStreamWriter CreateSut()
    {
        return new TextToSpeechAudioStreamWriter(
            Microsoft.Extensions.Options.Options.Create(new AudioPacingOptions()),
            new SpeechSynthesisDiagnosticsEmitter(
                Microsoft.Extensions.Options.Options.Create(new SpeechProviderOptions()),
                NullLogger<SpeechGrpcService>.Instance),
            NullLogger<SpeechGrpcService>.Instance);
    }

    private static async IAsyncEnumerable<AudioSynthesisChunk> AudioChunks(params AudioSynthesisChunk[] chunks)
    {
        foreach (var chunk in chunks)
        {
            await Task.Yield();
            yield return chunk;
        }
    }
}
