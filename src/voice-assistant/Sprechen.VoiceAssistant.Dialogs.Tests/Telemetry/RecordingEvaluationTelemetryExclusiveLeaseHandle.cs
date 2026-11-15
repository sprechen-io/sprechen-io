using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests;

internal sealed class RecordingEvaluationTelemetryExclusiveLeaseHandle(
    HashSet<string> heldLeaseKeys,
    string leaseKey) : IEvaluationTelemetryExclusiveLeaseHandle
{
    public ValueTask DisposeAsync()
    {
        heldLeaseKeys.Remove(leaseKey);
        return ValueTask.CompletedTask;
    }
}
