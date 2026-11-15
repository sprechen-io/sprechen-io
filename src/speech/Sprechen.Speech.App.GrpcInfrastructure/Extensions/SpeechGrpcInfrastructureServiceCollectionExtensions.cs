using Grpc.Net.Client;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using SpeechGrpcClient = Sprechen.Proto.Speech.SpeechClient;

namespace Sprechen.Speech.App.GrpcInfrastructure.Extensions;

/// <summary>
/// Registriert gRPC-Kanal und Sprachclients sowie die Abhängigkeiten
/// für Spracherkennung, Sprachbestätigung und PCM-Sprachausgabe.
/// </summary>
public static class SpeechGrpcInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddSpeechGrpcInfrastructure(
        this IServiceCollection services,
        TimeSpan firstPcmEnqueueTimeout,
        EarconId firstPcmTimeoutEarconId)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped(serviceProvider =>
        {
            var navigationManager = serviceProvider.GetRequiredService<NavigationManager>();
            var baseAddress = new Uri(navigationManager.BaseUri, UriKind.Absolute);
            return GrpcWebChannelFactory.Create(baseAddress);
        });

        services.AddScoped(serviceProvider => new SpeechGrpcClient(serviceProvider.GetRequiredService<GrpcChannel>()));

        services.AddScoped<SupportedLanguagesClient>();
        services.AddScoped<ISupportedLanguagesClient>(serviceProvider => serviceProvider.GetRequiredService<SupportedLanguagesClient>());
        // Sprachausgabe und Erkennung erhalten im selben Gültigkeitsbereich dieselbe
        // Sitzungsidentität, damit ihre Protokolle über diese Kennung zugeordnet werden können.
        services.AddScoped<ISpeechSessionIdentity, SpeechSessionIdentity>();
        services.AddScoped<SpeechRecognitionUnaryResponseMapper>();
        services.AddScoped<SpeechRecognitionClient>();
        services.AddScoped<ISpeechRecognitionClient>(serviceProvider => serviceProvider.GetRequiredService<SpeechRecognitionClient>());
        services.AddScoped<ISpeechToTextStreamingClient, SpeechToTextStreamingClient>();
        services.AddScoped<ILanguageConfirmationStreamingClient, LanguageConfirmationStreamingClient>();
        services.AddScoped<PcmAudioPlaybackLifecycle>();
        services.AddScoped<TextToSpeechPcmStreamPlayer>();
        services.AddScoped<TextToSpeechRequestFactory>();
        services.AddScoped(serviceProvider => ActivatorUtilities.CreateInstance<TextToSpeechEarconFeedback>(
            serviceProvider, firstPcmTimeoutEarconId));
        services.AddScoped<TextToSpeechPlaybackCancellationState>();
        services.AddSingleton<TtsOutputLoudnessMakeup>();
        services.AddScoped(serviceProvider => ActivatorUtilities.CreateInstance<TextToSpeechPlaybackRunner>(
            serviceProvider, firstPcmEnqueueTimeout));
        services.AddScoped<ITextToSpeechPlayer, TextToSpeechPlayer>();

        return services;
    }
}
