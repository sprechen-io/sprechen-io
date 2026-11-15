using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Testing;
using Moq;
using Sprechen.Proto.Transport;
using Sprechen.Service.Core.Services;
using Sprechen.Service.Grpc;
using Sprechen.Service.Options;
using Sprechen.Service.Tests.TestDoubles;
using Xunit;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Tests.Grpc;

public sealed class SpeechGrpcServiceTests
{
    [Fact]
    public async Task TextToSpeechToStream_MapsProtoMimeToCoreAndBack()
    {
        var language = new Mock<ISpeechService>();
        var supportedLanguages = CreateSupportedLanguages();
        var sample = new byte[] { 0x7, 0x8 };

        language.Setup(s => s.SynthesizeAsync(
                "hi",
                AudioMimeType.Wav,
                1.25f,
                It.IsAny<string?>(),
                It.IsAny<TextToSpeechPolitenessLevel>(),
                It.IsAny<CancellationToken>()))
            .Returns(SingleChunk(sample, AudioMimeType.Wav));

        var service = SpeechGrpcServiceTestFactory.Create(
            language.Object,
            supportedLanguages.Object);
        var writer = new RecordingServerStreamWriter<SprechenProto.AudioResponse>();
        var context = CreateContext("TextToSpeechToStream");
        var request = new SprechenProto.TextRequest
        {
            Text = "hi",
            LanguageCode = "de",
            DesiredMimeType = SprechenProto.AudioMimeType.AudioWav,
            SpeechRate = 1.25f
        };

        await service.TextToSpeechToStream(request, writer, context);

        var response = Assert.Single(writer.Items);
        Assert.Equal(SprechenProto.AudioMimeType.AudioWav, response.MimeType);
        Assert.Equal(ByteString.CopyFrom(sample), response.Data);
        language.VerifyAll();
    }

    [Fact]
    public async Task TextToSpeechToStream_WhenSpeechRateIsMissing_UsesDefaultRate()
    {
        var language = new Mock<ISpeechService>();
        var supportedLanguages = CreateSupportedLanguages();
        var sample = new byte[] { 0x2 };

        language.Setup(s => s.SynthesizeAsync(
                "hi",
                AudioMimeType.Pcm,
                1.0f,
                "de",
                TextToSpeechPolitenessLevel.Unspecified,
                It.IsAny<CancellationToken>()))
            .Returns(SingleChunk(sample, AudioMimeType.Pcm, sampleRateHz: 24_000));

        var service = SpeechGrpcServiceTestFactory.Create(
            language.Object,
            supportedLanguages.Object);
        var writer = new RecordingServerStreamWriter<SprechenProto.AudioResponse>();
        var request = new SprechenProto.TextRequest
        {
            Text = "hi",
            LanguageCode = "de",
            DesiredMimeType = SprechenProto.AudioMimeType.AudioPcm
        };

        await service.TextToSpeechToStream(request, writer, CreateContext("TextToSpeechToStream"));

        Assert.NotEmpty(writer.Items);
        language.VerifyAll();
    }

    [Fact]
    public async Task TextToSpeechToStream_MapsPolitenessLevelToCore()
    {
        var language = new Mock<ISpeechService>();
        var supportedLanguages = CreateSupportedLanguages();
        var sample = new byte[] { 0x1 };

        language.Setup(s => s.SynthesizeAsync(
                "hi",
                AudioMimeType.Pcm,
                It.IsAny<float>(),
                "de",
                TextToSpeechPolitenessLevel.Informal,
                It.IsAny<CancellationToken>()))
            .Returns(SingleChunk(sample, AudioMimeType.Pcm, sampleRateHz: 24_000));

        var service = SpeechGrpcServiceTestFactory.Create(
            language.Object,
            supportedLanguages.Object);
        var writer = new RecordingServerStreamWriter<SprechenProto.AudioResponse>();
        var context = CreateContext("TextToSpeechToStream");
        var request = new SprechenProto.TextRequest
        {
            Text = "hi",
            LanguageCode = "de",
            DesiredMimeType = SprechenProto.AudioMimeType.AudioPcm,
            PolitenessLevel = SprechenProto.TextToSpeechPolitenessLevel.TtsPolitenessInformal
        };

        await service.TextToSpeechToStream(request, writer, context);

        Assert.NotEmpty(writer.Items);
        language.VerifyAll();
    }

