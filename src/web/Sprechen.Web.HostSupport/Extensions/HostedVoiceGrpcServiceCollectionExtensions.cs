using Grpc.Net.ClientFactory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sprechen.Web.HostSupport.Options;
using Sprechen.Web.HostSupport.Services;
using SpeechGrpcClient = Sprechen.Proto.Speech.SpeechClient;

namespace Sprechen.Web.HostSupport.Extensions;

/// <summary>
/// Registriert den gRPC-Sprachclient und die Weitergabe des API-Schlüssels für den Webhost.
/// </summary>
public static class HostedVoiceGrpcServiceCollectionExtensions
{
    public static IServiceCollection AddHostedVoiceGrpc(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddGrpc();
        services.AddSingleton<ServiceApiKeyClientInterceptor>();

        services.AddGrpcClient<SpeechGrpcClient>((serviceProvider, clientOptions) =>
            {
                var config = serviceProvider.GetRequiredService<IOptions<SpeechServiceOptions>>().Value;
                clientOptions.Address = new Uri(config.BaseAddress);
            })
            .AddInterceptor<ServiceApiKeyClientInterceptor>();

        return services;
    }
}
