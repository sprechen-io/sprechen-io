namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Protokolliert entweder ein Ereignispaket oder den anfänglichen Abschlussstatus
/// von Feldern und Gruppen,
/// damit die Übernahme des Aufgabenstands in derselben Reihenfolge nachrechenbar bleibt.
/// </summary>
internal sealed record EvaluationTelemetryTransitionAdoptionCheckpointEntry(
    string Kind,
    int StateVersion,
    IReadOnlyList<string> EventIds,
    IReadOnlyList<string> TransitionIdentities)
{
    internal const string EventBatchKind = "event_batch";
    internal const string CompletionBaselineKind = "completion_baseline";

    internal static EvaluationTelemetryTransitionAdoptionCheckpointEntry ForEventBatch(
        int stateVersion,
        IReadOnlyList<string> eventIds)
        => new(
            EventBatchKind,
            stateVersion,
            eventIds,
            []);

    internal static EvaluationTelemetryTransitionAdoptionCheckpointEntry ForCompletionBaseline(
        int stateVersion,
        IReadOnlyList<string> completionTransitionIdentities)
        => new(
            CompletionBaselineKind,
            stateVersion,
            [],
            completionTransitionIdentities);
}
