using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Registriert die Dienste zur Prüfung und Annahme von Telemetrie
/// mit der Studienbedingung des Hosts.
/// </summary>
public static class EvaluationTelemetryIngestionServiceCollectionExtensions
{
    public static IServiceCollection AddEvaluationTelemetryIngestion(
        this IServiceCollection services,
        StudyCondition ingressCondition)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<TelemetryEnvelopeSchemaValidator>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(new EvaluationTelemetryIngressCondition(ingressCondition));
        services.AddSingleton<EvaluationTelemetryEnvelopeBuilder>();
        services.AddSingleton<IEvaluationTelemetryLedgerRecordValidator, EvaluationTelemetryLedgerRecordValidator>();
        services.AddSingleton<EvaluationTelemetryAcceptedEventLogger>();
        services.AddSingleton<IEvaluationTelemetryEventAcceptanceService, EvaluationTelemetryEventAcceptanceService>();
        return services;
    }
}
