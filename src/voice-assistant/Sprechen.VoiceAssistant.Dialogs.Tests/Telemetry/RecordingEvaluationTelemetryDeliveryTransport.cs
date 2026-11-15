using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests;

internal sealed class RecordingEvaluationTelemetryDeliveryTransport : IEvaluationTelemetryDeliveryTransport
{
    private readonly object _gate = new();
    private readonly Queue<Func<string, EvaluationTelemetryDeliveryResult>> _results = [];

    internal List<string> Bodies { get; } = [];

    internal void Enqueue(EvaluationTelemetryDeliveryResult result)
        => Enqueue(_ => result);

    internal void Enqueue(Func<string, EvaluationTelemetryDeliveryResult> result)
    {
        lock (_gate)
        {
            _results.Enqueue(result);
        }
    }

    public Task<EvaluationTelemetryDeliveryResult> DeliverAsync(string body, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Func<string, EvaluationTelemetryDeliveryResult>? result = null;
        lock (_gate)
        {
            Bodies.Add(body);
            if (_results.Count > 0)
            {
                result = _results.Dequeue();
            }
        }

        return Task.FromResult(
            result?.Invoke(body)
            ?? new EvaluationTelemetryDeliveryResult(EvaluationTelemetryDeliveryStatus.Accepted, 204));
    }
}
