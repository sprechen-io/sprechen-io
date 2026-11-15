using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Registriert die Telemetriedienste für Sitzungs-, Formular- und Befehlsereignisse des Sprachassistenten.
/// </summary>
public static class WebSprechenSpeechAssistantTelemetryServiceCollectionExtensions
{
    public static IServiceCollection AddWebSprechenSpeechAssistantTelemetry(this IServiceCollection services)
        => AddWebSprechenSpeechAssistantTelemetry(services, configuration: null);

    public static IServiceCollection AddWebSprechenSpeechAssistantTelemetry(
        this IServiceCollection services,
        IConfiguration? configuration)
    {
        ArgumentNullException.ThrowIfNull(services);

        var optionsBuilder = services.AddOptions<EvaluationTelemetryOptions>();
        if (configuration is not null)
        {
            optionsBuilder.Bind(configuration.GetSection(EvaluationTelemetryOptions.SectionName));
        }

        services.AddScoped<EvaluationTelemetrySessionStateStore>();
        services.AddScoped<EvaluationSessionTelemetryPayloads>();
        services.AddSingleton<TelemetryEnvelopeSchemaValidator>();
        services.AddScoped<EvaluationTelemetryEnvelopeSerializer>();
        services.AddScoped<EvaluationTelemetryCheckpointEventValidator>();
        services.AddScoped<EvaluationTelemetryCheckpointValidator>();
        services.AddScoped<EvaluationTelemetryStoredEventFactory>();
        services.AddScoped<IEvaluationTelemetryRetryDelay, EvaluationTelemetryRetryDelay>();
        services.AddScoped(static serviceProvider => new EvaluationTelemetryOutboxInfrastructure(
            serviceProvider.GetRequiredService<IEvaluationTelemetryOutboxStore>(),
            serviceProvider.GetRequiredService<IEvaluationTelemetryDeliveryTransport>(),
            serviceProvider.GetRequiredService<IEvaluationTelemetryExclusiveLease>(),
            serviceProvider.GetRequiredService<IEvaluationTelemetryRetryDelay>(),
            serviceProvider.GetRequiredService<IOptions<EvaluationTelemetryOptions>>(),
            serviceProvider.GetRequiredService<ILogger<EvaluationTelemetry>>()));
        services.AddScoped<EvaluationTelemetryTaskOutbox>();
        services.AddScoped<EvaluationTelemetryEnvelopeEmitter>();
        services.AddScoped<EvaluationFieldTelemetryPayloads>();
        services.AddScoped<EvaluationStatusTelemetryPayloads>();
        services.AddScoped<EvaluationCommandTelemetryPayloads>();
        services.AddScoped<EvaluationConversationalPacingTelemetryPayloads>();
        services.AddScoped<EvaluationConversationalPacingTelemetry>();
        services.AddScoped<EvaluationTelemetryDetailedStatusChangeDetector>();
        services.AddScoped<EvaluationSessionTelemetry>();
        services.AddScoped<EvaluationFormTelemetry>();
        services.AddScoped<EvaluationCommandTelemetry>();
        services.AddScoped<EvaluationTelemetry>();
        services.AddScoped<IEvaluationTelemetry>(static serviceProvider => serviceProvider.GetRequiredService<EvaluationTelemetry>());
        services.AddScoped<IEvaluationSessionTelemetry>(static serviceProvider => serviceProvider.GetRequiredService<EvaluationSessionTelemetry>());
        services.AddScoped<IEvaluationFormTelemetry>(static serviceProvider => serviceProvider.GetRequiredService<EvaluationFormTelemetry>());
        services.AddScoped<IEvaluationCommandTelemetry>(static serviceProvider => serviceProvider.GetRequiredService<EvaluationCommandTelemetry>());

        // Die Ersetzung verhindert, dass eine Aufzählung der Empfänger zusätzlich die leere Implementierung aufruft.
        services.Replace(ServiceDescriptor.Scoped<IEvaluationWakeWordTelemetry, EvaluationWakeWordTelemetry>());
        return services;
    }
}
