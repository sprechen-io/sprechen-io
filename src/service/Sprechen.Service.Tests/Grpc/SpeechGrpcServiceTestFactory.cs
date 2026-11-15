using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Common.Cqrs;
using Sprechen.Service.Application;
using Sprechen.Service.Application.Cqrs.LanguageCatalog;
using Sprechen.Service.Application.Cqrs.SpeechConversation;
using Sprechen.Service.Application.Cqrs.SpeechSynthesis;
using Sprechen.Service.Application.Cqrs.SpeechTranscription;
using Sprechen.Service.Grpc;
using Sprechen.Service.Options;

namespace Sprechen.Service.Tests.Grpc;

internal static class SpeechGrpcServiceTestFactory
{
    public static SpeechGrpcService Create(
        ISpeechService core,
        ISupportedLanguagesService supportedLanguages)
    {
        return Create(
            core,
            supportedLanguages,
            new SpeechProviderOptions(),
            new AudioPacingOptions(),
            new ConversationStreamingOptions(),
            NullLogger<SpeechGrpcService>.Instance);
    }

    public static SpeechGrpcService Create(
        ISpeechService core,
        ISupportedLanguagesService supportedLanguages,
        AudioPacingOptions audioPacingOptions)
    {
        return Create(
            core,
            supportedLanguages,
            new SpeechProviderOptions(),
            audioPacingOptions,
            new ConversationStreamingOptions(),
            NullLogger<SpeechGrpcService>.Instance);
    }

    public static SpeechGrpcService Create(
        ISpeechService core,
        ISupportedLanguagesService supportedLanguages,
        ILogger<SpeechGrpcService> logger)
    {
        return Create(
            core,
            supportedLanguages,
            new SpeechProviderOptions(),
            new AudioPacingOptions(),
            new ConversationStreamingOptions(),
            logger);
    }

    public static SpeechGrpcService Create(
        ISpeechService core,
        ISupportedLanguagesService supportedLanguages,
        ConversationStreamingOptions conversationStreamingOptions,
        ILogger<SpeechGrpcService> logger)
    {
        return Create(
            core,
            supportedLanguages,
            new SpeechProviderOptions(),
            new AudioPacingOptions(),
            conversationStreamingOptions,
            logger);
    }

    public static SpeechGrpcService Create(
        ISpeechService core,
        ISupportedLanguagesService supportedLanguages,
        SpeechProviderOptions speechProviderOptions,
        AudioPacingOptions audioPacingOptions,
        ConversationStreamingOptions conversationStreamingOptions,
        ILogger<SpeechGrpcService> logger)
    {
        ArgumentNullException.ThrowIfNull(core);
        ArgumentNullException.ThrowIfNull(supportedLanguages);
        ArgumentNullException.ThrowIfNull(speechProviderOptions);
        ArgumentNullException.ThrowIfNull(audioPacingOptions);
        ArgumentNullException.ThrowIfNull(conversationStreamingOptions);
        ArgumentNullException.ThrowIfNull(logger);

        var languageResolver = new LanguageResolver(supportedLanguages);
        var requestValidator = new SpeechRequestValidator(languageResolver);
        var speechProvider = Microsoft.Extensions.Options.Options.Create(speechProviderOptions);
        var diagnostics = new SpeechRecognitionDiagnosticsEmitter(speechProvider);
        var synthesisDiagnostics = new SpeechSynthesisDiagnosticsEmitter(
            speechProvider,
            logger);
        var exceptionMapper = new ProviderExceptionToRpcMapper(logger);
        var transcriptStreamWriter = new SpeechToTextTranscriptStreamWriter(diagnostics, logger);
        var conversationEventStreamWriter = new SpeechConversationEventStreamWriter(diagnostics);
        var textToSpeechAudioStreamWriter = new TextToSpeechAudioStreamWriter(
            Microsoft.Extensions.Options.Options.Create(audioPacingOptions),
            synthesisDiagnostics,
            logger);
        var commandDispatcher = new CommandDispatcher();
        commandDispatcher.Register<TranscribeSpeechCommand, TranscribeSpeechResult>(
            new TranscribeSpeechCommandHandler(core, NullLogger<TranscribeSpeechCommandHandler>.Instance));
        commandDispatcher.Register<StreamSpeechTranscriptCommand, IAsyncEnumerable<SpeechConversationEvent>>(
            new StreamSpeechTranscriptCommandHandler(core));
        commandDispatcher.Register<StreamSpeechConversationCommand, IAsyncEnumerable<SpeechConversationEvent>>(
            new StreamSpeechConversationCommandHandler(core));
        commandDispatcher.Register<SynthesizeSpeechCommand, IAsyncEnumerable<AudioSynthesisChunk>>(
            new SynthesizeSpeechCommandHandler(core));

        var queryDispatcher = new QueryDispatcher();
        queryDispatcher.Register<ListSupportedLanguagesQuery, SupportedLanguagesResult>(
            new ListSupportedLanguagesQueryHandler(supportedLanguages));

        var speechToTextEndpoint = new SpeechToTextEndpoint(
            commandDispatcher,
            requestValidator,
            diagnostics,
            exceptionMapper,
            logger);
        var speechToTextStreamEndpoint = new SpeechToTextStreamEndpoint(
            commandDispatcher,
            requestValidator,
            diagnostics,
            transcriptStreamWriter,
            exceptionMapper,
            logger);
        var speechToConversationStreamEndpoint = new SpeechToConversationStreamEndpoint(
            commandDispatcher,
            requestValidator,
            conversationEventStreamWriter,
            exceptionMapper,
            Microsoft.Extensions.Options.Options.Create(conversationStreamingOptions),
            logger);
        var textToSpeechToStreamEndpoint = new TextToSpeechToStreamEndpoint(
            commandDispatcher,
            requestValidator,
            exceptionMapper,
            speechProvider,
            textToSpeechAudioStreamWriter,
            logger);

        return new SpeechGrpcService(
            queryDispatcher,
            speechToTextEndpoint,
            speechToTextStreamEndpoint,
            speechToConversationStreamEndpoint,
            textToSpeechToStreamEndpoint);
    }
}
