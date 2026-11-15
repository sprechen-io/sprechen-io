using Sprechen.Speech.App.Contracts.Evaluation;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class RecordingEvaluationWakeWordTelemetry : IEvaluationWakeWordTelemetry
{
    public List<EvaluationWakeWordMiss> Recorded { get; } = [];

    public void TrackWakeWordMiss(EvaluationWakeWordMiss miss) => Recorded.Add(miss);
}
