using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests;

internal sealed class ImmediateEvaluationTelemetryExclusiveLeaseHandle : IEvaluationTelemetryExclusiveLeaseHandle
{
    internal static ImmediateEvaluationTelemetryExclusiveLeaseHandle Instance { get; } = new();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
