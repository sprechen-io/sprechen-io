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
/// Prüft, dass der Beginn einer Erkennung auf Information protokolliert wird.
/// Beginn und Abschluss müssen dieselbe Sitzungskennung tragen, damit sie derselben Sitzung zugeordnet werden können.
/// </summary>
public sealed class SpeechProxyRecognitionPairingTests
{
    [Fact]
    public async Task AnOpenedRecognition_IsReadableWithoutDebug_AndNamesItsSession()
    {
        var logger = await RecognizeAsync(Task.FromResult(new SprechenProto.SpeechTranscript()), "sess-1");

        var started = Assert.Single(
            logger.Entries,
            e => e.Message.Contains("Proxy SpeechToText started", StringComparison.Ordinal));
        Assert.Equal(LogLevel.Information, started.Level);
        Assert.Contains("sess-1", started.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheOpeningAndItsTerminal_NameTheSameSession()
    {

        var logger = await RecognizeAsync(Task.FromResult(new SprechenProto.SpeechTranscript()), "sess-2");

        var started = Assert.Single(
            logger.Entries,
            e => e.Message.Contains("Proxy SpeechToText started", StringComparison.Ordinal));
        var terminal = Assert.Single(
            logger.Entries,
            e => e.Message.Contains("terminal=Empty", StringComparison.Ordinal));

        Assert.Contains("sess-2", started.Message, StringComparison.Ordinal);
        Assert.Contains("sess-2", terminal.Message, StringComparison.Ordinal);
        Assert.Equal(LogLevel.Information, terminal.Level);
    }

    private static async Task<RecordingLogger<SpeechProxyService>> RecognizeAsync(
        Task<SprechenProto.SpeechTranscript> response,
        string sessionId)
    {
        var backend = new FakeLanguageClient(
            textToSpeechToStream: null,
            speechToText: _ => new AsyncClientStreamingCall<SprechenProto.AudioChunk, SprechenProto.SpeechTranscript>(
                new CollectingClientStreamWriter<SprechenProto.AudioChunk>(),
                response,
                Task.FromResult(new Metadata()),
                static () => new Status(StatusCode.OK, string.Empty),
                static () => new Metadata(),
                static () => { }));

        var logger = new RecordingLogger<SpeechProxyService>();
        var proxy = new SpeechProxyService(backend, logger);

        var requestHeaders = new Metadata();
        SprechenGrpcSessionMetadata.AddOrReplaceSessionId(requestHeaders, sessionId);

        await proxy.SpeechToText(
            new SequenceAsyncStreamReader<SprechenProto.AudioChunk>(
            [
                new SprechenProto.AudioChunk
                {
                    Data = ByteString.CopyFrom(new byte[] { 1, 2 }),
                    MimeType = SprechenProto.AudioMimeType.AudioPcm,
                    LanguageCode = "de"
                }
            ]),
            TestServerCallContext.Create(
                "SpeechToText",
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
