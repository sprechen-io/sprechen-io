using Microsoft.Extensions.DependencyInjection;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.App.Runtime.Services;
using Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.WakeRuntime.Extensions;

/// <summary>
/// Registriert die Aktivierungserkennung und ihre Abhängigkeiten für den Sprachassistenten.
/// </summary>
public static class SprechenSpeechAssistantWakeRuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddSprechenSpeechAssistantWakeRuntime(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IWakeArbitrator, WakeWordArbitrator>();
        // Die Instanz bewahrt Fehlerfolge, erfolgreiche Aktivierung, Zeitanker und
        // Hinweiszähler über Zuhörläufe im selben DI-Gültigkeitsbereich.
        // Fehlerbehandlung und Koordinator verwenden so denselben Zustand.
        services.AddScoped<WakeRecoveryGuidanceGate>();
        services.AddScoped<WakeListeningFallbackStatePublisher>();
        services.AddScoped<WakeWordLexicalActivationDispatcher>();
        services.AddScoped<WakeWordMissOutcomeHandler>();
        services.AddScoped<WakeAcousticDetectionPath>();
        // Auch die Mikrofonhinweise teilen ihren Zustand innerhalb des
        // DI-Gültigkeitsbereichs über mehrere Zuhörläufe. Der Beginn eines neuen
        // Laufs entfernt den Zeitanker. Fehlerfolge und bestätigte Ansagen bleiben erhalten.
        services.AddScoped<MicrophoneAvailabilityGate>();
        services.AddScoped<WakeCaptureUnavailableHandler>();
        // Eine eigene Regel für Wiederholungsabstände hält den Aktivierungsablauf unabhängig
        // von der Registrierung des Erkennungsworkers. Wachsendes Warten verhindert
        // enge Schleifen bei sofortigen Fehlern.
        services.AddScoped<LexicalWakeListenerCoordinator>(provider => new LexicalWakeListenerCoordinator(
            provider.GetRequiredService<IWakeArbitrator>(),
            provider.GetRequiredService<WakeAcousticDetectionPath>(),
            provider.GetRequiredService<WakeWordLexicalActivationDispatcher>(),
            provider.GetRequiredService<WakeWordMissOutcomeHandler>(),
            provider.GetRequiredService<WakeRecoveryGuidanceGate>(),
            new ExponentialBackoffPolicy(),
            new TaskWakeCaptureRetryPacer(),
            provider.GetRequiredService<MicrophoneAvailabilityGate>(),
            provider.GetRequiredService<WakeCaptureUnavailableHandler>()));
        services.AddScoped<WakeWordRuntime>(provider => new WakeWordRuntime(
            provider.GetRequiredService<LexicalWakeListenerCoordinator>(),
            provider.GetRequiredService<WakeListeningFallbackStatePublisher>()));
        return services;
    }
}
