using Sprechen.EvaluationTelemetry.Ingestion;

namespace Sprechen.Web.HostSupport.Tests.Endpoints;

internal sealed class BlockingEvaluationTelemetryEventStore : IEvaluationTelemetryEventStore
{
    private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _acceptedCount;

    public async Task<EvaluationTelemetryEventStoreStatus> AcceptAsync(
        EvaluationTelemetryLedgerRecord ledgerRecord,
        CancellationToken cancellationToken = default)
    {
        if (Interlocked.Increment(ref _acceptedCount) != 1)
        {
            return EvaluationTelemetryEventStoreStatus.Persisted;
        }

        _entered.TrySetResult();
        await _release.Task.WaitAsync(cancellationToken);
        return EvaluationTelemetryEventStoreStatus.Persisted;
    }

    public Task WaitUntilEnteredAsync() => _entered.Task.WaitAsync(TimeSpan.FromSeconds(5));

    public void Release() => _release.TrySetResult();
}
