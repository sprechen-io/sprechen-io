namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Hält den nachgerechneten Übergangszustand für den abschließenden Vergleich
/// mit dem gespeicherten Checkpoint fest.
/// </summary>
internal readonly record struct EvaluationTelemetryTransitionReplayState(
    int StateVersion,
    IReadOnlyList<string>? AdoptedTransitionIdentities);
