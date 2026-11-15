using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests;

internal sealed class RecordingEvaluationTelemetryExclusiveLease : IEvaluationTelemetryExclusiveLease
{
    private readonly HashSet<string> _heldLeaseKeys = new(StringComparer.Ordinal);

    internal IReadOnlySet<string> HeldLeaseKeys => _heldLeaseKeys;

    public ValueTask<IEvaluationTelemetryExclusiveLeaseHandle?> TryAcquireAsync(
        string leaseKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _heldLeaseKeys.Add(leaseKey);
        return ValueTask.FromResult<IEvaluationTelemetryExclusiveLeaseHandle?>(
            new RecordingEvaluationTelemetryExclusiveLeaseHandle(_heldLeaseKeys, leaseKey));
    }
}
