using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.VoiceAssistant.EvaluationTelemetry;

using EvaluationTelemetryImpl = Sprechen.VoiceAssistant.EvaluationTelemetry.EvaluationTelemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests;

internal static class EvaluationTelemetryTestOutboxFactory
{
    internal static EvaluationTelemetryTaskOutbox Create(
        IStudyTelemetryContext studyTelemetryContext,
        ILogger<EvaluationTelemetryImpl> logger,
        IEvaluationTelemetryOutboxStore? store = null,
        IEvaluationTelemetryDeliveryTransport? transport = null,
        IEvaluationTelemetryExclusiveLease? exclusiveLease = null,
        IEvaluationTelemetryRetryDelay? retryDelay = null,
        EvaluationTelemetryOptions? options = null,
        IOperationalProbeSession? operationalProbeSession = null)
    {
        operationalProbeSession ??= new OperationalProbeSession();
        var payloads = new EvaluationSessionTelemetryPayloads();
        var validator = new EvaluationTelemetryCheckpointValidator(
            payloads,
            new EvaluationTelemetryCheckpointEventValidator(
                new TelemetryEnvelopeSchemaValidator()), operationalProbeSession);
        return new EvaluationTelemetryTaskOutbox(
            studyTelemetryContext,
            new EvaluationTelemetryOutboxInfrastructure(
                store ?? new InMemoryEvaluationTelemetryOutboxStore(),
                transport ?? new RecordingEvaluationTelemetryDeliveryTransport(),
                exclusiveLease ?? new ImmediateEvaluationTelemetryExclusiveLease(),
                retryDelay ?? new ImmediateEvaluationTelemetryRetryDelay(),
                Options.Create(options ?? new EvaluationTelemetryOptions { RequireStudyContext = false }),
                logger),
            new EvaluationTelemetryStoredEventFactory(
                new EvaluationTelemetryEnvelopeSerializer(new TelemetryEnvelopeSchemaValidator()),
                payloads),
            validator, operationalProbeSession);
    }
}
