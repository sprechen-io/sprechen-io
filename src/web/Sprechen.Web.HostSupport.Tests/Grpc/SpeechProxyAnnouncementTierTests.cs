using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Testing;
using Microsoft.Extensions.Logging;
using Sprechen.Proto.Transport;
using Sprechen.Web.HostSupport.Grpc.Adapter;
using Sprechen.Web.HostSupport.Tests.TestDoubles;
using Xunit;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Web.HostSupport.Tests.Grpc;

/// <summary>
/// Prüft die Logstufe beim Beginn unterschiedlicher Sprachausgaben.
/// Hinweise auf fehlende Eingabe und behebbare Fehler bleiben ohne Debug sichtbar; gewöhnliche Prompts bleiben bei Debug.
/// Die Sitzungskennung ermöglicht den Abgleich mit anderen Meldungen derselben Sitzung.
/// </summary>
public sealed class SpeechProxyAnnouncementTierTests
{
    [Theory]
    [InlineData(SprechenGrpcAnnouncementMetadata.NoInputKind)]
    [InlineData(SprechenGrpcAnnouncementMetadata.RecoverableFailureKind)]
    public async Task AnAnnouncementThatCanBeFalseAboutTheParticipant_StaysReadableWithoutDebug(string announcementKind)
    {
        var logger = await ForwardAsync(announcementKind);

        var entry = Assert.Single(
            logger.Entries,
            e => e.Message.Contains("Proxy TextToSpeechToStream started", StringComparison.Ordinal));
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains(announcementKind, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnOrdinaryPrompt_StaysAtDebug()
    {
        var logger = await ForwardAsync(SprechenGrpcAnnouncementMetadata.PromptKind);

        var entry = Assert.Single(
            logger.Entries,
            e => e.Message.Contains("Proxy TextToSpeechToStream started", StringComparison.Ordinal));
        Assert.Equal(LogLevel.Debug, entry.Level);
    }

    [Fact]
    public async Task AnAnnouncementThatDeclaresNothing_StaysAtDebug()
    {

        var logger = await ForwardAsync(announcementKind: null);

        var entry = Assert.Single(
            logger.Entries,
            e => e.Message.Contains("Proxy TextToSpeechToStream started", StringComparison.Ordinal));
        Assert.Equal(LogLevel.Debug, entry.Level);
        Assert.Contains(SprechenGrpcAnnouncementMetadata.PromptKind, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheAnnouncementLineCarriesTheSessionId_SoItCanBeJoinedToTheRecognitionsAroundIt()
    {
        // Die Sitzungskennung ermöglicht die Zuordnung zu derselben Sitzung, ohne allein Zeitstempel vergleichen zu müssen.
        var logger = await ForwardAsync(SprechenGrpcAnnouncementMetadata.NoInputKind, sessionId: "sess-abc");

        var entry = Assert.Single(
            logger.Entries,
            e => e.Message.Contains("Proxy TextToSpeechToStream started", StringComparison.Ordinal));
        Assert.Contains("sess-abc", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WithoutASessionId_TheLineSaysSoRatherThanInventingOne()
    {
        var logger = await ForwardAsync(SprechenGrpcAnnouncementMetadata.NoInputKind, sessionId: null);

        var entry = Assert.Single(
            logger.Entries,
            e => e.Message.Contains("Proxy TextToSpeechToStream started", StringComparison.Ordinal));
        Assert.Contains("sessionId=,", entry.Message, StringComparison.Ordinal);
    }

    private static Task<RecordingLogger<SpeechProxyService>> ForwardAsync(string? announcementKind)
        => ForwardAsync(announcementKind, sessionId: null);

    private static async Task<RecordingLogger<SpeechProxyService>> ForwardAsync(
        string? announcementKind,
        string? sessionId)
    {
        var chunk = new SprechenProto.AudioResponse
        {
            MimeType = SprechenProto.AudioMimeType.AudioPcm,
            SampleRateHz = 24_000,
            Data = ByteString.CopyFrom(new byte[] { 0x1, 0x2 })
        };

        var backend = new FakeLanguageClient((_, _) => new AsyncServerStreamingCall<SprechenProto.AudioResponse>(
            new SequenceAsyncStreamReader<SprechenProto.AudioResponse>([chunk]),
            Task.FromResult(new Metadata()),
            static () => new Status(StatusCode.OK, string.Empty),
            static () => new Metadata(),
            static () => { }));

        var logger = new RecordingLogger<SpeechProxyService>();
        var proxy = new SpeechProxyService(backend, logger);

        var requestHeaders = new Metadata();
        if (announcementKind is not null)
        {
            requestHeaders.Add(SprechenGrpcAnnouncementMetadata.AnnouncementKindHeaderName, announcementKind);
        }

        if (sessionId is not null)
        {
            SprechenGrpcSessionMetadata.AddOrReplaceSessionId(requestHeaders, sessionId);
        }

        await proxy.TextToSpeechToStream(
            new SprechenProto.TextRequest
            {
                Text = "hallo",
                LanguageCode = "de",
                DesiredMimeType = SprechenProto.AudioMimeType.AudioPcm
            },
            new RecordingServerStreamWriter<SprechenProto.AudioResponse>(),
            TestServerCallContext.Create(
                "TextToSpeechToStream",
                null,
                DateTime.UtcNow.AddMinutes(1),
                requestHeaders,
                CancellationToken.None,
                "127.0.0.1",
                null,
                null,
                _ => Task.CompletedTask,
                () => new WriteOptions(),
                _ => { }));

        return logger;
    }
}
