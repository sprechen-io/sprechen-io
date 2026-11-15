using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class RecordingEvaluationTelemetryDeliveryTransport : IEvaluationTelemetryDeliveryTransport
{
    internal List<string> Bodies { get; } = [];

    public Task<EvaluationTelemetryDeliveryResult> DeliverAsync(string body, CancellationToken cancellationToken)
    {
        Bodies.Add(body);
        return Task.FromResult(new EvaluationTelemetryDeliveryResult(EvaluationTelemetryDeliveryStatus.Accepted, 204));
    }
}
