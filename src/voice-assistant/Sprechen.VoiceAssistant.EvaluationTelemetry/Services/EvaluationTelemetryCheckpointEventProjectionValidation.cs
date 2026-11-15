using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Hält gespeicherte und neu abgeleitete Übergänge für den Vergleich beim Wiederanlauf getrennt.
/// </summary>
internal sealed record EvaluationTelemetryCheckpointEventProjectionValidation(
    IReadOnlyList<TelemetryCanonicalTransition> RecordedTransitions,
    IReadOnlyList<TelemetryCanonicalTransition> ProjectedTransitions);
