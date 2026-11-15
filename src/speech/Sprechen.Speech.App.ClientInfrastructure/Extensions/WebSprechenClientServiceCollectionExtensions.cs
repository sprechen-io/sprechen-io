using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Sprechen.Speech.App.BrowserInfrastructure.Extensions;
using Sprechen.Speech.App.CaptureRuntime.Extensions;
using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;
using Sprechen.Speech.App.GrpcInfrastructure.Extensions;
using Sprechen.Speech.WakeWord.ClientInfrastructure.Extensions;

namespace Sprechen.Speech.App.ClientInfrastructure.Extensions;

/// <summary>
/// Stellt die DI-Zusammenstellungen für Clientgrundfunktionen, gRPC-Anbindung
/// und Sprachinteraktion aus den zuständigen Infrastrukturregistrierungen bereit.
/// </summary>
public static class WebSprechenClientServiceCollectionExtensions
{
    public static IServiceCollection AddWebSprechenClientCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSpeechBrowserInfrastructure();
        services.AddSpeechLanguageState();
        services.TryAddScoped<IFormsCatalogDataClient, FormsCatalogDataClient>();
        services.TryAddScoped<IMdaFormContractDataClient, MdaFormContractDataClient>();
        services.TryAddScoped<IClientMessageBus, ClientMessageBus>();
        services.TryAddScoped<ClientMessageBusSpeechInteractionSignals>();
        services.TryAddScoped<ISpeechInteractionSignals>(sp => sp.GetRequiredService<ClientMessageBusSpeechInteractionSignals>());
        services.TryAddScoped<ISpeechCaptureFeedback>(sp => sp.GetRequiredService<ClientMessageBusSpeechInteractionSignals>());
        services.TryAddScoped<ISpeechCaptureRecordingIndicator>(sp => new ListeningCueRecordingIndicatorDecorator(
            sp.GetRequiredService<ClientMessageBusSpeechInteractionSignals>(),
            sp.GetRequiredService<IEarconPlayer>(),
            sp.GetRequiredService<ILogger<ListeningCueRecordingIndicatorDecorator>>()));
        services.TryAddScoped<ISpeechBargeInSignals>(sp => sp.GetRequiredService<ClientMessageBusSpeechInteractionSignals>());
        services.TryAddScoped<IEarconFamilySignals>(sp => sp.GetRequiredService<ClientMessageBusSpeechInteractionSignals>());
        services.TryAddScoped<ISpeechCaptureStoppedSignals>(sp => sp.GetRequiredService<ClientMessageBusSpeechInteractionSignals>());
        services.TryAddScoped<ISpeechWakeListeningFallbackSignals>(sp => sp.GetRequiredService<ClientMessageBusSpeechInteractionSignals>());
        services.AddSprechenWakeWordTelemetrySink();
        return services;
    }

    public static IServiceCollection AddWebSprechenClientGrpc(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddWebSprechenClientCore();
        services.AddSpeechBrowserCaptureInfrastructure();
        services.AddSpeechCaptureRuntime();
        services.TryAddScoped<IBargeInCoordinator, BargeInCoordinator>();

        return services.AddSpeechGrpcInfrastructure(
            VoiceInterfaceModelCatalog.FirstPcmEnqueueTimeout,
            VoiceInterfaceModelCatalog.FirstPcmTimeoutEarconId);
    }

    public static IServiceCollection AddWebSprechenSpeechClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Aufnahme und Mikrofonsitzung kommen mit der gRPC-Zusammensetzung.
        // Die modellierte Mikrofonsperre verbindet Katalogregel und Browseranbindung
        // mit einem sitzungsbezogenen Beobachter beider Wiedergabezustände.
        // Der Aufnahmekoordinator löst ihn auf, bevor die Aufnahme ihn benötigt.
        services.AddSpeechLanguageCatalogAndSettings();
        services.TryAddScoped<IBargeInCoordinator, BargeInCoordinator>();
        services.AddSpeechCaptureRuntime();
        // Der Erkennungsdienst plant die Transkriptionsanfragen gemeinsam.
        // Produktionsaufrufer sollen ihre Aufträge dort einreihen, damit sie
        // die gemeinsame Planung nicht durch direkte Clientaufrufe umgehen.
        services.AddSingleton<IBackoffPolicy>(_ => new ExponentialBackoffPolicy());
        services.AddScoped<SpeechRecognitionWorker>(sp => new SpeechRecognitionWorker(
            sp.GetRequiredService<ISpeechRecognitionClient>(),
            sp.GetRequiredService<IBackoffPolicy>()));
        services.AddSprechenWakeWordClient();
        services.AddSingleton<EarconGuideCatalog>();
        services.AddScoped<EarconGuideWorkflow>();
        services.AddScoped<IEarconGuideWorkflow>(sp => sp.GetRequiredService<EarconGuideWorkflow>());

        return services;
    }
}
