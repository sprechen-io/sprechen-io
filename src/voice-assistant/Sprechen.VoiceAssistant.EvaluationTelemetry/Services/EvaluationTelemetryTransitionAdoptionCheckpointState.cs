namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Bewahrt den übernommenen Übergangszustand samt geordnetem Ablaufprotokoll, damit dieser Zustand beim Wiederanlauf nachgerechnet werden kann.
/// </summary>
internal sealed record EvaluationTelemetryTransitionAdoptionCheckpointState(
    int LastStateVersion,
    IReadOnlyList<string>? AdoptedTransitionIdentities,
    IReadOnlyList<EvaluationTelemetryTransitionAdoptionCheckpointEntry> LedgerEntries);
