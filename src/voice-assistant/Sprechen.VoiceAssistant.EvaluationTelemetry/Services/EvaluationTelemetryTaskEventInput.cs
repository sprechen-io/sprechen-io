using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Bündelt Ereignisdaten und abgeleitete Übergänge für die gemeinsame Übernahme zusammengehöriger Beobachtungen.
/// </summary>
internal sealed record EvaluationTelemetryTaskEventInput(
    string EventName,
    object? Payload,
    IReadOnlyList<TelemetryCanonicalTransition> ProjectedTransitions);