    [Fact]
    public async Task TextToSpeechToStream_Pcm_IsChunkedForLowLatency()
    {
        var language = new Mock<ISpeechService>();
        var supportedLanguages = CreateSupportedLanguages();
        var sample = new byte[5000];
        for (var i = 0; i < sample.Length; i++)
        {
            sample[i] = (byte)(i % 256);
        }

        language.Setup(s => s.SynthesizeAsync(
                "hi",
                AudioMimeType.Pcm,
                It.IsAny<float>(),
                It.IsAny<string?>(),
                It.IsAny<TextToSpeechPolitenessLevel>(),
                It.IsAny<CancellationToken>()))
            .Returns(SingleChunk(sample, AudioMimeType.Pcm, sampleRateHz: 24_000));

        var service = SpeechGrpcServiceTestFactory.Create(
            language.Object,
            supportedLanguages.Object);
        var writer = new RecordingServerStreamWriter<SprechenProto.AudioResponse>();
        var context = CreateContext("TextToSpeechToStream");
        var request = new SprechenProto.TextRequest
        {
            Text = "hi",
            LanguageCode = "de",
            DesiredMimeType = SprechenProto.AudioMimeType.AudioPcm
        };

        await service.TextToSpeechToStream(request, writer, context);

        Assert.Equal(2, writer.Items.Count);
        Assert.All(writer.Items, r => Assert.Equal(SprechenProto.AudioMimeType.AudioPcm, r.MimeType));
        Assert.All(writer.Items, r => Assert.Equal(24_000, r.SampleRateHz));
        Assert.Equal(4096, writer.Items[0].Data.Length);
        Assert.Equal(904, writer.Items[1].Data.Length);

        var combined = new byte[sample.Length];
        writer.Items[0].Data.Span.CopyTo(combined);
        writer.Items[1].Data.Span.CopyTo(combined.AsSpan(writer.Items[0].Data.Length));
        Assert.Equal(sample, combined);
        language.VerifyAll();
    }

