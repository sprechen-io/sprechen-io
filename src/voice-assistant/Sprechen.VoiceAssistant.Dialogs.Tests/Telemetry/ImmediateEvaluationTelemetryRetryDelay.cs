using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests;

internal sealed class ImmediateEvaluationTelemetryRetryDelay : IEvaluationTelemetryRetryDelay
{
    public Task WaitAsync(int attempt, TimeSpan? retryAfter, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
