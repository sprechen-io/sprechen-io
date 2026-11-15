using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sprechen.Contracts.ApiClient;
using Sprechen.Speech.App.MicrophoneRecording.Extensions;

namespace Sprechen.Speech.App.BrowserInfrastructure.Extensions;

/// <summary>
/// Registriert Browseradapter und ihre gemeinsamen Sitzungsinstanzen,
/// damit Mikrofonzugriff, Sperrsteuerung und Diagnose dieselben Zuständigkeiten verwenden.
/// </summary>
public static class SpeechBrowserInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddSpeechBrowserInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped(serviceProvider =>
        {
            var navigationManager = serviceProvider.GetRequiredService<NavigationManager>();
            return new HttpClient { BaseAddress = new Uri(navigationManager.BaseUri, UriKind.Absolute) };
        });
        services.AddContractsApiClient(
            "ContractsHttp",
            (serviceProvider, httpClient) =>
            {
                var navigationManager = serviceProvider.GetRequiredService<NavigationManager>();
                httpClient.BaseAddress = new Uri(navigationManager.BaseUri, UriKind.Absolute);
            });

        services.AddScoped<IUserLanguageSettingsStore, UserLanguageSettingsStore>();
        services.AddScoped<IBrowserLanguageProvider, BrowserLanguageProvider>();
        services.AddScoped<IBrowserDebugConsole, BrowserDebugConsole>();
        services.TryAddScoped<ISpeechSessionIdentity, BrowserSpeechSessionIdentity>();
        services.AddScoped<IDiagnosticOperatingMode, DiagnosticOperatingMode>();
        // Der Diagnosemodus gibt Aufnahmeberichte frei. Sonst gilt die Gerätefreigabe,
        // die nur bei ausdrücklicher Deaktivierung sperrt. Die Nutzlast enthält kein erkanntes
        // Transkript. Der Host protokolliert zusätzlich nur ausdrücklich zugelassene Felder.
        services.AddScoped<ICaptureDiagnosticsReporter>(serviceProvider => new HttpCaptureDiagnosticsReporter(
            serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("ContractsHttp"),
            serviceProvider.GetRequiredService<IBrowserDebugConsole>(),
            serviceProvider.GetRequiredService<IDiagnosticOperatingMode>(),
            serviceProvider.GetService<ICaptureTrackProcessingReader>()));
        // Aktivierungsfehler bleiben ohne zuvor gesetzte Gerätefreigabe diagnostizierbar.
        // Die Nutzlast enthält kein erkanntes Transkript. Der Host protokolliert
        // zusätzlich nur ausdrücklich zugelassene Felder.
        services.AddScoped<IWakeMissDiagnosticsReporter>(serviceProvider => new HttpWakeMissDiagnosticsReporter(
            serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("ContractsHttp"),
            serviceProvider.GetRequiredService<IDiagnosticOperatingMode>()));
        services.AddScoped<BrowserDebugConsoleSpeechClientTrace>();
        services.AddScoped<ISpeechClientTrace>(serviceProvider => serviceProvider.GetRequiredService<BrowserDebugConsoleSpeechClientTrace>());
        services.AddScoped<ISpeechAssistantTrace, BrowserDebugConsoleSpeechAssistantTrace>();
        services.AddScoped<IDialogDecisionSink>(serviceProvider =>
        {
            var operatingMode = serviceProvider.GetRequiredService<IDiagnosticOperatingMode>();
            var httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("ContractsHttp");
            return new CompositeDialogDecisionSink(
                new SpeechAssistantTraceDialogDecisionSink(
                    serviceProvider.GetRequiredService<ISpeechAssistantTrace>(),
                    operatingMode.ClientCorrelationId),
                new HttpDialogDecisionDiagnosticsReporter(httpClient, operatingMode));
        });
        services.AddScoped<IVoiceDialogSettingsOverrideStore, VoiceDialogSettingsOverrideStore>();
        services.AddScoped<IVoiceActivationSettings, VoiceActivationSettings>();
        services.AddScoped<IAudioOutputSettings, AudioOutputSettings>();
        services.AddScoped<IThemeSettings, ThemeSettings>();
        services.AddScoped<SpeechCaptureSettings>();
        services.AddScoped<ISpeechCaptureSilenceDebounceSettings>(serviceProvider => serviceProvider.GetRequiredService<SpeechCaptureSettings>());
        services.AddScoped<ISpeechCaptureAmplitudeThresholdSettings>(serviceProvider => serviceProvider.GetRequiredService<SpeechCaptureSettings>());
        services.AddScoped<IBootStatusBrowserBridge, BootStatusBrowserBridge>();

        return services;
    }

    public static IServiceCollection AddSpeechBrowserCaptureInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IEarconPlayer, EarconPlayer>();
        services.TryAddScoped<IPcmAudioPlayer, PcmAudioPlayer>();
        services.AddMicrophoneRecording();
        services.TryAddScoped<IUserSpeechBargeInHandler, UserSpeechBargeInHandler>();
        services.TryAddScoped<IMicGatePolicyResolver, MicGatePolicyResolver>();
        services.TryAddScoped<MicGateLifecycleObserver>();
        services.TryAddScoped<IMicGateState>(serviceProvider => serviceProvider.GetRequiredService<MicGateLifecycleObserver>());

        return services;
    }
}
