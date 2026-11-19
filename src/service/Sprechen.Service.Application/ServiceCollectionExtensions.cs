using Microsoft.Extensions.DependencyInjection;
using Monstertacle.SharpQrs;
using Sprechen.Service.Application.Cqrs.LanguageCatalog;
using Sprechen.Service.Application.Cqrs.SpeechConversation;
using Sprechen.Service.Application.Cqrs.SpeechSynthesis;
using Sprechen.Service.Application.Cqrs.SpeechTranscription;

namespace Sprechen.Service.Application;

/// <summary>
/// Registriert die internen Anwendungsfall-Handler sowie Kommando- und Abfrageverteiler.
/// Aufrufer verwenden die öffentlichen Kommando-, Abfrage- und Ergebnistypen.
/// </summary>
public static class UseCaseServiceCollectionExtensions
{
    public static IServiceCollection AddServiceApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<TranscribeSpeechCommandHandler>();
        services.AddSingleton<StreamSpeechTranscriptCommandHandler>();
        services.AddSingleton<StreamSpeechConversationCommandHandler>();
        services.AddSingleton<SynthesizeSpeechCommandHandler>();
        services.AddSingleton<ListSupportedLanguagesQueryHandler>();

        services.AddSingleton<ICommandDispatcher>(provider =>
        {
            var dispatcher = new CommandDispatcher();
            dispatcher.Register<TranscribeSpeechCommand, TranscribeSpeechResult>(
                provider.GetRequiredService<TranscribeSpeechCommandHandler>());
            dispatcher.Register<StreamSpeechTranscriptCommand, IAsyncEnumerable<SpeechConversationEvent>>(
                provider.GetRequiredService<StreamSpeechTranscriptCommandHandler>());
            dispatcher.Register<StreamSpeechConversationCommand, IAsyncEnumerable<SpeechConversationEvent>>(
                provider.GetRequiredService<StreamSpeechConversationCommandHandler>());
            dispatcher.Register<SynthesizeSpeechCommand, IAsyncEnumerable<AudioSynthesisChunk>>(
                provider.GetRequiredService<SynthesizeSpeechCommandHandler>());
            return dispatcher;
        });

        services.AddSingleton<IQueryDispatcher>(provider =>
        {
            var dispatcher = new QueryDispatcher();
            dispatcher.Register<ListSupportedLanguagesQuery, SupportedLanguagesResult>(
                provider.GetRequiredService<ListSupportedLanguagesQueryHandler>());
            return dispatcher;
        });

        return services;
    }
}
