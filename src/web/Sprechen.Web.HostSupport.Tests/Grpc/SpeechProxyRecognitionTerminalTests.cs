using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Testing;
using Microsoft.Extensions.Logging;
using Sprechen.Web.HostSupport.Grpc.Adapter;
using Sprechen.Web.HostSupport.Tests.TestDoubles;
using Xunit;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Web.HostSupport.Tests.Grpc;

/// <summary>
/// Prüft die Unterscheidung zwischen fehlgeschlagenem Aufruf, angefordertem Abbruch und erfolgreich empfangenem leerem Transkript.
/// Die Abschlussmeldung enthält den Ergebnisstatus sowie Chunk- und Bytezahlen.
/// </summary>
public sealed class SpeechProxyRecognitionTerminalTests
{
    [Fact]
    public async Task BackendCancellation_WithTheCallerStillConnected_IsNotAnsweredWithAnEmptyTranscript()
    {
        var logger = new RecordingLogger<SpeechProxyService>();
        var proxy = new SpeechProxyService(
            CreateBackend(Task.FromException<SprechenProto.SpeechTranscript>(
                new RpcException(new Status(StatusCode.Cancelled, "backend cancelled")))),
            logger);

        var thrown = await Record.ExceptionAsync(() =>
            proxy.SpeechToText(OneChunk(), CreateContext(CancellationToken.None)));

        // Ein Backendabbruch bei weiterhin verbundenem Aufrufer muss als Fehler weitergegeben werden, statt wie ein erfolgreiches leeres Transkript zu erscheinen.
        Assert.NotNull(thrown);
        Assert.IsType<RpcException>(thrown);

        var terminal = Assert.Single(TerminalEntries(logger));
        Assert.Equal(LogLevel.Warning, terminal.Level);
        Assert.Contains("terminal=Failed", terminal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CallerThatWentAway_IsStillAnsweredWithAnEmptyTranscript()
    {
        using var callerGone = new CancellationTokenSource();
        await callerGone.CancelAsync();

        var logger = new RecordingLogger<SpeechProxyService>();
        var proxy = new SpeechProxyService(
            CreateBackend(Task.FromException<SprechenProto.SpeechTranscript>(
                new RpcException(new Status(StatusCode.Cancelled, "caller went away")))),
            logger);

        var response = await proxy.SpeechToText(OneChunk(), CreateContext(callerGone.Token));


        Assert.Equal(string.Empty, response.Text);

        var terminal = Assert.Single(TerminalEntries(logger));
        Assert.Contains("terminal=CallerGone", terminal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EmptyTranscriptFromACompletedCall_StaysADomainResult()
    {
        var logger = new RecordingLogger<SpeechProxyService>();
        var proxy = new SpeechProxyService(
            CreateBackend(Task.FromResult(new SprechenProto.SpeechTranscript())),
            logger);

        var response = await proxy.SpeechToText(OneChunk(), CreateContext(CancellationToken.None));

        // Ein erfolgreich abgeschlossener Aufruf ohne erkannten Text bleibt ein leeres Ergebnis und wird nicht als Transportfehler behandelt.
        Assert.Equal(string.Empty, response.Text);

        var terminal = Assert.Single(TerminalEntries(logger));
        Assert.Contains("terminal=Empty", terminal.Message, StringComparison.Ordinal);

        // Leere Ergebnisse sollen auch bei deaktivierten Debug-Meldungen erkennbar bleiben.
        Assert.Equal(LogLevel.Information, terminal.Level);
    }

    [Fact]
    public async Task EveryRecognition_LeavesExactlyOneTerminalLine()
    {
        var logger = new RecordingLogger<SpeechProxyService>();
        var proxy = new SpeechProxyService(
            CreateBackend(Task.FromResult(new SprechenProto.SpeechTranscript { Text = "einfaches Beispiel" })),
            logger);

        await proxy.SpeechToText(OneChunk(), CreateContext(CancellationToken.None));


        Assert.Single(logger.Entries, entry => entry.Message.Contains("Proxy SpeechToText started", StringComparison.Ordinal));

        var terminal = Assert.Single(TerminalEntries(logger));
        Assert.Contains("terminal=Transcript", terminal.Message, StringComparison.Ordinal);
        Assert.Contains("transcriptLength=18", terminal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheTerminalLine_SaysHowMuchAudioTheRecognitionCarried()
    {
        var logger = new RecordingLogger<SpeechProxyService>();
        var proxy = new SpeechProxyService(
            CreateBackend(Task.FromResult(new SprechenProto.SpeechTranscript { Text = "Beta" })),
            logger);

        await proxy.SpeechToText(Chunks(3, bytesEach: 4), CreateContext(CancellationToken.None));

        // Chunk- und Bytezahlen ergänzen die Startmeldung.
        var terminal = Assert.Single(TerminalEntries(logger));
        Assert.Contains("chunks=3", terminal.Message, StringComparison.Ordinal);
        Assert.Contains("bytes=12", terminal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AStreamThatCarriedNothing_IsDistinguishableFromOneThatDid()
    {
        var logger = new RecordingLogger<SpeechProxyService>();
        var proxy = new SpeechProxyService(
            CreateBackend(Task.FromException<SprechenProto.SpeechTranscript>(
                new RpcException(new Status(StatusCode.Cancelled, "cancelled before any audio")))),
            logger);

        await Record.ExceptionAsync(() =>
            proxy.SpeechToText(Chunks(0, bytesEach: 0), CreateContext(CancellationToken.None)));

        // Auch im Fehlerfall müssen die bis dahin erfassten Zähler im Abschluss stehen.
        var terminal = Assert.Single(TerminalEntries(logger));
        Assert.Contains("terminal=Failed", terminal.Message, StringComparison.Ordinal);
        Assert.Contains("chunks=0", terminal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ACancelledLanguageLookup_IsNotAnsweredWithAnEmptyLanguageList()
    {
        var backend = new FakeSupportedLanguagesClient((_, _) =>
            throw new RpcException(new Status(StatusCode.Cancelled, "backend cancelled")));
        var proxy = new SpeechProxyService(backend, new RecordingLogger<SpeechProxyService>());

        var thrown = await Record.ExceptionAsync(() =>
            proxy.GetSupportedLanguages(new Google.Protobuf.WellKnownTypes.Empty(), CreateContext(CancellationToken.None)));

        // Eine fehlgeschlagene Sprachabfrage darf nicht als erfolgreich gelieferte leere Liste erscheinen; Verbraucher müssen den Fehler erkennen können.
        Assert.NotNull(thrown);
        Assert.IsType<RpcException>(thrown);
    }

    private static IEnumerable<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> TerminalEntries(
        RecordingLogger<SpeechProxyService> logger)
        => logger.Entries.Where(entry =>
            entry.Message.Contains("Proxy SpeechToText terminal", StringComparison.Ordinal));

    private static FakeLanguageClient CreateBackend(Task<SprechenProto.SpeechTranscript> response)
        => new(
            textToSpeechToStream: null,
            speechToText: _ => new AsyncClientStreamingCall<SprechenProto.AudioChunk, SprechenProto.SpeechTranscript>(
                new CollectingClientStreamWriter<SprechenProto.AudioChunk>(),
                response,
                Task.FromResult(new Metadata()),
                static () => new Status(StatusCode.OK, string.Empty),
                static () => new Metadata(),
                static () => { }),
            speechToTextStream: null,
            speechToConversationStream: null);

    private static SequenceAsyncStreamReader<SprechenProto.AudioChunk> Chunks(int count, int bytesEach)
        => new([.. Enumerable.Range(0, count).Select(_ => new SprechenProto.AudioChunk
        {
            Data = ByteString.CopyFrom(new byte[bytesEach]),
            MimeType = SprechenProto.AudioMimeType.AudioPcm,
            LanguageCode = "de"
        })]);

    private static SequenceAsyncStreamReader<SprechenProto.AudioChunk> OneChunk()
        => new(
        [
            new SprechenProto.AudioChunk
            {
                Data = ByteString.CopyFrom([0x1]),
                MimeType = SprechenProto.AudioMimeType.AudioPcm,
                LanguageCode = "de"
            }
        ]);

    private static ServerCallContext CreateContext(CancellationToken cancellationToken)
        => TestServerCallContext.Create(
            "SpeechToText",
            null,
            DateTime.UtcNow.AddMinutes(1),
            new Metadata(),
            cancellationToken,
            "127.0.0.1",
            null,
            null,
            _ => Task.CompletedTask,
            () => new WriteOptions(),
            _ => { });
}
