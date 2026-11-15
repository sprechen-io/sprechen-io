using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests;

internal sealed class BlockingEvaluationTelemetryRetryDelay : IEvaluationTelemetryRetryDelay
{
    private readonly TaskCompletionSource _waitObserved =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal Task WaitObserved => _waitObserved.Task;

    public async Task WaitAsync(int attempt, TimeSpan? retryAfter, CancellationToken cancellationToken)
    {
        _waitObserved.TrySetResult();
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }
}
