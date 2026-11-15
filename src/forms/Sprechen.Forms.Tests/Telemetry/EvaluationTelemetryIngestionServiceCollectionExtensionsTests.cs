using Microsoft.Extensions.DependencyInjection;
using Sprechen.EvaluationTelemetry.Ingestion;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.Forms.Tests.Telemetry;

public sealed class EvaluationTelemetryIngestionServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEvaluationTelemetryIngestion_RegistersCanonicalAcceptanceAndValidationServices()
    {
        var services = new ServiceCollection();

        services.AddEvaluationTelemetryIngestion(StudyCondition.A);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IEvaluationTelemetryEventAcceptanceService)
                && descriptor.Lifetime == ServiceLifetime.Singleton);
        var provider = services.BuildServiceProvider();
        Assert.Equal(
            StudyCondition.A,
            provider.GetRequiredService<EvaluationTelemetryIngressCondition>().Value);
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(TelemetryEnvelopeSchemaValidator)
                && descriptor.Lifetime == ServiceLifetime.Singleton);
    }
}
