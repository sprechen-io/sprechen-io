using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.Voice.App.Tests.TestDoubles;

internal sealed class ImmediateEvaluationTelemetryExclusiveLease : IEvaluationTelemetryExclusiveLease
{
    public ValueTask<IEvaluationTelemetryExclusiveLeaseHandle?> TryAcquireAsync(
        string leaseKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IEvaluationTelemetryExclusiveLeaseHandle?>(
            ImmediateEvaluationTelemetryExclusiveLeaseHandle.Instance);
    }
}
