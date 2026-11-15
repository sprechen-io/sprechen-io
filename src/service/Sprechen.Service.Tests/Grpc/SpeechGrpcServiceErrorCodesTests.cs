using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Diagnostics.Metrics;
using Grpc.Core;
using Grpc.Core.Testing;
using Moq;
using Sprechen.Service.Core.Services;
using Sprechen.Service.Grpc;
using Sprechen.Service.Options;
using Xunit;
using SprechenProto = Sprechen.Proto;
using Sprechen.Proto.Transport;

namespace Sprechen.Service.Tests.Grpc;

[Collection(SpeechRecognitionTelemetryCollection.Name)]
public sealed class SpeechGrpcServiceErrorCodesTests
{
    [Fact]
    public async Task TextToSpeechToStream_MissingText_ReturnsCodedInvalidArgument()
    {
        var language = new Mock<ISpeechService>(MockBehavior.Strict);
        var supportedLanguages = CreateSupportedLanguages();
        var service = CreateService(language.Object, supportedLanguages.Object);

        var request = new SprechenProto.TextRequest
        {
            Text = " ",
            LanguageCode = "de",
            DesiredMimeType = SprechenProto.AudioMimeType.AudioPcm
        };

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            service.TextToSpeechToStream(request, new RecordingServerStreamWriter<SprechenProto.AudioResponse>(), CreateContext("TextToSpeechToStream")));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        AssertErrorCode(ex, SprechenProto.SprechenErrorCode.TextRequired);
    }

    [Fact]
    public async Task TextToSpeechToStream_MissingLanguageCode_ReturnsCodedInvalidArgument()
    {
        var language = new Mock<ISpeechService>(MockBehavior.Strict);
        var supportedLanguages = CreateSupportedLanguages();
        var service = CreateService(language.Object, supportedLanguages.Object);

        var request = new SprechenProto.TextRequest
        {
            Text = "hi",
            DesiredMimeType = SprechenProto.AudioMimeType.AudioPcm
        };

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            service.TextToSpeechToStream(request, new RecordingServerStreamWriter<SprechenProto.AudioResponse>(), CreateContext("TextToSpeechToStream")));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        AssertErrorCode(ex, SprechenProto.SprechenErrorCode.LanguageCodeRequired);
    }

    [Fact]
    public async Task TextToSpeechToStream_InvalidLanguageCode_ReturnsCodedInvalidArgument()
    {
        var language = new Mock<ISpeechService>(MockBehavior.Strict);
        var supportedLanguages = CreateSupportedLanguages();
        var service = CreateService(language.Object, supportedLanguages.Object);

        var request = new SprechenProto.TextRequest
        {
            Text = "hi",
            LanguageCode = "german",
            DesiredMimeType = SprechenProto.AudioMimeType.AudioPcm
        };

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            service.TextToSpeechToStream(request, new RecordingServerStreamWriter<SprechenProto.AudioResponse>(), CreateContext("TextToSpeechToStream")));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        AssertErrorCode(ex, SprechenProto.SprechenErrorCode.LanguageCodeInvalid);
    }

    [Fact]
    public async Task TextToSpeechToStream_UnsupportedLanguageCode_ReturnsCodedInvalidArgument()
    {
        var language = new Mock<ISpeechService>(MockBehavior.Strict);
        var supportedLanguages = CreateSupportedLanguages();
        var service = CreateService(language.Object, supportedLanguages.Object);

        var request = new SprechenProto.TextRequest
        {
            Text = "hi",
            LanguageCode = "fr",
            DesiredMimeType = SprechenProto.AudioMimeType.AudioPcm
        };

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            service.TextToSpeechToStream(request, new RecordingServerStreamWriter<SprechenProto.AudioResponse>(), CreateContext("TextToSpeechToStream")));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        AssertErrorCode(ex, SprechenProto.SprechenErrorCode.LanguageCodeUnsupported);
    }

    [Fact]
    public async Task TextToSpeechToStream_MissingDesiredMimeType_ReturnsCodedInvalidArgument()
    {
        var language = new Mock<ISpeechService>(MockBehavior.Strict);
        var supportedLanguages = CreateSupportedLanguages();
        var service = CreateService(language.Object, supportedLanguages.Object);

        var request = new SprechenProto.TextRequest
        {
            Text = "hi",
            LanguageCode = "de"
        };

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            service.TextToSpeechToStream(request, new RecordingServerStreamWriter<SprechenProto.AudioResponse>(), CreateContext("TextToSpeechToStream")));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        AssertErrorCode(ex, SprechenProto.SprechenErrorCode.AudioOutputMimeTypeRequired);
    }

    [Fact]
    public async Task TextToSpeechToStream_UnsupportedDesiredMimeType_ReturnsCodedInvalidArgument()
    {
        var language = new Mock<ISpeechService>(MockBehavior.Strict);
        var supportedLanguages = CreateSupportedLanguages();
        var service = CreateService(language.Object, supportedLanguages.Object);

        var request = new SprechenProto.TextRequest
        {
            Text = "hi",
            LanguageCode = "de",
            DesiredMimeType = (SprechenProto.AudioMimeType)999
        };

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            service.TextToSpeechToStream(request, new RecordingServerStreamWriter<SprechenProto.AudioResponse>(), CreateContext("TextToSpeechToStream")));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        AssertErrorCode(ex, SprechenProto.SprechenErrorCode.AudioOutputMimeTypeUnsupported);
    }

    [Fact]
    public async Task SpeechToText_MissingAudioMimeType_ReturnsCodedInvalidArgument()
    {
        var language = new Mock<ISpeechService>(MockBehavior.Strict);
        var supportedLanguages = CreateSupportedLanguages();
        var service = CreateService(language.Object, supportedLanguages.Object);

        var requestStream = new SequenceAsyncStreamReader<SprechenProto.AudioChunk>(new[]
        {
            new SprechenProto.AudioChunk { Data = ByteString.CopyFrom(new byte[] { 0x1 }), LanguageCode = "de" }
        });

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            service.SpeechToText(requestStream, CreateContext("SpeechToText")));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        AssertErrorCode(ex, SprechenProto.SprechenErrorCode.AudioInputMimeTypeRequired);
    }

    [Fact]
    public async Task SpeechToText_UnsupportedAudioMimeType_ReturnsCodedInvalidArgument()
    {
        var language = new Mock<ISpeechService>(MockBehavior.Strict);
        var supportedLanguages = CreateSupportedLanguages();
        var service = CreateService(language.Object, supportedLanguages.Object);

        var requestStream = new SequenceAsyncStreamReader<SprechenProto.AudioChunk>(new[]
        {
            new SprechenProto.AudioChunk
            {
                Data = ByteString.CopyFrom(new byte[] { 0x1 }),
                MimeType = (SprechenProto.AudioMimeType)999,
                LanguageCode = "de"
            }
        });

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            service.SpeechToText(requestStream, CreateContext("SpeechToText")));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        AssertErrorCode(ex, SprechenProto.SprechenErrorCode.AudioInputMimeTypeUnsupported);
    }

    [Fact]
    public async Task SpeechToText_EmptyAudioStream_ReturnsCodedInvalidArgument()
    {
        var language = new Mock<ISpeechService>(MockBehavior.Strict);
        var supportedLanguages = CreateSupportedLanguages();
        var service = CreateService(language.Object, supportedLanguages.Object);

        var requestStream = new SequenceAsyncStreamReader<SprechenProto.AudioChunk>(Array.Empty<SprechenProto.AudioChunk>());

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            service.SpeechToText(requestStream, CreateContext("SpeechToText")));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        AssertErrorCode(ex, SprechenProto.SprechenErrorCode.AudioInputMimeTypeRequired);
    }

    [Fact]
    public async Task SpeechToText_WhenProviderReturnsNoTranscript_ReturnsEmptyTranscript()
    {
        var language = new Mock<ISpeechService>();
        language.Setup(s => s.TranscribeStreamingAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new SpeechTranscript(string.Empty, null));

        var supportedLanguages = CreateSupportedLanguages();
        var service = CreateService(language.Object, supportedLanguages.Object);
        var measurements = new List<(long Value, Dictionary<string, object?> Tags)>();
        using var meterListener = StartEmptyTranscriptMeterListener(measurements);

        var requestStream = new SequenceAsyncStreamReader<SprechenProto.AudioChunk>(new[]
        {
            new SprechenProto.AudioChunk
            {
                Data = ByteString.CopyFrom(new byte[] { 0x1 }),
                MimeType = SprechenProto.AudioMimeType.AudioPcm,
                LanguageCode = "de"
            }
        });

        var response = await service.SpeechToText(requestStream, CreateContext("SpeechToText"));

        Assert.Equal(string.Empty, response.Text);
        Assert.False(response.HasConfidence);
        Assert.Contains(
            measurements,
            measurement => measurement.Value == 1
                && Equals(measurement.Tags["operation"], "SpeechToText")
                && Equals(measurement.Tags["language_code"], "de"));
    }

    [Fact]
    public async Task SpeechToTextStream_WhenProviderReturnsNoTranscript_WritesEmptyTranscript()
    {
        var language = new Mock<ISpeechService>();
        language.Setup(s => s.StreamConversationAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de"))
            .Returns(EmptyConversationFeed());

        var supportedLanguages = CreateSupportedLanguages();
        var service = CreateService(language.Object, supportedLanguages.Object);
        var measurements = new List<(long Value, Dictionary<string, object?> Tags)>();
        using var meterListener = StartEmptyTranscriptMeterListener(measurements);

        var requestStream = new SequenceAsyncStreamReader<SprechenProto.AudioChunk>(new[]
        {
            new SprechenProto.AudioChunk
            {
                Data = ByteString.CopyFrom(new byte[] { 0x1 }),
                MimeType = SprechenProto.AudioMimeType.AudioPcm,
                LanguageCode = "de"
            }
        });

        var responseWriter = new RecordingServerStreamWriter<SprechenProto.SpeechTranscript>();

        await service.SpeechToTextStream(
            requestStream,
            responseWriter,
            CreateContext("SpeechToTextStream"));

        var response = Assert.Single(responseWriter.Items);
        Assert.Equal(string.Empty, response.Text);
        Assert.False(response.HasConfidence);
        Assert.Contains(
            measurements,
            measurement => measurement.Value == 1
                && Equals(measurement.Tags["operation"], "SpeechToTextStream")
                && Equals(measurement.Tags["language_code"], "de"));
    }

    [Fact]
    public async Task TextToSpeechToStream_WhenProviderThrowsRpcException_WrapsAsProviderFailure()
    {
        var providerFailure = new RpcException(new Status(StatusCode.NotFound, "models/not-found"));

        var language = new Mock<ISpeechService>();
        language.Setup(s => s.SynthesizeAsync(
                "hi",
                AudioMimeType.Pcm,
                It.IsAny<float>(),
                "de",
                It.IsAny<TextToSpeechPolitenessLevel>(),
                It.IsAny<CancellationToken>()))
            .Returns(ThrowingSynthesis(providerFailure));

        var supportedLanguages = CreateSupportedLanguages();
        var service = CreateService(language.Object, supportedLanguages.Object);

        var request = new SprechenProto.TextRequest
        {
            Text = "hi",
            LanguageCode = "de",
            DesiredMimeType = SprechenProto.AudioMimeType.AudioPcm
        };

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            service.TextToSpeechToStream(request, new RecordingServerStreamWriter<SprechenProto.AudioResponse>(), CreateContext("TextToSpeechToStream")));

        Assert.Equal(StatusCode.Internal, ex.StatusCode);
        AssertErrorCode(ex, SprechenProto.SprechenErrorCode.ProviderFailure);
    }

    [Fact]
    public async Task TextToSpeechToStream_WhenProviderRateLimited_ReturnsCodedResourceExhausted()
    {
        var rateLimited = new SpeechProviderRateLimitedException("Deepgram", "TextToSpeechToStream");

        var language = new Mock<ISpeechService>();
        language.Setup(s => s.SynthesizeAsync(
                "hi",
                AudioMimeType.Pcm,
                It.IsAny<float>(),
                "de",
                It.IsAny<TextToSpeechPolitenessLevel>(),
                It.IsAny<CancellationToken>()))
            .Returns(ThrowingSynthesis(rateLimited));

        var supportedLanguages = CreateSupportedLanguages();
        var service = CreateService(language.Object, supportedLanguages.Object);

        var request = new SprechenProto.TextRequest
        {
            Text = "hi",
            LanguageCode = "de",
            DesiredMimeType = SprechenProto.AudioMimeType.AudioPcm
        };

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            service.TextToSpeechToStream(request, new RecordingServerStreamWriter<SprechenProto.AudioResponse>(), CreateContext("TextToSpeechToStream")));

        Assert.Equal(StatusCode.ResourceExhausted, ex.StatusCode);
        AssertErrorCode(ex, SprechenProto.SprechenErrorCode.ProviderRateLimited);
    }

    [Fact]
    public async Task TextToSpeechToStream_WhenProviderReturnsNoAudio_ReturnsCodedProviderFailure()
    {
        var language = new Mock<ISpeechService>();
        language.Setup(s => s.SynthesizeAsync(
                "hi",
                AudioMimeType.Pcm,
                It.IsAny<float>(),
                "de",
                It.IsAny<TextToSpeechPolitenessLevel>(),
                It.IsAny<CancellationToken>()))
            .Returns(EmptySynthesis());

        var supportedLanguages = CreateSupportedLanguages();
        var service = CreateService(language.Object, supportedLanguages.Object);

        var request = new SprechenProto.TextRequest
        {
            Text = "hi",
            LanguageCode = "de",
            DesiredMimeType = SprechenProto.AudioMimeType.AudioPcm
        };

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            service.TextToSpeechToStream(request, new RecordingServerStreamWriter<SprechenProto.AudioResponse>(), CreateContext("TextToSpeechToStream")));

        Assert.Equal(StatusCode.Internal, ex.StatusCode);
        AssertErrorCode(ex, SprechenProto.SprechenErrorCode.ProviderFailure);
    }

    private static SpeechGrpcService CreateService(ISpeechService core, ISupportedLanguagesService supportedLanguages)
    {
        return SpeechGrpcServiceTestFactory.Create(
            core,
            supportedLanguages);
    }

    private static Mock<ISupportedLanguagesService> CreateSupportedLanguages()
    {
        var supportedLanguages = new Mock<ISupportedLanguagesService>(MockBehavior.Strict);
        supportedLanguages.SetupGet(s => s.DefaultIsoCode).Returns("de");
        supportedLanguages
            .SetupGet(s => s.Languages)
            .Returns(new[]
            {
                new SupportedLanguage("de", "Deutsch"),
                new SupportedLanguage("en", "English")
            });
        supportedLanguages
            .Setup(s => s.IsSupportedIsoCode(It.IsAny<string>()))
            .Returns((string code) => string.Equals(code, "de", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(code, "en", StringComparison.OrdinalIgnoreCase));

        return supportedLanguages;
    }

    private static void AssertErrorCode(RpcException exception, SprechenProto.SprechenErrorCode expected)
    {
        Assert.True(SprechenGrpcErrorMetadata.TryGetErrorCode(exception, out var actual));
        Assert.Equal(expected, actual);
    }

    private static MeterListener StartEmptyTranscriptMeterListener(List<(long Value, Dictionary<string, object?> Tags)> measurements)
    {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (string.Equals(instrument.Meter.Name, "Sprechen.Service.SpeechRecognition", StringComparison.Ordinal)
                && string.Equals(instrument.Name, "stt.empty_transcripts_total", StringComparison.Ordinal))
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            var copiedTags = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var tag in tags)
            {
                copiedTags[tag.Key] = tag.Value;
            }

            measurements.Add((value, copiedTags));
        });
        listener.Start();
        return listener;
    }

    private static async IAsyncEnumerable<AudioSynthesisChunk> ThrowingSynthesis(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        await Task.Yield();
        yield return new AudioSynthesisChunk(Array.Empty<byte>(), AudioMimeType.Pcm);
        throw exception;
    }

    private static async IAsyncEnumerable<AudioSynthesisChunk> EmptySynthesis()
    {
        await Task.Yield();
        yield break;
    }

    private static async IAsyncEnumerable<SpeechConversationEvent> EmptyConversationFeed()
    {
        await Task.Yield();
        yield break;
    }

    private static ServerCallContext CreateContext(string method) =>
        TestServerCallContext.Create(
            method,
            null,
            DateTime.UtcNow.AddMinutes(1),
            new Metadata(),
            CancellationToken.None,
            "127.0.0.1",
            null,
            null,
            _ => Task.CompletedTask,
            static () => new WriteOptions(),
            static _ => { });
}