    [Fact]
    public async Task TextToSpeechToStream_Pcm_WithAudioPacingEnabled_DelaysStreaming()
    {
        var language = new Mock<ISpeechService>();
        var supportedLanguages = CreateSupportedLanguages();
        var sample = new byte[8192];
        for (var i = 0; i < sample.Length; i++)
        {
            sample[i] = (byte)(i % 256);
        }

        language.Setup(s => s.SynthesizeAsync(
                "hi",
                AudioMimeType.Pcm,
                It.IsAny<float>(),
                It.IsAny<string?>(),
                It.IsAny<TextToSpeechPolitenessLevel>(),
                It.IsAny<CancellationToken>()))
            .Returns(() => SingleChunk(sample, AudioMimeType.Pcm, sampleRateHz: 24_000));

        var request = new SprechenProto.TextRequest
        {
            Text = "hi",
            LanguageCode = "de",
            DesiredMimeType = SprechenProto.AudioMimeType.AudioPcm
        };

        var pacedService = SpeechGrpcServiceTestFactory.Create(
            language.Object,
            supportedLanguages.Object,
            new AudioPacingOptions { Enabled = true });
        var pacedWriter = new RecordingServerStreamWriter<SprechenProto.AudioResponse>();
        var pacedContext = CreateContext("TextToSpeechToStream");
        var pacedSw = Stopwatch.StartNew();
        await pacedService.TextToSpeechToStream(request, pacedWriter, pacedContext);
        pacedSw.Stop();

        var unpacedService = SpeechGrpcServiceTestFactory.Create(
            language.Object,
            supportedLanguages.Object,
            new AudioPacingOptions { Enabled = false });
        var unpacedWriter = new RecordingServerStreamWriter<SprechenProto.AudioResponse>();
        var unpacedContext = CreateContext("TextToSpeechToStream");
        var unpacedSw = Stopwatch.StartNew();
        await unpacedService.TextToSpeechToStream(request, unpacedWriter, unpacedContext);
        unpacedSw.Stop();

        Assert.Equal(pacedWriter.Items.Count, unpacedWriter.Items.Count);
        Assert.Equal(2, pacedWriter.Items.Count);
        Assert.True(
            pacedSw.Elapsed >= unpacedSw.Elapsed + TimeSpan.FromMilliseconds(100),
            $"Expected paced streaming to be slower. paced={pacedSw.Elapsed.TotalMilliseconds:0}ms unpaced={unpacedSw.Elapsed.TotalMilliseconds:0}ms");

        language.Verify(s => s.SynthesizeAsync(
                "hi",
                AudioMimeType.Pcm,
                It.IsAny<float>(),
                It.IsAny<string?>(),
                It.IsAny<TextToSpeechPolitenessLevel>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task TextToSpeechToStream_ClientCancellation_DoesNotThrow()
    {
        var language = new Mock<ISpeechService>();
        var supportedLanguages = CreateSupportedLanguages();

        var sample = new byte[5000];
        for (var i = 0; i < sample.Length; i++)
        {
            sample[i] = (byte)(i % 256);
        }

        language.Setup(s => s.SynthesizeAsync(
                "hi",
                AudioMimeType.Pcm,
                It.IsAny<float>(),
                It.IsAny<string?>(),
                It.IsAny<TextToSpeechPolitenessLevel>(),
                It.IsAny<CancellationToken>()))
            .Returns(SingleChunk(sample, AudioMimeType.Pcm, sampleRateHz: 24_000));

        using var cts = new CancellationTokenSource();
        var service = SpeechGrpcServiceTestFactory.Create(
            language.Object,
            supportedLanguages.Object);
        var writer = new CancellingServerStreamWriter<SprechenProto.AudioResponse>(cts, cancelAfterWrites: 1);
        var context = CreateContext("TextToSpeechToStream", cts.Token);
        var request = new SprechenProto.TextRequest
        {
            Text = "hi",
            LanguageCode = "de",
            DesiredMimeType = SprechenProto.AudioMimeType.AudioPcm
        };

        await service.TextToSpeechToStream(request, writer, context);

        Assert.Single(writer.Items);
        Assert.Equal(SprechenProto.AudioMimeType.AudioPcm, writer.Items[0].MimeType);
        Assert.Equal(24_000, writer.Items[0].SampleRateHz);
        language.VerifyAll();
    }

    [Fact]
    public async Task SpeechToText_WhenConfidencePresent_MapsConfidence()
    {
        var language = new Mock<ISpeechService>(MockBehavior.Strict);
        language.Setup(s => s.TranscribeStreamingAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new SpeechTranscript("hallo", 72));
        var supportedLanguages = CreateSupportedLanguages();

        var service = SpeechGrpcServiceTestFactory.Create(
            language.Object,
            supportedLanguages.Object);

        var response = await service.SpeechToText(CreateSpeechToTextRequestStream(), CreateContext("SpeechToText"));

        Assert.Equal("hallo", response.Text);
        Assert.True(response.HasConfidence);
        Assert.Equal(72, response.Confidence);
        language.VerifyAll();
    }

    [Fact]
    public async Task SpeechToText_WhenConfidenceMissing_LeavesConfidenceUnset()
    {
        var language = new Mock<ISpeechService>(MockBehavior.Strict);
        language.Setup(s => s.TranscribeStreamingAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new SpeechTranscript("hallo", null));
        var supportedLanguages = CreateSupportedLanguages();

        var service = SpeechGrpcServiceTestFactory.Create(
            language.Object,
            supportedLanguages.Object);

        var response = await service.SpeechToText(CreateSpeechToTextRequestStream(), CreateContext("SpeechToText"));

        Assert.Equal("hallo", response.Text);
        Assert.False(response.HasConfidence);
        language.VerifyAll();
    }

    [Fact]
    public async Task SpeechToTextStream_MapsConfidencePerItem()
    {
        var language = new Mock<ISpeechService>(MockBehavior.Strict);
        language.Setup(s => s.StreamConversationAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de"))
            .Returns(StreamConversationResults(
                new SpeechConversationTranscriptUpdateEvent("eins", 61, null),
                new SpeechConversationTranscriptFinalizedEvent("zwei", null, null),
                new SpeechConversationTranscriptFinalizedEvent(" ", 99, null)));
        var supportedLanguages = CreateSupportedLanguages();

        var service = SpeechGrpcServiceTestFactory.Create(
            language.Object,
            supportedLanguages.Object);
        var writer = new RecordingServerStreamWriter<SprechenProto.SpeechTranscript>();

        await service.SpeechToTextStream(CreateSpeechToTextRequestStream(), writer, CreateContext("SpeechToTextStream"));

        Assert.Equal(2, writer.Items.Count);
        Assert.Equal("eins", writer.Items[0].Text);
        Assert.True(writer.Items[0].HasConfidence);
        Assert.Equal(61, writer.Items[0].Confidence);
        Assert.Equal("zwei", writer.Items[1].Text);
        Assert.False(writer.Items[1].HasConfidence);
        language.VerifyAll();
    }

    [Fact]
    public async Task SpeechToTextStream_IgnoresNonTranscriptConversationEvents()
    {
        var language = new Mock<ISpeechService>(MockBehavior.Strict);
        language.Setup(s => s.StreamConversationAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de"))
            .Returns(StreamConversationResults(
                new SpeechConversationTurnStartedEvent(1),
                new SpeechConversationLanguageDetectedEvent(["de"], 1),
                new SpeechConversationTranscriptUpdateEvent("eins", 61, 1),
                new SpeechConversationEndOfTurnEvent(1, "eins"),
                new SpeechConversationCompletedEvent()));
        var supportedLanguages = CreateSupportedLanguages();

        var service = SpeechGrpcServiceTestFactory.Create(
            language.Object,
            supportedLanguages.Object);
        var writer = new RecordingServerStreamWriter<SprechenProto.SpeechTranscript>();

        await service.SpeechToTextStream(CreateSpeechToTextRequestStream(), writer, CreateContext("SpeechToTextStream"));

        var transcript = Assert.Single(writer.Items);
        Assert.Equal("eins", transcript.Text);
        Assert.True(transcript.HasConfidence);
        Assert.Equal(61, transcript.Confidence);
        language.VerifyAll();
    }

    [Fact]
    public async Task SpeechToConversationStream_WhenClientStreamGoesIdle_CompletesConversationAfterGracefulInputFinalization()
    {
        var language = new Mock<ISpeechService>(MockBehavior.Strict);
        var supportedLanguages = CreateSupportedLanguages();
        var requestStream = new ChannelAsyncStreamReader<SprechenProto.AudioChunk>();
        requestStream.Write(new SprechenProto.AudioChunk
        {
            Data = ByteString.CopyFrom(new byte[] { 0x1, 0x2 }),
            MimeType = SprechenProto.AudioMimeType.AudioPcm,
            LanguageCode = "de"
        });

        var observedChunks = new List<byte[]>();
        language.Setup(service => service.StreamConversationAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de",
                It.IsAny<RecognitionProfile>()))
            .Returns((IAsyncEnumerable<byte[]> chunks, AudioMimeType _, CancellationToken _, string? _, RecognitionProfile _) =>
                CaptureChunksAndComplete(chunks, observedChunks));

        var logger = new RecordingLogger<SpeechGrpcService>();
        var service = SpeechGrpcServiceTestFactory.Create(
            language.Object,
            supportedLanguages.Object,
            new ConversationStreamingOptions { ClientInactivityTimeoutMs = 30 },
            logger);
        var writer = new RecordingServerStreamWriter<SprechenProto.SpeechConversationEventMessage>();
        var requestHeaders = new Metadata
        {
            { SprechenGrpcCorrelationMetadata.CorrelationIdHeaderName, "corr-123" }
        };

        await service.SpeechToConversationStream(
            requestStream,
            writer,
            CreateContext("SpeechToConversationStream", CancellationToken.None, requestHeaders));

        var observedFrame = Assert.Single(observedChunks);
        Assert.Equal(new byte[] { 0x1, 0x2 }, observedFrame);

        var completedMessage = Assert.Single(writer.Items);
        Assert.NotNull(completedMessage.ConversationCompleted);
        VerifyLogContains(
            logger,
            LogLevel.Debug,
            "SpeechToConversationStream request started",
            "corr-123");
        VerifyLogContains(
            logger,
            LogLevel.Debug,
            "SpeechToConversationStream request completed",
            "InactivityTimeout",
            "corr-123");
        language.VerifyAll();
    }

    [Fact]
    public async Task SpeechToText_DoesNotLogRecognizedTranscriptContent()
    {
        var language = new Mock<ISpeechService>(MockBehavior.Strict);
        language.Setup(s => s.TranscribeStreamingAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new SpeechTranscript("okay luma anwendungskatalog oeffnen", 88));
        var supportedLanguages = CreateSupportedLanguages();
        var logger = new RecordingLogger<SpeechGrpcService>();

        var service = SpeechGrpcServiceTestFactory.Create(
            language.Object,
            supportedLanguages.Object,
            logger);

        var response = await service.SpeechToText(CreateSpeechToTextRequestStream(), CreateContext("SpeechToText"));

        Assert.Equal("okay luma anwendungskatalog oeffnen", response.Text);
        VerifyLogDoesNotContain(logger, "okay luma anwendungskatalog oeffnen");
        language.VerifyAll();
    }

    private static async IAsyncEnumerable<AudioSynthesisChunk> SingleChunk(byte[] data, AudioMimeType mime, int sampleRateHz)
    {
        await Task.Yield();
        yield return new AudioSynthesisChunk(data, mime, sampleRateHz);
    }

    private static Mock<ISupportedLanguagesService> CreateSupportedLanguages()
    {
        var supportedLanguages = new Mock<ISupportedLanguagesService>(MockBehavior.Strict);
        supportedLanguages.SetupGet(s => s.DefaultIsoCode).Returns("de");
        supportedLanguages.SetupGet(s => s.Languages).Returns(new[] { new SupportedLanguage("de", "Deutsch") });
        supportedLanguages
            .Setup(s => s.IsSupportedIsoCode(It.IsAny<string>()))
            .Returns((string iso) => string.Equals(iso, "de", StringComparison.OrdinalIgnoreCase));

        return supportedLanguages;
    }

    private static IAsyncEnumerable<AudioSynthesisChunk> SingleChunk(byte[] data, AudioMimeType mime)
    {
        return SingleChunk(data, mime, sampleRateHz: 0);
    }

    private static SequenceAsyncStreamReader<SprechenProto.AudioChunk> CreateSpeechToTextRequestStream()
    {
        return new SequenceAsyncStreamReader<SprechenProto.AudioChunk>(
            new[]
            {
                new SprechenProto.AudioChunk
                {
                    Data = ByteString.CopyFrom(new byte[] { 0x1, 0x2 }),
                    MimeType = SprechenProto.AudioMimeType.AudioPcm,
                    LanguageCode = "de",
                }
            });
    }

    private static async IAsyncEnumerable<SpeechConversationEvent> StreamConversationResults(params SpeechConversationEvent[] results)
    {
        foreach (var result in results)
        {
            await Task.Yield();
            yield return result;
        }
    }

    private static async IAsyncEnumerable<SpeechConversationEvent> CaptureChunksAndComplete(
        IAsyncEnumerable<byte[]> chunks,
        List<byte[]> observedChunks)
    {
        await foreach (var chunk in chunks)
        {
            observedChunks.Add(chunk);
        }

        yield return new SpeechConversationCompletedEvent();
    }

    private static void VerifyLogContains(
        RecordingLogger<SpeechGrpcService> logger,
        LogLevel level,
        params string[] requiredFragments)
    {
        Assert.Contains(
            logger.Entries,
            entry => entry.Level == level && ContainsAllFragments(entry.Message, requiredFragments));
    }

    private static void VerifyLogDoesNotContain(
        RecordingLogger<SpeechGrpcService> logger,
        string forbiddenFragment)
    {
        Assert.DoesNotContain(
            logger.Entries,
            entry => ContainsFragment(entry.Message, forbiddenFragment));
    }

    private static bool ContainsAllFragments(string message, IReadOnlyList<string> requiredFragments)
    {
        return requiredFragments.All(fragment => message.Contains(fragment, StringComparison.Ordinal));
    }

    private static bool ContainsFragment(string message, string fragment)
    {
        return message.Contains(fragment, StringComparison.Ordinal);
    }

    private static ServerCallContext CreateContext(string method) => CreateContext(method, CancellationToken.None);

    private static ServerCallContext CreateContext(string method, CancellationToken cancellationToken) =>
        CreateContext(method, cancellationToken, new Metadata());

    private static ServerCallContext CreateContext(string method, CancellationToken cancellationToken, Metadata requestHeaders) =>
        TestServerCallContext.Create(
            method,
            null,
            DateTime.UtcNow.AddMinutes(1),
            requestHeaders,
            cancellationToken,
            "127.0.0.1",
            null,
            null,
            _ => Task.CompletedTask,
            () => new WriteOptions(),
            _ => { }
        );
}
