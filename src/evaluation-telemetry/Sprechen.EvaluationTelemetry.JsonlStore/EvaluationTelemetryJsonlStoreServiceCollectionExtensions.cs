using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sprechen.EvaluationTelemetry.Ingestion;

namespace Sprechen.EvaluationTelemetry.JsonlStore;

/// <summary>
/// Bindet den JSONL-Speicher an den Host, damit Aufnahme und Archiv denselben
/// Adapter nutzen.
/// </summary>
public static class EvaluationTelemetryJsonlStoreServiceCollectionExtensions
{
    public static IServiceCollection AddEvaluationTelemetryJsonlStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<EvaluationTelemetryJsonlStoreOptions>()
            .Bind(configuration.GetSection(EvaluationTelemetryJsonlStoreOptions.SectionName))
            .Validate(
                static options => !string.IsNullOrWhiteSpace(options.DataDirectory),
                "The evaluation telemetry JSONL data directory must not be empty.")
            .ValidateOnStart();
        services.AddSingleton<EvaluationTelemetryJsonlStoreAdapter>();
        services.AddSingleton<IEvaluationTelemetryEventStore>(serviceProvider =>
            serviceProvider.GetRequiredService<EvaluationTelemetryJsonlStoreAdapter>());
        services.AddSingleton<IEvaluationTelemetryArchiveReader>(serviceProvider =>
            serviceProvider.GetRequiredService<EvaluationTelemetryJsonlStoreAdapter>());
        return services;
    }
}
