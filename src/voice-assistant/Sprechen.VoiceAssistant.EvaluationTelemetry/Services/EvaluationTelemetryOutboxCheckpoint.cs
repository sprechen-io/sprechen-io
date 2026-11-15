using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Bewahrt Versandphase, Ereignisse und Übergangshistorie eines Durchlaufs
/// zusammen, damit Wiederherstellung und Abschluss denselben gespeicherten Stand prüfen.
/// </summary>
internal sealed record EvaluationTelemetryOutboxCheckpoint(
    int SchemaVersion,
    string StorageKey,
    string SessionId,
    bool IsTaskRun,
    EvaluationTelemetryEnvelopeIdentity Identity,
    string Phase,
    EvaluationTelemetryTransitionAdoptionCheckpointState TransitionAdoptionState,
    IReadOnlyList<EvaluationTelemetryStoredEvent> PendingEvents,
    IReadOnlyList<EvaluationTelemetryStoredEvent> AcknowledgedEvents,
    TelemetryTaskEndOutcome? TerminalIntent,
    EvaluationTelemetryAcceptedReceipt? AcceptedReceipt,
    int? PermanentFailureStatusCode,
    long ClientOffsetElapsedMs);
