using Microsoft.Extensions.DependencyInjection;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.Voice.EvaluationTelemetry.BrowserInfrastructure;

/// <summary>
/// Registriert Browserspeicher, HTTP-Versand und Sperren für die
/// Telemetrieübertragung in der WebAssembly-App.
/// </summary>
public static class VoiceEvaluationTelemetryServiceCollectionExtensions
{
    public static IServiceCollection AddVoiceEvaluationTelemetryDelivery(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IOperationalProbeBootstrapClient, HttpOperationalProbeBootstrapClient>();
        services.AddScoped<IEvaluationTelemetryOutboxStore, BrowserEvaluationTelemetryOutboxStore>();
        services.AddScoped<IEvaluationTelemetryDeliveryTransport, HttpEvaluationTelemetryDeliveryTransport>();
        services.AddScoped<IEvaluationTelemetryExclusiveLease, BrowserEvaluationTelemetryExclusiveLease>();
        return services;
    }
}
