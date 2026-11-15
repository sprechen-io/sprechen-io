using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests;

internal sealed class BlockingPredecessorEvaluationTelemetryDeliveryTransport : IEvaluationTelemetryDeliveryTransport
{
    private readonly TaskCompletionSource<EvaluationTelemetryDeliveryResult> _predecessor =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _predecessorObserved =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal List<string> Bodies { get; } = [];

    internal Task PredecessorObserved => _predecessorObserved.Task;

    public Task<EvaluationTelemetryDeliveryResult> DeliverAsync(
        string body,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Bodies.Add(body);
        using var document = JsonDocument.Parse(body);
        var eventName = document.RootElement
            .GetProperty(TelemetryEnvelopeFieldNames.Event)
            .GetString();
        if (eventName is TelemetryEventNames.TaskStart or TelemetryEventNames.TaskEnd)
        {
            return Task.FromResult(Accepted());
        }

        _predecessorObserved.TrySetResult();
        return _predecessor.Task;
    }

    internal void AcceptPredecessor() => _predecessor.TrySetResult(Accepted());

    private static EvaluationTelemetryDeliveryResult Accepted()
        => new(EvaluationTelemetryDeliveryStatus.Accepted, 204);
}
