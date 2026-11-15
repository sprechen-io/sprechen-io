using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.Voice.App.Tests.TestDoubles;

internal sealed class ImmediateEvaluationTelemetryExclusiveLeaseHandle : IEvaluationTelemetryExclusiveLeaseHandle
{
    internal static ImmediateEvaluationTelemetryExclusiveLeaseHandle Instance { get; } = new();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
