using Google.Protobuf;
using Moq;
using Sprechen.Proto.Transport;
using Sprechen.Service.Grpc;
using Sprechen.Service.Options;
using Sprechen.Service.Tests.Grpc;
using Xunit;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Tests.Grpc.Endpoints;

public sealed class RecognitionProfileServerThreadingTests
{
    [Theory]
    [InlineData(SprechenProto.RecognitionProfile.Spelling, RecognitionProfile.Spelling)]
    [InlineData(SprechenProto.RecognitionProfile.Digits, RecognitionProfile.Digits)]
    [InlineData(SprechenProto.RecognitionProfile.Conversational, RecognitionProfile.Conversational)]
    [InlineData(SprechenProto.RecognitionProfile.Unspecified, RecognitionProfile.Conversational)]
    public async Task SpeechToTextEndpoint_ForwardsWireProfileToSpeechService(
        SprechenProto.RecognitionProfile wireProfile,
        RecognitionProfile expected)
    {
        var captured = (RecognitionProfile?)null;
        var speechToText = new Mock<ISpeechToTextService>(MockBehavior.Strict);
        speechToText
            .Setup(service => service.TranscribeStreamingAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<string?>(),
                It.IsAny<bool>()))
            .Callback((IAsyncEnumerable<byte[]> _, AudioMimeType _, CancellationToken _, string? _, RecognitionProfile p, SpeechKeyterms? _, string? _, bool _) => captured = p)
            .ReturnsAsync(new SpeechTranscript("hallo", 80));
        var endpoint = CreateTextEndpoint(speechToText.Object);

        await endpoint.HandleAsync(
            CreatePcmRequestStream(wireProfile),
            GrpcEndpointTestFactory.CreateContext("SpeechToText"));

        Assert.Equal(expected, captured);
    }

    [Fact]
    public async Task SpeechToTextStreamEndpoint_ForwardsSpellingProfileToConversationalService()
    {
        var captured = await CaptureConversationalProfileAsync(
            language => CreateTranscriptStreamEndpoint(language),
            (endpoint, requestStream) => endpoint.HandleAsync(
                requestStream,
                new RecordingServerStreamWriter<SprechenProto.SpeechTranscript>(),
                GrpcEndpointTestFactory.CreateContext("SpeechToTextStream")),
            SprechenProto.RecognitionProfile.Spelling);

        Assert.Equal(RecognitionProfile.Spelling, captured);
    }

    [Fact]
    public async Task SpeechToConversationStreamEndpoint_ForwardsDigitsProfileToConversationalService()
    {
        var captured = await CaptureConversationalProfileAsync(
            language => CreateConversationStreamEndpoint(language),
            (endpoint, requestStream) => endpoint.HandleAsync(
                requestStream,
                new RecordingServerStreamWriter<SprechenProto.SpeechConversationEventMessage>(),
                GrpcEndpointTestFactory.CreateContext("SpeechToConversationStream")),
            SprechenProto.RecognitionProfile.Digits);

        Assert.Equal(RecognitionProfile.Digits, captured);
    }

    private static async Task<RecognitionProfile?> CaptureConversationalProfileAsync<TEndpoint>(
        Func<IConversationalSpeechToTextService, TEndpoint> createEndpoint,
        Func<TEndpoint, SequenceAsyncStreamReader<SprechenProto.AudioChunk>, Task> handle,
        SprechenProto.RecognitionProfile wireProfile)
    {
        var captured = (RecognitionProfile?)null;
        var language = new Mock<IConversationalSpeechToTextService>(MockBehavior.Strict);
        language
            .Setup(service => service.StreamConversationAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de",
                It.IsAny<RecognitionProfile>()))
            .Callback((IAsyncEnumerable<byte[]> _, AudioMimeType _, CancellationToken _, string? _, RecognitionProfile p) => captured = p)
            .Returns(CompletedConversationStream());

        await handle(createEndpoint(language.Object), CreatePcmRequestStream(wireProfile));
        return captured;
    }

    private static SpeechToTextEndpoint CreateTextEndpoint(ISpeechToTextService speechToText)
    {
        var supportedLanguages = GrpcEndpointTestFactory.CreateSupportedLanguages();
        return new SpeechToTextEndpoint(
            GrpcEndpointTestFactory.CreateTranscribeSpeechDispatcher(speechToText),
            GrpcEndpointTestFactory.CreateRequestValidator(supportedLanguages.Object),
            GrpcEndpointTestFactory.CreateDiagnosticsEmitter(),
            GrpcEndpointTestFactory.CreateExceptionMapper(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SpeechGrpcService>.Instance);
    }

    private static SpeechToTextStreamEndpoint CreateTranscriptStreamEndpoint(IConversationalSpeechToTextService language)
    {
        var supportedLanguages = GrpcEndpointTestFactory.CreateSupportedLanguages();
        return new SpeechToTextStreamEndpoint(
            GrpcEndpointTestFactory.CreateStreamSpeechTranscriptDispatcher(language),
            GrpcEndpointTestFactory.CreateRequestValidator(supportedLanguages.Object),
            GrpcEndpointTestFactory.CreateDiagnosticsEmitter(),
            new SpeechToTextTranscriptStreamWriter(
                GrpcEndpointTestFactory.CreateDiagnosticsEmitter(),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<SpeechGrpcService>.Instance),
            GrpcEndpointTestFactory.CreateExceptionMapper(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SpeechGrpcService>.Instance);
    }

    private static SpeechToConversationStreamEndpoint CreateConversationStreamEndpoint(IConversationalSpeechToTextService language)
    {
        var supportedLanguages = GrpcEndpointTestFactory.CreateSupportedLanguages();
        return new SpeechToConversationStreamEndpoint(
            GrpcEndpointTestFactory.CreateStreamSpeechConversationDispatcher(language),
            GrpcEndpointTestFactory.CreateRequestValidator(supportedLanguages.Object),
            new SpeechConversationEventStreamWriter(GrpcEndpointTestFactory.CreateDiagnosticsEmitter()),
            GrpcEndpointTestFactory.CreateExceptionMapper(),
            Microsoft.Extensions.Options.Options.Create(new ConversationStreamingOptions()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SpeechGrpcService>.Instance);
    }

    private static SequenceAsyncStreamReader<SprechenProto.AudioChunk> CreatePcmRequestStream(
        SprechenProto.RecognitionProfile recognitionProfile)
    {
        var chunk = new SprechenProto.AudioChunk
        {
            Data = ByteString.CopyFrom(new byte[] { 0x1, 0x2 }),
            MimeType = SprechenProto.AudioMimeType.AudioPcm,
            LanguageCode = "de",
        };

        if (recognitionProfile != SprechenProto.RecognitionProfile.Unspecified)
        {
            chunk.RecognitionProfile = recognitionProfile;
        }

        return new SequenceAsyncStreamReader<SprechenProto.AudioChunk>(new[] { chunk });
    }

    private static async IAsyncEnumerable<SpeechConversationEvent> CompletedConversationStream()
    {
        await Task.Yield();
        yield return new SpeechConversationCompletedEvent();
    }
}
