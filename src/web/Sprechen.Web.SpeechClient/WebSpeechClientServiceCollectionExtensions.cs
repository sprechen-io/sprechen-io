using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sprechen.Speech.App.ClientInfrastructure.Extensions;
using Sprechen.VoiceAssistant.Extensions;

namespace Sprechen.Web.SpeechClient;

/// <summary>
/// Bündelt die Dienstregistrierungen für Sprachclient, Dialoge und Oberflächen der Webanwendung.
/// </summary>
public static class WebSpeechClientServiceCollectionExtensions
{
    public static IServiceCollection AddSprechenSpeechWebClient(this IServiceCollection services)
        => AddSprechenSpeechWebClient(services, configuration: null);

    public static IServiceCollection AddSprechenSpeechWebClient(
        this IServiceCollection services,
        IConfiguration? configuration)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddWebSprechenClientGrpc()
            .AddWebSprechenSpeechClient()
            .AddWebSprechenSpeechAssistantSurfaces()
            .AddWebSprechenDialogs(configuration)
            .AddWebSprechenSpeechAssistantSurfaceControllers();

        return services;
    }
}
