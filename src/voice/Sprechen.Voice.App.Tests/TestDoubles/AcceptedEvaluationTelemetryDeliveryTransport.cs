using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.Voice.App.Tests.TestDoubles;

internal sealed class AcceptedEvaluationTelemetryDeliveryTransport : IEvaluationTelemetryDeliveryTransport
{
    public Task<EvaluationTelemetryDeliveryResult> DeliverAsync(string body, CancellationToken cancellationToken)
        => Task.FromResult(new EvaluationTelemetryDeliveryResult(EvaluationTelemetryDeliveryStatus.Accepted, 204));
}
