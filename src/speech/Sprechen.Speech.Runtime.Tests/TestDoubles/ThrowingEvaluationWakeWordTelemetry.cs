using Sprechen.Speech.App.Contracts.Evaluation;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class ThrowingEvaluationWakeWordTelemetry : IEvaluationWakeWordTelemetry
{
    public void TrackWakeWordMiss(EvaluationWakeWordMiss miss)
        => throw new InvalidOperationException("evaluation telemetry unavailable");
}
