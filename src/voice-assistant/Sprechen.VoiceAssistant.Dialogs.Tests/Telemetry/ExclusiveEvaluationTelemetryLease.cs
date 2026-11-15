using System.Collections.Concurrent;
using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests;

internal sealed class ExclusiveEvaluationTelemetryLease : IEvaluationTelemetryExclusiveLease
{
    private readonly ConcurrentDictionary<string, byte> _leases = new(StringComparer.Ordinal);
    private int _acquisitionCount;

    internal int AcquisitionCount => Volatile.Read(ref _acquisitionCount);

    internal int HeldCount => _leases.Count;

    public ValueTask<IEvaluationTelemetryExclusiveLeaseHandle?> TryAcquireAsync(
        string leaseKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_leases.TryAdd(leaseKey, 0))
        {
            return ValueTask.FromResult<IEvaluationTelemetryExclusiveLeaseHandle?>(null);
        }

        Interlocked.Increment(ref _acquisitionCount);
        return ValueTask.FromResult<IEvaluationTelemetryExclusiveLeaseHandle?>(
            new ExclusiveEvaluationTelemetryLeaseHandle(_leases, leaseKey));
    }
}
