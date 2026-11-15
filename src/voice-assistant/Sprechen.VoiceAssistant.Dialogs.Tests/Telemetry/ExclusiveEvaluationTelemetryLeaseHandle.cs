using System.Collections.Concurrent;
using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests;

internal sealed class ExclusiveEvaluationTelemetryLeaseHandle(
    ConcurrentDictionary<string, byte> leases,
    string leaseKey) : IEvaluationTelemetryExclusiveLeaseHandle
{
    private readonly ConcurrentDictionary<string, byte> _leases = leases;
    private readonly string _leaseKey = leaseKey;
    private bool _released;

    public ValueTask DisposeAsync()
    {
        if (!_released)
        {
            _released = true;
            _leases.TryRemove(_leaseKey, out _);
        }

        return ValueTask.CompletedTask;
    }
}
