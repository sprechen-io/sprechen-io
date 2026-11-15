using Grpc.Core;
using Sprechen.Proto;
using Microsoft.Extensions.Logging;
using Sprechen.Web.HostSupport.Tests.TestDoubles;
using Sprechen.Web.HostSupport.Grpc.Proxy;
using Xunit;

namespace Sprechen.Web.HostSupport.Tests.Grpc;

public sealed class SpeechProxyTextToSpeechAudioCompletionTests
{
    [Fact]
    public void LogOrThrow_WhenNoAudioWasForwarded_ThrowsProviderFailure()
    {
        var logger = new RecordingLogger<SpeechProxyTextToSpeechAudioCompletionTests>();
        var completion = new SpeechProxyTextToSpeechAudioCompletion(logger);

        var exception = Assert.Throws<RpcException>(() => completion.LogOrThrow(
            "TextToSpeechToStream",
            new TextRequest { LanguageCode = "de-DE", DesiredMimeType = AudioMimeType.AudioPcm },
            (ChunkCount: 0, ByteCount: 0, FirstSampleRateHz: null),
            "<none>",
            LogLevel.Debug));

        Assert.Equal(StatusCode.Internal, exception.StatusCode);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error);
    }
}
