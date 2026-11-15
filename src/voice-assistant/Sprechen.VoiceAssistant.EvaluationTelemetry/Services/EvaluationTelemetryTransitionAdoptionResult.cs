using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Gibt den fortgeschriebenen Aufgabenstand und die tatsächlich auszugebenden
/// Übergänge gemeinsam an die Checkpoint-Erzeugung zurück.
/// </summary>
internal sealed record EvaluationTelemetryTransitionAdoptionResult(
    int StateVersion,
    IReadOnlyList<string>? AdoptedTransitionIdentities,
    IReadOnlyList<TelemetryCanonicalTransition> EmittedTransitions);
