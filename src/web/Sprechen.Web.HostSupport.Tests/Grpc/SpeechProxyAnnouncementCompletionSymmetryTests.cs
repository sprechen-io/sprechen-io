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
/// Prüft übereinstimmende Logstufen für Beginn und Abschluss einer Sprachausgabe.
/// Hinweise auf fehlende Eingabe und behebbare Fehler verwenden Information, gewöhnliche Prompts Debug.
/// Abbrüche behalten die jeweilige Stufe und die bis dahin erfassten Chunk- und Bytezahlen.
/// </summary>
public sealed class SpeechProxyAnnouncementCompletionSymmetryTests
{
    [Theory]
    [InlineData(SprechenGrpcAnnouncementMetadata.NoInputKind)]
    [InlineData(SprechenGrpcAnnouncementMetadata.RecoverableFailureKind)]
    public async Task AnAnnouncementReadableWithoutDebug_HasACompletionReadableThere_Too(string announcementKind)
    {
        var logger = await ForwardAsync(announcementKind);

        Assert.Equal(LogLevel.Information, LevelOf(logger, "started"));
        Assert.Equal(LogLevel.Information, LevelOf(logger, "completed"));
    }

    [Theory]
    [InlineData(SprechenGrpcAnnouncementMetadata.PromptKind)]
    [InlineData(null)]
    public async Task AnOrdinaryPrompt_StaysAtDebugOnBothHalves(string? announcementKind)
    {
        // Gewöhnliche Prompts sollen auf beiden Seiten bei Debug bleiben.
        var logger = await ForwardAsync(announcementKind);

        Assert.Equal(LogLevel.Debug, LevelOf(logger, "started"));
        Assert.Equal(LogLevel.Debug, LevelOf(logger, "completed"));
    }

    [Theory]
    [InlineData(SprechenGrpcAnnouncementMetadata.NoInputKind, LogLevel.Information)]
    [InlineData(SprechenGrpcAnnouncementMetadata.PromptKind, LogLevel.Debug)]
    public async Task AnAbandonedAnnouncement_EndsOnTheRecordAtItsOwnLevel(
        string announcementKind,
        LogLevel expected)
    {

        var logger = await ForwardAsync(announcementKind, abortAfterFirstChunk: true);

        var aborted = Single(logger, "aborted");
        Assert.Equal(expected, aborted.Level);
        Assert.DoesNotContain(
            logger.Entries,
            e => e.Message.Contains("Proxy TextToSpeechToStream completed", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AnAbandonedAnnouncement_SaysHowMuchTheParticipantGot()
    {
        // Die Abbruchmeldung soll die bis dahin erfassten Chunks und Bytes enthalten.
        var logger = await ForwardAsync(
            SprechenGrpcAnnouncementMetadata.NoInputKind, abortAfterFirstChunk: true);

        var aborted = Single(logger, "aborted");
        Assert.Contains("chunkCount=1", aborted.Message, StringComparison.Ordinal);
        Assert.Contains("byteCount=2", aborted.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheCompletionCarriesTheCountersThatSayAudioWasProduced()
    {

        var logger = await ForwardAsync(SprechenGrpcAnnouncementMetadata.NoInputKind);

        var completed = Single(logger, "completed");
        Assert.Contains("chunkCount=1", completed.Message, StringComparison.Ordinal);
        Assert.Contains("byteCount=2", completed.Message, StringComparison.Ordinal);
    }

    private static LogLevel LevelOf(RecordingLogger<SpeechProxyService> logger, string half)
        => Single(logger, half).Level;

    private static (LogLevel Level, EventId EventId, string Message, Exception? Exception) Single(
        RecordingLogger<SpeechProxyService> logger,
        string half)
        => Assert.Single(
            logger.Entries,
            e => e.Message.Contains($"Proxy TextToSpeechToStream {half}", StringComparison.Ordinal));

    private static Task<RecordingLogger<SpeechProxyService>> ForwardAsync(string? announcementKind)
        => ForwardAsync(announcementKind, abortAfterFirstChunk: false);

    private static async Task<RecordingLogger<SpeechProxyService>> ForwardAsync(
        string? announcementKind,
        bool abortAfterFirstChunk)
    {
        var chunk = new SprechenProto.AudioResponse
        {
            MimeType = SprechenProto.AudioMimeType.AudioPcm,
            SampleRateHz = 24_000,
            Data = ByteString.CopyFrom(new byte[] { 0x1, 0x2 })
        };


        var backend = new FakeLanguageClient((_, _) => new AsyncServerStreamingCall<SprechenProto.AudioResponse>(
            new SequenceAsyncStreamReader<SprechenProto.AudioResponse>(
                [chunk],
                abortAfterFirstChunk
                    ? new RpcException(new Status(StatusCode.Cancelled, "caller went away"))
                    : null),
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
