using Grpc.Core;
using Grpc.Core.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Common.Cqrs;
using Sprechen.Service.Application;
using Sprechen.Service.Application.Cqrs.LanguageCatalog;
using Sprechen.Service.Application.Cqrs.SpeechConversation;
using Sprechen.Service.Application.Cqrs.SpeechSynthesis;
using Sprechen.Service.Application.Cqrs.SpeechTranscription;
using Sprechen.Service.Grpc;
using Sprechen.Service.Options;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Tests.Grpc.Endpoints;

internal static class GrpcEndpointTestFactory
{
    public static Mock<ISupportedLanguagesService> CreateSupportedLanguages()
    {
        var supportedLanguages = new Mock<ISupportedLanguagesService>(MockBehavior.Strict);
        supportedLanguages.SetupGet(service => service.DefaultIsoCode).Returns("de");
        supportedLanguages
            .SetupGet(service => service.Languages)
            .Returns(new[]
            {
                new SupportedLanguage("de", "Deutsch"),
                new SupportedLanguage("en", "English")
            });
        supportedLanguages
            .Setup(service => service.IsSupportedIsoCode(It.IsAny<string>()))
            .Returns((string code) => string.Equals(code, "de", StringComparison.OrdinalIgnoreCase)
                                     || string.Equals(code, "en", StringComparison.OrdinalIgnoreCase));

        return supportedLanguages;
    }

    public static SpeechRequestValidator CreateRequestValidator(ISupportedLanguagesService supportedLanguages)
        => new(new LanguageResolver(supportedLanguages));

    public static SpeechRecognitionDiagnosticsEmitter CreateDiagnosticsEmitter()
        => CreateDiagnosticsEmitter(NullLogger<SpeechGrpcService>.Instance);

    public static SpeechRecognitionDiagnosticsEmitter CreateDiagnosticsEmitter(ILogger<SpeechGrpcService> logger)
        => new(Microsoft.Extensions.Options.Options.Create(new SpeechProviderOptions()));

    public static ProviderExceptionToRpcMapper CreateExceptionMapper()
        => CreateExceptionMapper(NullLogger<SpeechGrpcService>.Instance);

    public static ProviderExceptionToRpcMapper CreateExceptionMapper(ILogger<SpeechGrpcService> logger)
        => new(logger);

    public static ICommandDispatcher CreateTranscribeSpeechDispatcher(ISpeechToTextService speechToText)
    {
        var dispatcher = new CommandDispatcher();
        dispatcher.Register<TranscribeSpeechCommand, TranscribeSpeechResult>(
            new TranscribeSpeechCommandHandler(speechToText, NullLogger<TranscribeSpeechCommandHandler>.Instance));
        return dispatcher;
    }

    public static ICommandDispatcher CreateStreamSpeechTranscriptDispatcher(IConversationalSpeechToTextService conversational)
    {
        var dispatcher = new CommandDispatcher();
        dispatcher.Register<StreamSpeechTranscriptCommand, IAsyncEnumerable<SpeechConversationEvent>>(
            new StreamSpeechTranscriptCommandHandler(conversational));
        return dispatcher;
    }

    public static ICommandDispatcher CreateStreamSpeechConversationDispatcher(IConversationalSpeechToTextService conversational)
    {
        var dispatcher = new CommandDispatcher();
        dispatcher.Register<StreamSpeechConversationCommand, IAsyncEnumerable<SpeechConversationEvent>>(
            new StreamSpeechConversationCommandHandler(conversational));
        return dispatcher;
    }

    public static ICommandDispatcher CreateSynthesizeSpeechDispatcher(ITextToSpeechService textToSpeech)
    {
        var dispatcher = new CommandDispatcher();
        dispatcher.Register<SynthesizeSpeechCommand, IAsyncEnumerable<AudioSynthesisChunk>>(
            new SynthesizeSpeechCommandHandler(textToSpeech));
        return dispatcher;
    }

    public static IQueryDispatcher CreateListSupportedLanguagesDispatcher(ISupportedLanguagesService supportedLanguages)
    {
        var dispatcher = new QueryDispatcher();
        dispatcher.Register<ListSupportedLanguagesQuery, SupportedLanguagesResult>(
            new ListSupportedLanguagesQueryHandler(supportedLanguages));
        return dispatcher;
    }

    public static SequenceAsyncStreamReader<SprechenProto.AudioChunk> CreatePcmRequestStream()
    {
        return new SequenceAsyncStreamReader<SprechenProto.AudioChunk>(
            new[]
            {
                new SprechenProto.AudioChunk
                {
                    Data = Google.Protobuf.ByteString.CopyFrom(new byte[] { 0x1, 0x2 }),
                    MimeType = SprechenProto.AudioMimeType.AudioPcm,
                    LanguageCode = "de",
                }
            });
    }

    public static ServerCallContext CreateContext(string method)
        => CreateContext(method, CancellationToken.None);

    public static ServerCallContext CreateContext(string method, CancellationToken cancellationToken)
    {
        return TestServerCallContext.Create(
            method,
            null,
            DateTime.UtcNow.AddMinutes(1),
            new Metadata(),
            cancellationToken,
            "127.0.0.1",
            null,
            null,
            _ => Task.CompletedTask,
            static () => new WriteOptions(),
            static _ => { });
    }
}
