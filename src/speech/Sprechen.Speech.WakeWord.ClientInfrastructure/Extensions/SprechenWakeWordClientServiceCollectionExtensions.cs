using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sprechen.Speech.App.Contracts.Evaluation;
using Sprechen.Speech.WakeWord.ClientInfrastructure.Services;
using Sprechen.Speech.WakeWord.Contracts;

namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Extensions;

/// <summary>
/// Registriert Aktivierungsdiagnose getrennt von Erkennung und Einrichtung,
/// damit der gemeinsame Client auch ohne vollständigen Einrichtungsablauf Telemetrie auflösen kann.
/// </summary>
public static class SprechenWakeWordClientServiceCollectionExtensions
{
    public static IServiceCollection AddSprechenWakeWordTelemetrySink(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Ohne registrierte Studienauswertung hält die wirkungslose Implementierung
        // den gemeinsamen Client auflösbar. Eine vorhandene Auswertung bleibt erhalten.
        services.TryAddScoped<IEvaluationWakeWordTelemetry, NoOpEvaluationWakeWordTelemetry>();

        services.AddScoped<WakeWordTelemetryForwardingSink>();
        services.AddScoped<WakeWordEvaluationTelemetryProjection>();
        services.AddScoped<IWakeWordTelemetrySink, WakeWordTelemetryBroadcastSink>();

        return services;
    }

    public static IServiceCollection AddSprechenWakeWordClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IWakeWordProvider, WakeWordProvider>();
        services.AddScoped<IWakeWordDetectorProfileSelector, FirstProfileSelector>();
        services.AddScoped<IAcousticWakeWordDetectorConfigurationResolver, AcousticWakeWordDetectorConfigurationResolver>();
        services.AddScoped<IAcousticWakeWordScorer, UnavailableAcousticWakeWordScorer>();
        services.AddScoped<IAcousticWakeWordTriggerPolicy, ThresholdAcousticWakeWordTriggerPolicy>();
        services.AddScoped<WakeWordSettingsWorkflow>();
        services.AddScoped<IWakeWordSettingsWorkflow>(sp => sp.GetRequiredService<WakeWordSettingsWorkflow>());
        services.AddScoped<IWakeWordSetupScenarioFactory, WakeWordSetupScenarioFactory>();
        services.AddScoped<IWakeWordSetupSpeechInteractor, WakeWordSetupSpeechInteractor>();
        services.AddScoped<IWakeWordSpeechSetupRunner, WakeWordSetupWorkflow>();

        return services;
    }
}
