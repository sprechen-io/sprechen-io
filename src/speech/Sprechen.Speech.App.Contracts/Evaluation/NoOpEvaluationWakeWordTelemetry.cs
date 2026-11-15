namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Erfüllt den Vertrag für Wake-Word-Telemetrie ohne Ereignisse aufzuzeichnen.
/// So können Hosts den Sprachclient auch ohne Auswertungstelemetrie verwenden.
/// </summary>
public sealed class NoOpEvaluationWakeWordTelemetry : IEvaluationWakeWordTelemetry
{
    /// <inheritdoc />
    public void TrackWakeWordMiss(EvaluationWakeWordMiss miss)
    {
    }
}
