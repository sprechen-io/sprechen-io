using Sprechen.Service.Grpc;
using Sprechen.Service.Tests.TestDoubles;
using Xunit;

namespace Sprechen.Service.Tests.Grpc.Cross;

public sealed class SpeechSynthesisDiagnosticsEmitterTests
{
    [Fact]
    public void LogSpeakRequested_LogsOnlyAggregateTextLength()
    {
        var logger = new RecordingLogger<SpeechGrpcService>();
        var emitter = new SpeechSynthesisDiagnosticsEmitter(
            Microsoft.Extensions.Options.Options.Create(new SpeechProviderOptions()),
            logger);

        emitter.LogSpeakRequested("de", AudioMimeType.Pcm, textLength: 20, speechRate: 1.0f);

        Assert.Contains(
            logger.Entries,
            entry => entry.Message.Contains("TextToSpeechToStream speak request", StringComparison.Ordinal)
                && entry.Message.Contains("textLength=20", StringComparison.Ordinal));
    }

    [Fact]
    public void LogSpeakCompleted_LogsAggregateChunkAndByteCounts()
    {
        var logger = new RecordingLogger<SpeechGrpcService>();
        var emitter = new SpeechSynthesisDiagnosticsEmitter(
            Microsoft.Extensions.Options.Options.Create(new SpeechProviderOptions()),
            logger);

        emitter.LogSpeakCompleted("de", AudioMimeType.Pcm, speechRate: 1.0f, chunkCount: 42, byteCount: 128_000, firstSampleRateHz: 24_000);

        Assert.Contains(
            logger.Entries,
            entry => entry.Message.Contains("TextToSpeechToStream speak completed", StringComparison.Ordinal)
                && entry.Message.Contains("chunkCount=42", StringComparison.Ordinal)
                && entry.Message.Contains("byteCount=128000", StringComparison.Ordinal));
    }
}
