/**
 * Erzeugt und versendet Evaluationsereignisse der Formularoberfläche.
 * Gemeinsame Typen, Konstanten und Projektionen stammen aus
 * Generated/telemetryVocabulary.generated.ts und werden als klassisches Skript geladen.
 */

/** Lokaler Abschlussgrund der Oberfläche. Wird nicht als eigenes Telemetrieereignis versendet. */
type SessionOutcome = "submitted" | "cancelled" | "abandoned";

/** Gemeinsame Metadaten und Nutzdaten eines Evaluationsereignisses. */
interface TelemetryEventEnvelope {
    event_id: string;
    session_id: string;
    client_offset_ms: number;
    participant_id: string;
    condition: StudyCondition;
    application_id: string;
    task_id: string;
    record_provenance: RecordProvenance;
    event: string;
    state_version: number;
    data?: Record<string, unknown>;
}

/** Teilnehmer-, Aufgaben- und Bedingungskennung der Studie. */
interface StudyContext {
    participantId: string;
    taskId: string;
    condition: StudyCondition;
}

/** Aufgabenidentität einschließlich Anwendung und Herkunft des Datensatzes. */
interface EvaluationTaskIdentity extends StudyContext {
    applicationId: string;
    recordProvenance: RecordProvenance;
}

/** Lebenszyklus der Aufgabe vom Start bis zum bestätigten oder fehlgeschlagenen Abschluss. */
type EvaluationTaskPhase = "idle" | "active" | "ending" | "accepted" | "committed" | "ended" | "failed";

/** Gemeinsamer Sitzungszustand für Checkpoints und Ereignisversand. */
interface EvaluationSession {
    sessionId: string;
    checkpointKey: string;
    applicationId: string;
    formId: string;
    identity: EvaluationTaskIdentity;
    stateVersion: number;
    // Bereits übernommene Zustandsübergänge. Null vor der ersten Übernahme.
    adoptedTransitions: string[] | null;
    transitionAdoptionEntries: TransitionAdoptionCheckpointEntry[];
    acknowledgedEventBodies: string[];
    pendingEventBodies: string[];
    taskPhase: EvaluationTaskPhase;
    terminalIntent?: TaskEndOptions;
    failureCode?: string;
    terminalDelivery: TerminalDelivery | null;
    deliveryTask: Promise<void> | null;
    retryAttempt: number;
    retryTimer: number | null;
    identityWriterLease: EvaluationIdentityWriterLease | null;
    releaseIdentityLeaseWhenCaughtUp: boolean;
    clientOffsetOriginMs: number;
}

/** Exklusive Schreibsperre für eine Aufgabenidentität über mehrere Tabs hinweg. */
interface EvaluationIdentityWriterLease {
    released: boolean;
    release: () => void;
}

/** Promise für die Annahme oder endgültige Ablehnung des Aufgabenabschlusses. */
interface TerminalDelivery {
    promise: Promise<void>;
    resolve: () => void;
    reject: (reason: Error) => void;
    settled: boolean;
}

/** Identitäts- und Seitenangaben zum Initialisieren einer Telemetriesitzung. */
interface StartSessionOptions {
    operationalProbe?: boolean;
    applicationId: string;
    formId: string;
    requestedLanguage: string;
    participantId?: string;
    condition?: string;
}

/** Angaben zur Ableitung des Aufgabenendzustands aus Abschluss und Validierung. */
interface TaskEndOptions {
    processSubmitted: boolean;
    requiredFieldsTotal: number;
    requiredFieldsCompleted: number;
    openBlockingValidationCount: number;
    terminationClassification: ParticipantTaskTerminationClassification;
    mismatchCode: string;
    correctnessBasis: string;
}

/** Unterscheidet regulären Abschluss, Teilnehmerabbruch und technische Abbruchgründe. */
type TaskTerminationClassification = "completed" | "participant_cancelled" | "session_abandoned"
    | "study_directed_abort" | "technical_failure";

/** Teilnehmerseitig zulässige Abschlussarten einschließlich technischer Fehler. Ohne studiengeleiteten Abbruch. */
type ParticipantTaskTerminationClassification = "completed" | "participant_cancelled"
    | "session_abandoned" | "technical_failure";

/** Aktuelles Navigationsziel und Position innerhalb der navigierbaren Felder. */
interface StatusNavigationData {
    current_field_id: string;
    current_field_group_id: string;
    navigation_index: number;
    navigation_total: number;
}

/** Anzahl abgeschlossener und insgesamt vorhandener Einheiten. */
interface ProgressCounterData {
    completed: number;
    total: number;
}

/** Fortschritt nach allen Feldern, Pflichtfeldern und Feldgruppen. */
interface StatusProgressData {
    allFields: ProgressCounterData;
    requiredFieldsOnly: ProgressCounterData;
    fieldGroupBased: ProgressCounterData;
}

/** Gemeinsame Momentaufnahme von Navigation und Fortschritt. */
interface StatusSnapshotData {
    navigation: StatusNavigationData;
    progress: StatusProgressData;
    zero_denominator_mode?: "allFields" | "requiredFieldsOnly" | "fieldGroupBased";
}

/** Feldzustand mit Kennzeichen für Änderungen an Relevanz und Vollständigkeit. */
interface FieldStatusChangeData {
    fieldId: string;
    fieldGroupId: string;
    relevant: boolean;
    required: boolean;
    completed: boolean;
    blockingErrorCodes: string[];
    relevanceChanged: boolean;
    completionChanged: boolean;
}

/** Gruppenzustand mit Kennzeichen für Änderungen an Relevanz und Vollständigkeit. */
interface FieldGroupStatusChangeData {
    fieldGroupId: string;
    relevant: boolean;
    completed: boolean;
    openGroupErrorCodes: string[];
    relevanceChanged: boolean;
    completionChanged: boolean;
}

/** Zusammengehörige Feld- und Gruppenänderungen für einen gemeinsamen Ereignisversand. */
interface FieldAndGroupStatusChanges {
    fields: FieldStatusChangeData[];
    fieldGroups: FieldGroupStatusChangeData[];
}

/** Anfänglicher Vollständigkeitszustand eines Feldes. */
interface FieldCompletionBaselineData {
    fieldId: string;
    completed: boolean;
}

/** Anfänglicher Vollständigkeitszustand einer Feldgruppe. */
interface FieldGroupCompletionBaselineData {
    fieldGroupId: string;
    completed: boolean;
}

/** Ausgangszustände zur Unterscheidung späterer Vollständigkeitsänderungen. */
interface FieldAndGroupCompletionBaseline {
    fields: FieldCompletionBaselineData[];
    fieldGroups: FieldGroupCompletionBaselineData[];
}

/** Gespeicherte Zuordnung einer Ereignisgruppe zu ihrer Zustandsversion. */
interface EventBatchTransitionAdoptionCheckpointEntry {
    kind: "event_batch";
    stateVersion: number;
    eventIds: string[];
}

/** Gespeicherte Ausgangszustände der Vollständigkeit mit ihrer Zustandsversion. */
interface CompletionBaselineTransitionAdoptionCheckpointEntry {
    kind: "completion_baseline";
    stateVersion: number;
    transitionIdentities: string[];
}

/** Gespeicherte Übernahme von Ereignissen oder anfänglichen Vollständigkeitszuständen. */
type TransitionAdoptionCheckpointEntry =
    EventBatchTransitionAdoptionCheckpointEntry
    | CompletionBaselineTransitionAdoptionCheckpointEntry;

/** Schnittstelle für Ereignisse, Zustand und Aufgabenabschluss der Formularnavigation. */
interface FormsTelemetryApi {
    initializeOperationalProbe(options: StartSessionOptions): Promise<void>;
    getOperationalProbeIdentity(applicationId: string, formId: string): EvaluationTaskIdentity;
    /** Setzt bei Studien- und Testläufen den Anwendungszustand mit der übergebenen Rücksetzfunktion unter der Identitätssperre zurück. */
    startSession(options: StartSessionOptions, resetApplicationState?: () => void): Promise<EvaluationSession>;
    subscribeToDeliveryFailures(listener: (session: EvaluationSession, error: Error) => void): () => void;
    endSession(session: EvaluationSession, outcome: SessionOutcome): boolean;
    startTask(session: EvaluationSession): void;
    endTask(session: EvaluationSession, options: TaskEndOptions): Promise<void>;
    commitTask(session: EvaluationSession): void;
    switchForm(session: EvaluationSession, newFormId: string): EvaluationSession;
    trackNavigationChanged(session: EvaluationSession, data: StatusNavigationData): void;
    primeFieldAndGroupCompletionBaseline(
        session: EvaluationSession,
        baseline: FieldAndGroupCompletionBaseline
    ): void;
    trackFieldAndGroupStatusChanges(session: EvaluationSession, changes: FieldAndGroupStatusChanges): void;
    trackStatusSnapshot(session: EvaluationSession, data: StatusSnapshotData): void;
    trackFieldFocus(session: EvaluationSession, fieldId: string, fieldGroupId?: string): void;
    trackFieldAttempt(session: EvaluationSession, fieldId: string, inputMode: string, success: boolean): void;
    trackValidationErrorOpened(
        session: EvaluationSession,
        errorCode: string,
        severity: string,
        trigger: ErrorTrigger,
        scope?: ContractScope | string,
        fieldId?: string,
        fieldGroupId?: string,
        relatedFieldIds?: string[]
    ): void;
    trackValidationErrorCleared(
        session: EvaluationSession,
        errorCode: string,
        severity: string,
        scope?: ContractScope | string,
        fieldId?: string,
        fieldGroupId?: string,
        relatedFieldIds?: string[]
    ): void;
    trackNavigationError(session: EvaluationSession, scope?: ContractScope | string): void;
    trackAmbiguityResolved(
        session: EvaluationSession,
        ambiguityType: AmbiguityType,
        resolutionStrategy: AmbiguityResolutionStrategy,
        signalClass: AmbiguitySignalClass,
        result: AmbiguityResult,
        fieldId?: string,
        commandId?: string
    ): void;
}

/** Optionale Hostkonfiguration des Telemetrieendpunkts. */
interface FormsSprechenConfig {
    telemetryEndpoint?: string;
}

/** Hängt formsTelemetry an window, damit klassische Skripte denselben Einstieg teilen. */
interface Window {
    formsTelemetry: FormsTelemetryApi;
    formsSprechen?: FormsSprechenConfig;
}

((): void => {
    "use strict";

    const DefaultTelemetryEndpoint = "/api/telemetry";
    const EvaluationCheckpointVersion = 1;
    const EvaluationCheckpointKeyPrefix = "Sprechen.Forms.evaluation.v1:";
    const EvaluationIdentityWriterLockPrefix = "Sprechen.Forms.evaluation.writer.v1:";
    const MaximumCheckpointEvents = 10000;
    const MaximumTransitionAdoptionEntries = MaximumCheckpointEvents * 2;
    const MaximumEventBodyLength = 262144;
    const InitialRetryDelayMilliseconds = 250;
    const MaximumRetryDelayMilliseconds = 30000;
    const EventIdPattern = /^[a-f0-9]{32}$/;
    const WebLocksUnavailableErrorCode = "telemetry.delivery.web_locks_unavailable";
    const StorageUnavailableErrorCode = "telemetry.delivery.storage_unavailable";
    const CheckpointInvalidErrorCode = "telemetry.delivery.checkpoint_invalid";
    const CheckpointMissingErrorCode = "telemetry.delivery.checkpoint_missing";
    const IdentityWriterUnavailableErrorCode = "telemetry.delivery.identity_writer_unavailable";
    const PermanentRejectionErrorCode = "telemetry.delivery.permanent_rejection";
    const FocusTokenSeparator = "\u001f";
    const lastFocusTokenBySession = new Map<string, string>();
    const sessionsByCheckpointKey = new Map<string, EvaluationSession>();
    const deliveryFailureListeners = new Set<(session: EvaluationSession, error: Error) => void>();

    const subscribeToDeliveryFailures = (
        listener: (session: EvaluationSession, error: Error) => void
    ): (() => void) => {
        deliveryFailureListeners.add(listener);
        return (): void => {
            deliveryFailureListeners.delete(listener);
        };
    };

    const generateOpaqueId = (): string => {
        const array = new Uint8Array(16);
        crypto.getRandomValues(array);
        return Array.from(array, (b) => b.toString(16).padStart(2, "0")).join("");
    };

    const createDeliveryError = (code: string): Error => {
        const error = new Error(code);
        error.name = "EvaluationTelemetryDeliveryError";
        return error;
    };

    let operationalProbeIdentity: EvaluationTaskIdentity | null = null;
    let operationalBootstrapEpoch = 0;
    let operationalEntryRequested = window.location.pathname === "/umfrage/probe";

    const getOperationalProbeIdentity = (applicationId: string, formId: string): EvaluationTaskIdentity => {
        if (window.location.pathname !== "/umfrage/probe"
            || operationalProbeIdentity === null
            || operationalProbeIdentity.applicationId !== applicationId.trim()
            || operationalProbeIdentity.taskId !== formId.trim()) {
            throw new Error("telemetry.probe.context_unavailable");
        }
        return { ...operationalProbeIdentity };
    };

    const initializeOperationalProbe = async (options: StartSessionOptions): Promise<void> => {
        if (window.location.pathname !== "/umfrage/probe"
            || options.operationalProbe !== true
            || studyRunIsParticipantInvited(options)
            || normalizeCondition(options.condition, false) !== "A"
            || !options.applicationId.trim()
            || !options.formId.trim()) {
            throw new Error("telemetry.probe.context_invalid");
        }
        operationalEntryRequested = true;
        if (operationalProbeIdentity !== null) {
            getOperationalProbeIdentity(options.applicationId, options.formId);
            return;
        }
        const epoch = ++operationalBootstrapEpoch;
        const response = await fetch("/api/telemetry/probe/context", {
            method: "GET",
            cache: "no-store",
            credentials: "omit",
            redirect: "error",
            signal: AbortSignal.timeout(15000)
        });
        if (response.status !== 200) {
            throw new Error("telemetry.probe.context_failed");
        }
        const body: unknown = await response.json();
        if (body === null || typeof body !== "object"
            || Object.keys(body).sort().join(",") !== "condition,probe_id"
            || !("probe_id" in body) || typeof body.probe_id !== "string"
            || !/^probe-[a-f0-9]{32}$/.test(body.probe_id)
            || !("condition" in body) || body.condition !== "A"
            || epoch !== operationalBootstrapEpoch
            || window.location.pathname !== "/umfrage/probe") {
            throw new Error("telemetry.probe.context_invalid");
        }
        operationalProbeIdentity = {
            applicationId: options.applicationId.trim(),
            taskId: options.formId.trim(),
            participantId: body.probe_id,
            condition: "A",
            recordProvenance: "operational_probe"
        };
    };

    const mayDeliverIdentity = (identity: EvaluationTaskIdentity): boolean => {
        if (operationalEntryRequested) {
            return operationalProbeIdentity !== null
                && window.location.pathname === "/umfrage/probe"
                && identitiesEqual(operationalProbeIdentity, identity);
        }
        return identity.recordProvenance !== "operational_probe";
    };

    window.addEventListener("pagehide", () => {
        operationalBootstrapEpoch += 1;
        operationalProbeIdentity = null;
    });

    const getTelemetryEndpoint = (identity: EvaluationTaskIdentity): string => {
        if (identity.recordProvenance === "operational_probe") {
            return "/api/telemetry/probe";
        }

        const configured = window.formsSprechen?.telemetryEndpoint;
        if (typeof configured === "string" && configured.trim().length > 0) {
            return configured.trim();
        }

        return DefaultTelemetryEndpoint;
    };

    const normalizeParticipantId = (participantId?: string): string => {
        const normalized = participantId?.trim() ?? "";
        return normalized.length > 0 ? normalized : telemetryUnknownParticipantId;
    };

    const normalizeTaskId = (taskId?: string): string => {
        const normalized = taskId?.trim() ?? "";
        return normalized.length > 0 ? normalized : telemetryUnknownTaskId;
    };

    const normalizeSeverity = (severity: string): Severity => {
        const normalized = severity.trim().toLowerCase();
        if (normalized === "warning" || normalized === "info") {
            return normalized;
        }

        return "error";
    };

    // Leere Eingaben als unclear weitergeben, damit die Vertragsprüfung sie
    // zurückweisen kann.
    const sanitizeVocabularyValue = (value: string): string => {
        const normalized = value.trim();
        return normalized.length > 0 ? normalized : "unclear";
    };

    const sanitizeId = (value: string): string => {
        const normalized = value.trim();
        return normalized.length > 0 ? normalized : telemetryUnidentifiedObject;
    };

    const sanitizePositiveNumber = (value: number): number => {
        if (!Number.isFinite(value)) {
            return 0;
        }

        return Math.max(0, Math.trunc(value));
    };

    const sanitizeOptionalString = (value: string | undefined): string | undefined => {
        if (typeof value !== "string") {
            return undefined;
        }

        const normalized = value.trim();
        return normalized.length > 0 ? normalized : undefined;
    };

    const sanitizeRelatedFieldIds = (relatedFieldIds?: string[]): string[] | undefined => {
        if (!Array.isArray(relatedFieldIds)) {
            return undefined;
        }

        const normalized = Array.from(
            new Set(
                relatedFieldIds
                    .map((id) => (typeof id === "string" ? id.trim() : ""))
                    .filter((id) => id.length > 0)
            )
        ).sort();

        return normalized.length > 0 ? normalized : undefined;
    };

    const createErrorData = (
        diagnosisCode: StudyDiagnosisCode,
        severity: string,
        scope: ContractScope | string | undefined,
        fieldId?: string,
        fieldGroupId?: string,
        relatedFieldIds?: string[],
        trigger?: ErrorTrigger
    ): Record<string, unknown> => {
        const data: Record<string, unknown> = {
            error_code: diagnosisCode,
            severity: normalizeSeverity(severity),
            scope: toLoggingScope(scope)
        };

        if (trigger) {
            data.trigger = trigger;
        }

        const normalizedFieldId = sanitizeOptionalString(fieldId);
        if (normalizedFieldId) {
            data.field_id = normalizedFieldId;
        }

        const normalizedFieldGroupId = sanitizeOptionalString(fieldGroupId);
        if (normalizedFieldGroupId) {
            data.field_group_id = normalizedFieldGroupId;
        }

        const normalizedRelatedFieldIds = sanitizeRelatedFieldIds(relatedFieldIds);
        if (normalizedRelatedFieldIds) {
            data.related_field_ids = normalizedRelatedFieldIds;
        }

        return data;
    };

    const parseEventBody = (body: string): TelemetryEventEnvelope =>
        JSON.parse(body) as TelemetryEventEnvelope;

    /** Lokal gespeicherter Sitzungsstand zur Wiederaufnahme ausstehender Ereignisse. */
    interface EvaluationSessionCheckpoint {
        version: 1;
        sessionId: string;
        identity: EvaluationTaskIdentity;
        stateVersion: number;
        adoptedTransitions: string[] | null;
        transitionAdoptionEntries: TransitionAdoptionCheckpointEntry[];
        acknowledgedEventBodies: string[];
        pendingEventBodies: string[];
        taskPhase: EvaluationTaskPhase;
        clientOffsetElapsedMs: number;
        terminalIntent?: TaskEndOptions;
        failureCode?: string;
    }

    const getCheckpointKey = (identity: EvaluationTaskIdentity, sessionId: string): string => {
        const identityMembers = [
            identity.applicationId,
            identity.participantId,
            identity.condition,
            identity.taskId,
            identity.recordProvenance,
            sessionId
        ];
        return `${EvaluationCheckpointKeyPrefix}${identityMembers.map(encodeURIComponent).join(":")}`;
    };

    const getIdentityWriterLockName = (identity: EvaluationTaskIdentity): string => {
        const identityMembers = [
            identity.applicationId,
            identity.participantId,
            identity.condition,
            identity.taskId,
            identity.recordProvenance
        ];
        return `${EvaluationIdentityWriterLockPrefix}${identityMembers.map(encodeURIComponent).join(":")}`;
    };

    const identitiesEqual = (left: EvaluationTaskIdentity, right: EvaluationTaskIdentity): boolean =>
        left.applicationId === right.applicationId
        && left.participantId === right.participantId
        && left.condition === right.condition
        && left.taskId === right.taskId
        && left.recordProvenance === right.recordProvenance;

    const acquireIdentityWriterLease = async (
        identity: EvaluationTaskIdentity
    ): Promise<EvaluationIdentityWriterLease> => {
        if (!navigator.locks || typeof navigator.locks.request !== "function") {
            throw createDeliveryError(WebLocksUnavailableErrorCode);
        }

        const lockName = getIdentityWriterLockName(identity);
        let grantResolve: ((lease: EvaluationIdentityWriterLease) => void) | undefined;
        let grantReject: ((reason: Error) => void) | undefined;
        let acquired = false;
        const grant = new Promise<EvaluationIdentityWriterLease>((resolve, reject) => {
            grantResolve = resolve;
            grantReject = reject;
        });

        let requestTask: Promise<void>;
        try {
            requestTask = navigator.locks.request<void>(
                lockName,
                { mode: "exclusive", ifAvailable: true },
                async (lock): Promise<void> => {
                    if (!lock) {
                        grantReject?.(createDeliveryError(IdentityWriterUnavailableErrorCode));
                        return;
                    }

                    acquired = true;
                    let releaseResolve: (() => void) | undefined;
                    const released = new Promise<void>((resolve) => {
                        releaseResolve = resolve;
                    });
                    const lease: EvaluationIdentityWriterLease = {
                        released: false,
                        release: (): void => {
                            if (lease.released) {
                                return;
                            }

                            lease.released = true;
                            releaseResolve?.();
                        }
                    };
                    grantResolve?.(lease);
                    await released;
                });
        } catch {
            throw createDeliveryError(WebLocksUnavailableErrorCode);
        }

        void requestTask.catch(() => {
            if (!acquired) {
                grantReject?.(createDeliveryError(WebLocksUnavailableErrorCode));
            }
        });

        return grant;
    };

    const isTaskPhase = (value: unknown): value is EvaluationTaskPhase =>
        value === "idle" || value === "active" || value === "ending"
        || value === "accepted" || value === "committed" || value === "failed";

    const isEventBodyList = (value: unknown): value is string[] =>
        Array.isArray(value)
        && value.length <= MaximumCheckpointEvents
        && value.every((body) => typeof body === "string"
            && body.length > 0
            && body.length <= MaximumEventBodyLength);

    const ParticipantTaskTerminationClassifications = new Set<ParticipantTaskTerminationClassification>([
        "completed", "participant_cancelled", "session_abandoned", "technical_failure"
    ]);
    const EmittableMismatchCodes = new Set([
        "none",
        "task_card_mismatch",
        "missing_required",
        "wrong_field_class",
        "wrong_dependency_state",
        "validation_error_open",
        "not_submitted",
        "not_assessed"
    ]);

    const normalizeTerminationClassification = (value: unknown): ParticipantTaskTerminationClassification => {
        if (typeof value !== "string"
            || !ParticipantTaskTerminationClassifications.has(value as ParticipantTaskTerminationClassification)) {
            throw createDeliveryError(CheckpointInvalidErrorCode);
        }
        return value as ParticipantTaskTerminationClassification;
    };

    const normalizeTaskEndOptions = (options: TaskEndOptions): TaskEndOptions => ({
        processSubmitted: options.processSubmitted === true,
        requiredFieldsTotal: sanitizePositiveNumber(options.requiredFieldsTotal),
        requiredFieldsCompleted: sanitizePositiveNumber(options.requiredFieldsCompleted),
        openBlockingValidationCount: sanitizePositiveNumber(options.openBlockingValidationCount),
        terminationClassification: normalizeTerminationClassification(options.terminationClassification),
        mismatchCode: options.mismatchCode.trim(),
        correctnessBasis: options.correctnessBasis.trim()
    });

    const isTaskEndOptions = (value: unknown): value is TaskEndOptions => {
        if (value === null || typeof value !== "object") {
            return false;
        }

        const candidate = value as Partial<TaskEndOptions>;
        return typeof candidate.processSubmitted === "boolean"
            && typeof candidate.requiredFieldsTotal === "number"
            && Number.isFinite(candidate.requiredFieldsTotal)
            && typeof candidate.requiredFieldsCompleted === "number"
            && Number.isFinite(candidate.requiredFieldsCompleted)
            && typeof candidate.openBlockingValidationCount === "number"
            && Number.isFinite(candidate.openBlockingValidationCount)
            && typeof candidate.terminationClassification === "string"
            && ParticipantTaskTerminationClassifications.has(
                candidate.terminationClassification as ParticipantTaskTerminationClassification)
            && typeof candidate.mismatchCode === "string"
            && EmittableMismatchCodes.has(candidate.mismatchCode)
            && typeof candidate.correctnessBasis === "string"
            && candidate.correctnessBasis === "log_and_system_check";
    };

    const MaximumClientOffsetMs = 2147483647;

    const readClientOffsetMs = (session: EvaluationSession): number => {
        const value = Math.floor(performance.now() - session.clientOffsetOriginMs);
        if (!Number.isInteger(value) || value < 0 || value > MaximumClientOffsetMs) {
            throw createDeliveryError(CheckpointInvalidErrorCode);
        }
        return value;
    };

    const originFromElapsed = (elapsedMs: number): number => performance.now() - elapsedMs;

    const hasExactKeys = (value: Record<string, unknown>, keys: string[]): boolean => {
        const actual = Object.keys(value).sort();
        const expected = [...keys].sort();
        return actual.length === expected.length
            && actual.every((key, index) => key === expected[index]);
    };

    const FormsCheckpointEventNames = new Set([
        "task_start", "task_end", "navigation_changed", "field_relevance_changed",
        "field_completion_changed", "field_group_relevance_changed",
        "field_group_completion_changed", "status_snapshot", "focus_change", "value_change",
        "validation_error_opened", "validation_error_cleared", "navigation_error", "ambiguity_resolved"
    ]);
    const isCanonicalEventBody = (
        body: string,
        identity: EvaluationTaskIdentity
    ): body is string => {
        let event: TelemetryEventEnvelope;
        try {
            event = parseEventBody(body);
        } catch {
            return false;
        }

        if (!event
            || typeof event !== "object"
            || Array.isArray(event)
            || JSON.stringify(event) !== body
            || !validateTelemetryCheckpointEnvelope(event)
            || !FormsCheckpointEventNames.has(event.event)
            || event.participant_id !== identity.participantId
            || event.condition !== identity.condition
            || event.application_id !== identity.applicationId
            || event.task_id !== identity.taskId
            || event.record_provenance !== identity.recordProvenance
            || typeof event.event !== "string"
            || !Number.isInteger(event.state_version)
            || event.state_version <= 0
            || event.data === null
            || typeof event.data !== "object"
            || Array.isArray(event.data)) {
            return false;
        }

        try {
            projectCanonicalTransitions(event.event, event.data);
            return Array.isArray(event.data.canonical_transitions);
        } catch {
            return false;
        }
    };

    /** Aus gespeicherten Übernahmen rekonstruierte Zustandsversion und Übergänge. */
    interface ReplayedCheckpointState {
        stateVersion: number;
        adoptedTransitions: string[] | null;
    }

    const isCompletionTransitionIdentity = (identity: unknown): identity is string => {
        if (typeof identity !== "string") {
            return false;
        }

        const members = identity.split("\u001F");
        if (members.length !== 4
            || members.some((member) => member.trim().length === 0)
            || members[2] !== members[2].trim()
            || members[2].length > 128) {
            return false;
        }

        return (members[0] === "field_completion_changed"
                && members[1] === "field"
                && (members[3] === "complete" || members[3] === "incomplete"))
            || (members[0] === "field_group_completion_changed"
                && members[1] === "field_group"
                && (members[3] === "complete" || members[3] === "incomplete"));
    };

    const isTransitionAdoptionCheckpointEntry = (
        value: unknown
    ): value is TransitionAdoptionCheckpointEntry => {
        if (value === null || typeof value !== "object" || Array.isArray(value)) {
            return false;
        }

        const candidate = value as Partial<TransitionAdoptionCheckpointEntry>;
        if (!Number.isInteger(candidate.stateVersion) || (candidate.stateVersion ?? 0) < 1) {
            return false;
        }

        if (candidate.kind === "event_batch") {
            return hasExactKeys(value as Record<string, unknown>, ["kind", "stateVersion", "eventIds"])
                && Array.isArray(candidate.eventIds)
                && candidate.eventIds.length > 0
                && candidate.eventIds.length <= MaximumCheckpointEvents
                && candidate.eventIds.every((eventId) =>
                    typeof eventId === "string" && EventIdPattern.test(eventId))
                && new Set(candidate.eventIds).size === candidate.eventIds.length;
        }

        if (candidate.kind === "completion_baseline") {
            const transitionIdentities = candidate.transitionIdentities;
            if (!hasExactKeys(value as Record<string, unknown>, [
                "kind", "stateVersion", "transitionIdentities"
            ])
                || !Array.isArray(transitionIdentities)
                || transitionIdentities.length === 0
                || transitionIdentities.length > MaximumCheckpointEvents
                || !transitionIdentities.every(isCompletionTransitionIdentity)
                || new Set(transitionIdentities).size !== transitionIdentities.length) {
                return false;
            }

            try {
                const dimensions = transitionIdentities.map(transitionDimension);
                return new Set(dimensions).size === dimensions.length;
            } catch {
                return false;
            }
        }

        return false;
    };

    const isTransitionAdoptionCheckpointEntries = (
        value: unknown
    ): value is TransitionAdoptionCheckpointEntry[] =>
        Array.isArray(value)
        && value.length <= MaximumTransitionAdoptionEntries
        && value.every(isTransitionAdoptionCheckpointEntry);

    const replayCheckpointState = (
        events: TelemetryEventEnvelope[],
        entries: TransitionAdoptionCheckpointEntry[]
    ): ReplayedCheckpointState | null => {
        let stateVersion = 1;
        let adoptedTransitions: string[] | null = null;
        let eventIndex = 0;
        let precedingEventName: string | null = null;

        for (const entry of entries) {
            if (entry.kind === "completion_baseline") {
                if (entry.stateVersion !== stateVersion || precedingEventName !== "navigation_changed") {
                    return null;
                }

                try {
                    adoptedTransitions = primeCompletionAdoption(
                        adoptedTransitions,
                        entry.transitionIdentities);
                } catch {
                    return null;
                }
                precedingEventName = null;
                continue;
            }

            const adoptedEvents: TelemetryEventEnvelope[] = [];
            for (const eventId of entry.eventIds) {
                const event = events[eventIndex];
                if (!event || event.event_id !== eventId || event.state_version !== entry.stateVersion) {
                    return null;
                }
                adoptedEvents.push(event);
                eventIndex += 1;
            }
            const projectedByEvent = adoptedEvents.map((event) =>
                projectCanonicalTransitions(event.event, event.data));
            const adoption = advanceAdoption(stateVersion, adoptedTransitions, projectedByEvent.flat());
            const emittedIdentities = new Set(adoption.emittedTransitions.map(transitionIdentity));
            const eventsAreCanonical = adoptedEvents.every((event, eventIndex) =>
                JSON.stringify(event.data?.canonical_transitions)
                    === JSON.stringify(projectedByEvent[eventIndex].filter((transition) =>
                        emittedIdentities.has(transitionIdentity(transition)))));
            if (entry.stateVersion !== adoption.stateVersion || !eventsAreCanonical) {
                return null;
            }
            stateVersion = adoption.stateVersion;
            adoptedTransitions = adoption.adoptedTransitions;
            precedingEventName = adoptedEvents[adoptedEvents.length - 1].event;
        }

        return eventIndex === events.length
            ? { stateVersion, adoptedTransitions }
            : null;
    };

    const checkpointBodiesAreCanonical = (
        checkpoint: Pick<EvaluationSessionCheckpoint,
            "identity" | "stateVersion" | "adoptedTransitions"
            | "transitionAdoptionEntries"
            | "acknowledgedEventBodies" | "pendingEventBodies" | "taskPhase" | "terminalIntent">
    ): boolean => {
        const acknowledged = checkpoint.acknowledgedEventBodies;
        const pending = checkpoint.pendingEventBodies;
        const bodies = [...acknowledged, ...pending];
        if (!bodies.every((body) => isCanonicalEventBody(body, checkpoint.identity))) {
            return false;
        }

        const events = bodies.map(parseEventBody);
        const eventIds = new Set(events.map((event) => event.event_id));
        const replayedState = replayCheckpointState(events, checkpoint.transitionAdoptionEntries);
        if (eventIds.size !== events.length
            || replayedState === null
            || checkpoint.stateVersion !== replayedState.stateVersion
            || JSON.stringify(checkpoint.adoptedTransitions) !== JSON.stringify(replayedState.adoptedTransitions)
            || (acknowledged.some((body) => parseEventBody(body).event === "task_end")
                && checkpoint.taskPhase !== "accepted"
                && checkpoint.taskPhase !== "committed")) {
            return false;
        }

        if ((checkpoint.taskPhase === "idle" && bodies.length !== 0)
            || (checkpoint.taskPhase !== "idle"
                && (events.length === 0 || events[0].event !== "task_start"))
            || ((checkpoint.taskPhase === "accepted" || checkpoint.taskPhase === "committed")
                && (pending.length !== 0
                    || acknowledged.length === 0
                    || parseEventBody(acknowledged[acknowledged.length - 1]).event !== "task_end"))
            || (checkpoint.taskPhase === "ending"
                && acknowledged.some((body) => parseEventBody(body).event === "task_end"))) {
            return false;
        }

        const terminalIndexes = events
            .map((event, index) => event.event === "task_end" ? index : -1)
            .filter((index) => index >= 0);
        if (terminalIndexes.length > 1
            || (terminalIndexes.length === 1 && terminalIndexes[0] !== events.length - 1)
            || (terminalIndexes.length === 1
                && checkpoint.taskPhase !== "ending"
                && checkpoint.taskPhase !== "accepted"
                && checkpoint.taskPhase !== "committed"
                && checkpoint.taskPhase !== "failed")) {
            return false;
        }

        if (terminalIndexes.length === 1) {
            if (!checkpoint.terminalIntent) {
                return false;
            }
            const terminal = events[terminalIndexes[0]];
            const projected = projectTaskEnd({
                processSubmitted: checkpoint.terminalIntent.processSubmitted,
                requiredFieldsTotal: checkpoint.terminalIntent.requiredFieldsTotal,
                requiredFieldsCompleted: checkpoint.terminalIntent.requiredFieldsCompleted,
                openBlockingValidationCount: checkpoint.terminalIntent.openBlockingValidationCount,
                terminationClassification: checkpoint.terminalIntent.terminationClassification
            });
            const data = terminal.data ?? {};
            const expectedData = {
                end_state: projected.endState,
                mismatch_code: checkpoint.terminalIntent.mismatchCode,
                correctness_basis: checkpoint.terminalIntent.correctnessBasis,
                canonical_transitions: []
            };
            if (JSON.stringify(data) !== JSON.stringify(expectedData)) {
                return false;
            }
        }

        return checkpoint.taskPhase !== "idle" || bodies.length === 0;
    };

    const createTerminalDelivery = (): TerminalDelivery => {
        let resolvePromise: (() => void) | undefined;
        let rejectPromise: ((reason: Error) => void) | undefined;
        const delivery: TerminalDelivery = {
            promise: new Promise<void>((resolve, reject) => {
                resolvePromise = resolve;
                rejectPromise = reject;
            }),
            resolve: (): void => {
                if (!delivery.settled) {
                    delivery.settled = true;
                    resolvePromise?.();
                }
            },
            reject: (reason: Error): void => {
                if (!delivery.settled) {
                    delivery.settled = true;
                    rejectPromise?.(reason);
                }
            },
            settled: false
        };
        return delivery;
    };

    const ensureTerminalDelivery = (session: EvaluationSession): TerminalDelivery => {
        session.terminalDelivery ??= createTerminalDelivery();
        return session.terminalDelivery;
    };

    const clearRetryTimer = (session: EvaluationSession): void => {
        if (session.retryTimer !== null) {
            window.clearTimeout(session.retryTimer);
            session.retryTimer = null;
        }
    };

    const releaseIdentityWriterLease = (session: EvaluationSession): void => {
        const lease = session.identityWriterLease;
        if (!lease || lease.released) {
            return;
        }

        lease.release();
        session.identityWriterLease = null;
        session.releaseIdentityLeaseWhenCaughtUp = false;
    };

    const markSessionFailed = (session: EvaluationSession, errorCode: string): void => {
        const firstFailure = session.taskPhase !== "failed";
        const error = createDeliveryError(errorCode);
        session.taskPhase = "failed";
        session.failureCode = errorCode;
        clearRetryTimer(session);
        session.terminalDelivery?.reject(error);
        if (firstFailure) {
            for (const listener of [...deliveryFailureListeners]) {
                try {
                    listener(session, error);
                } catch (listenerError) {
                    if (typeof console !== "undefined" && typeof console.warn === "function") {
                        console.warn("evaluationTelemetry: delivery failure listener failed", listenerError);
                    }
                }
            }
        }
    };

    const toCheckpoint = (session: EvaluationSession): EvaluationSessionCheckpoint => {
        return {
            version: EvaluationCheckpointVersion,
            sessionId: session.sessionId,
            identity: { ...session.identity },
            stateVersion: session.stateVersion,
            adoptedTransitions: session.adoptedTransitions === null
                ? null
                : [...session.adoptedTransitions],
            transitionAdoptionEntries: session.transitionAdoptionEntries.map((entry) =>
                entry.kind === "event_batch"
                    ? { ...entry, eventIds: [...entry.eventIds] }
                    : { ...entry, transitionIdentities: [...entry.transitionIdentities] }),
            acknowledgedEventBodies: [...session.acknowledgedEventBodies],
            pendingEventBodies: [...session.pendingEventBodies],
            taskPhase: session.taskPhase,
            clientOffsetElapsedMs: session.taskPhase === "idle" ? 0 : readClientOffsetMs(session),
            ...(session.terminalIntent ? { terminalIntent: { ...session.terminalIntent } } : {}),
            ...(session.failureCode ? { failureCode: session.failureCode } : {})
        };
    };

    const persistCheckpoint = (session: EvaluationSession): boolean => {
        if (session.identity.recordProvenance === "demonstration") {
            return true;
        }

        try {
            if (session.taskPhase !== "idle"
                && localStorage.getItem(session.checkpointKey) === null) {
                markSessionFailed(session, CheckpointMissingErrorCode);
                return false;
            }
            localStorage.setItem(session.checkpointKey, JSON.stringify(toCheckpoint(session)));
            return true;
        } catch (error) {
            markSessionFailed(session, StorageUnavailableErrorCode);
            if (typeof console !== "undefined" && typeof console.warn === "function") {
                console.warn("evaluationTelemetry: durable checkpoint persistence failed", error);
            }
            return false;
        }
    };

    const clearCheckpoint = (session: EvaluationSession): boolean => {
        try {
            localStorage.removeItem(session.checkpointKey);
            if (localStorage.getItem(session.checkpointKey) !== null) {
                throw createDeliveryError(StorageUnavailableErrorCode);
            }
            sessionsByCheckpointKey.delete(session.checkpointKey);
            return true;
        } catch (error) {
            if (typeof console !== "undefined" && typeof console.warn === "function") {
                console.warn("evaluationTelemetry: durable checkpoint cleanup failed", error);
            }
            return false;
        }
    };

    const parseCheckpoint = (key: string, raw: string): EvaluationSessionCheckpoint => {
        let parsed: Partial<EvaluationSessionCheckpoint> | null;
        try {
            parsed = JSON.parse(raw) as Partial<EvaluationSessionCheckpoint> | null;
        } catch {
            throw createDeliveryError(CheckpointInvalidErrorCode);
        }

        if (!parsed
            || JSON.stringify(parsed) !== raw
            || parsed.version !== EvaluationCheckpointVersion
            || typeof parsed.sessionId !== "string"
            || !EventIdPattern.test(parsed.sessionId)
            || parsed.identity === null
            || typeof parsed.identity !== "object") {
            throw createDeliveryError(CheckpointInvalidErrorCode);
        }

        const identity = parsed.identity as EvaluationTaskIdentity;
        const expectedCheckpointKeys = [
            "version", "sessionId", "identity", "stateVersion",
            "adoptedTransitions", "transitionAdoptionEntries", "acknowledgedEventBodies",
            "pendingEventBodies", "taskPhase", "clientOffsetElapsedMs",
            ...(parsed.terminalIntent === undefined ? [] : ["terminalIntent"]),
            ...(parsed.failureCode === undefined ? [] : ["failureCode"])
        ];
        if (!hasExactKeys(parsed as Record<string, unknown>, expectedCheckpointKeys)
            || !hasExactKeys(identity as unknown as Record<string, unknown>, [
                "applicationId", "participantId", "condition", "taskId",
                "recordProvenance"
            ])
            || typeof identity.applicationId !== "string"
            || identity.applicationId.trim().length === 0
            || identity.applicationId !== identity.applicationId.trim()
            || typeof identity.participantId !== "string"
            || identity.participantId.trim().length === 0
            || identity.participantId !== identity.participantId.trim()
            || typeof identity.condition !== "string"
            || (identity.condition !== "A" && identity.condition !== "B")
            || typeof identity.taskId !== "string"
            || identity.taskId.trim().length === 0
            || identity.taskId !== identity.taskId.trim()
            || (identity.recordProvenance !== "participant" && identity.recordProvenance !== "operational_probe")
            || key !== getCheckpointKey(identity, parsed.sessionId)
            || !Number.isInteger(parsed.stateVersion)
            || (parsed.stateVersion ?? 0) < 1
            || (parsed.adoptedTransitions !== null
                && (!Array.isArray(parsed.adoptedTransitions)
                    || parsed.adoptedTransitions.some((value) => typeof value !== "string" || value.length === 0)
                    || new Set(parsed.adoptedTransitions).size !== parsed.adoptedTransitions.length))
            || !isTransitionAdoptionCheckpointEntries(parsed.transitionAdoptionEntries)
            || !isEventBodyList(parsed.acknowledgedEventBodies)
            || !isEventBodyList(parsed.pendingEventBodies)
            || !isTaskPhase(parsed.taskPhase)
            || typeof parsed.clientOffsetElapsedMs !== "number"
            || !Number.isInteger(parsed.clientOffsetElapsedMs)
            || parsed.clientOffsetElapsedMs < 0
            || parsed.clientOffsetElapsedMs > MaximumClientOffsetMs
            || (parsed.terminalIntent !== undefined && !isTaskEndOptions(parsed.terminalIntent))
            || (parsed.terminalIntent !== undefined
                && JSON.stringify(parsed.terminalIntent)
                    !== JSON.stringify(normalizeTaskEndOptions(parsed.terminalIntent)))
            || ((parsed.taskPhase === "ending"
                    || parsed.taskPhase === "accepted"
                    || parsed.taskPhase === "committed")
                && parsed.terminalIntent === undefined)
            || (parsed.taskPhase !== "ending"
                && parsed.taskPhase !== "accepted"
                && parsed.taskPhase !== "committed"
                && parsed.taskPhase !== "failed"
                && parsed.terminalIntent !== undefined)
            || (parsed.failureCode !== undefined
                && (typeof parsed.failureCode !== "string" || parsed.failureCode.trim().length === 0))
            || (parsed.taskPhase === "failed" && parsed.failureCode === undefined)
            || (parsed.taskPhase !== "failed" && parsed.failureCode !== undefined)
            || !checkpointBodiesAreCanonical(parsed as EvaluationSessionCheckpoint)) {
            throw createDeliveryError(CheckpointInvalidErrorCode);
        }

        return {
            version: EvaluationCheckpointVersion,
            sessionId: parsed.sessionId,
            identity: { ...identity },
            stateVersion: parsed.stateVersion as number,
            adoptedTransitions: parsed.adoptedTransitions === null
                ? null
                : [...parsed.adoptedTransitions],
            transitionAdoptionEntries: parsed.transitionAdoptionEntries.map((entry) =>
                entry.kind === "event_batch"
                    ? { ...entry, eventIds: [...entry.eventIds] }
                    : { ...entry, transitionIdentities: [...entry.transitionIdentities] }),
            acknowledgedEventBodies: [...parsed.acknowledgedEventBodies],
            pendingEventBodies: [...parsed.pendingEventBodies],
            taskPhase: parsed.taskPhase,
            clientOffsetElapsedMs: parsed.clientOffsetElapsedMs as number,
            ...(parsed.terminalIntent ? { terminalIntent: normalizeTaskEndOptions(parsed.terminalIntent) } : {}),
            ...(parsed.failureCode ? { failureCode: parsed.failureCode } : {})
        };
    };

    const readCheckpoint = (key: string): EvaluationSessionCheckpoint | null => {
        let raw: string | null;
        try {
            raw = localStorage.getItem(key);
        } catch {
            throw createDeliveryError(StorageUnavailableErrorCode);
        }
        return raw === null ? null : parseCheckpoint(key, raw);
    };

    const applyCheckpoint = (session: EvaluationSession, checkpoint: EvaluationSessionCheckpoint): void => {
        session.stateVersion = checkpoint.stateVersion;
        session.adoptedTransitions = checkpoint.adoptedTransitions === null
            ? null
            : [...checkpoint.adoptedTransitions];
        session.transitionAdoptionEntries = checkpoint.transitionAdoptionEntries.map((entry) =>
            entry.kind === "event_batch"
                ? { ...entry, eventIds: [...entry.eventIds] }
                : { ...entry, transitionIdentities: [...entry.transitionIdentities] });
        session.acknowledgedEventBodies = [...checkpoint.acknowledgedEventBodies];
        session.pendingEventBodies = [...checkpoint.pendingEventBodies];
        session.taskPhase = checkpoint.taskPhase;
        session.terminalIntent = checkpoint.terminalIntent
            ? { ...checkpoint.terminalIntent }
            : undefined;
        session.failureCode = checkpoint.failureCode;
        session.clientOffsetOriginMs = checkpoint.taskPhase === "idle"
            ? 0
            : originFromElapsed(checkpoint.clientOffsetElapsedMs);
        if (session.taskPhase === "ending" || session.taskPhase === "accepted" || session.taskPhase === "committed") {
            const terminal = ensureTerminalDelivery(session);
            if (session.taskPhase === "accepted" || session.taskPhase === "committed") {
                terminal.resolve();
            }
        }
    };

    const createSessionFromCheckpoint = (key: string, checkpoint: EvaluationSessionCheckpoint): EvaluationSession => {
        const terminalDelivery = checkpoint.taskPhase === "ending"
            || checkpoint.taskPhase === "accepted"
            || checkpoint.taskPhase === "committed"
            ? createTerminalDelivery()
            : null;
        if (terminalDelivery) {
            // Wiederhergestellte Abschlussvorgänge haben noch keinen wartenden Aufrufer.
            // Die Ablehnung abfangen, aber im ursprünglichen Promise für endTask erhalten.
            void terminalDelivery.promise.catch(() => undefined);
            if (checkpoint.taskPhase === "accepted" || checkpoint.taskPhase === "committed") {
                terminalDelivery.resolve();
            }
        }
        const session: EvaluationSession = {
            sessionId: checkpoint.sessionId,
            checkpointKey: key,
            applicationId: checkpoint.identity.applicationId,
            formId: "",
            identity: { ...checkpoint.identity },
            stateVersion: checkpoint.stateVersion,
            adoptedTransitions: checkpoint.adoptedTransitions === null
                ? null
                : [...checkpoint.adoptedTransitions],
            transitionAdoptionEntries: checkpoint.transitionAdoptionEntries.map((entry) =>
                entry.kind === "event_batch"
                    ? { ...entry, eventIds: [...entry.eventIds] }
                    : { ...entry, transitionIdentities: [...entry.transitionIdentities] }),
            acknowledgedEventBodies: [...checkpoint.acknowledgedEventBodies],
            pendingEventBodies: [...checkpoint.pendingEventBodies],
            taskPhase: checkpoint.taskPhase,
            ...(checkpoint.terminalIntent ? { terminalIntent: { ...checkpoint.terminalIntent } } : {}),
            ...(checkpoint.failureCode ? { failureCode: checkpoint.failureCode } : {}),
            terminalDelivery,
            deliveryTask: null,
            retryAttempt: 0,
            retryTimer: null,
            identityWriterLease: null,
            releaseIdentityLeaseWhenCaughtUp: false,
            clientOffsetOriginMs: checkpoint.taskPhase === "idle"
                ? 0
                : originFromElapsed(checkpoint.clientOffsetElapsedMs)
        };
        return session;
    };

    const restoreCheckpointRegistry = (currentIdentity: EvaluationTaskIdentity): void => {
        let keys: string[];
        try {
            keys = Array.from({ length: localStorage.length }, (_, index) => localStorage.key(index))
                .filter((key): key is string => typeof key === "string" && key.startsWith(EvaluationCheckpointKeyPrefix));
        } catch {
            throw createDeliveryError(StorageUnavailableErrorCode);
        }

        for (const key of keys) {
            // Ein neuer Testlauf übernimmt keine alten Aufgaben anderer Identitäten.
            if (operationalEntryRequested && !key.startsWith(getCheckpointKey(currentIdentity, ""))) {
                continue;
            }
            const checkpoint = readCheckpoint(key);
            if (!checkpoint || !mayDeliverIdentity(checkpoint.identity)) {
                continue;
            }

            const existing = sessionsByCheckpointKey.get(key);
            if (existing) {
                applyCheckpoint(existing, checkpoint);
            } else {
                sessionsByCheckpointKey.set(key, createSessionFromCheckpoint(key, checkpoint));
            }
        }

        const registered = Array.from(sessionsByCheckpointKey.values());
        for (let leftIndex = 0; leftIndex < registered.length; leftIndex += 1) {
            for (let rightIndex = leftIndex + 1; rightIndex < registered.length; rightIndex += 1) {
                if (registered[leftIndex].sessionId === registered[rightIndex].sessionId
                    && identitiesEqual(registered[leftIndex].identity, registered[rightIndex].identity)) {
                    throw createDeliveryError(CheckpointInvalidErrorCode);
                }
            }
        }
    };

    const createTaskEndEvent = (session: EvaluationSession, options: TaskEndOptions): TelemetryEventEnvelope => {
        const projected = projectTaskEnd({
            processSubmitted: options.processSubmitted,
            requiredFieldsTotal: options.requiredFieldsTotal,
            requiredFieldsCompleted: options.requiredFieldsCompleted,
            openBlockingValidationCount: options.openBlockingValidationCount,
            terminationClassification: options.terminationClassification
        });

        return createEvent(session, "task_end", {
            end_state: projected.endState,
            mismatch_code: options.mismatchCode,
            correctness_basis: options.correctnessBasis
        });
    };

    const isTransientDeliveryFailure = (status: number): boolean =>
        status === 408 || status === 429 || status >= 500;

    const scheduleRetry = (session: EvaluationSession): void => {
        if (session.retryTimer !== null
            || session.taskPhase === "failed"
            || session.taskPhase === "ended") {
            return;
        }

        const exponent = Math.min(session.retryAttempt, 16);
        const delay = Math.min(
            InitialRetryDelayMilliseconds * (2 ** exponent),
            MaximumRetryDelayMilliseconds);
        session.retryAttempt += 1;
        session.retryTimer = window.setTimeout(() => {
            session.retryTimer = null;
            void pumpPendingEvents(session);
        }, delay);
    };

    const failPermanentDelivery = (session: EvaluationSession): void => {
        markSessionFailed(session, PermanentRejectionErrorCode);
        if (persistCheckpoint(session)) {
            releaseIdentityWriterLease(session);
        }
    };

    const acknowledgePersistedHead = (
        session: EvaluationSession,
        acknowledgedBody: string
    ): boolean => {
        let checkpoint: EvaluationSessionCheckpoint | null;
        try {
            checkpoint = readCheckpoint(session.checkpointKey);
        } catch {
            markSessionFailed(session, StorageUnavailableErrorCode);
            return false;
        }

        if (!checkpoint) {
            markSessionFailed(session, CheckpointMissingErrorCode);
            return false;
        }

        applyCheckpoint(session, checkpoint);
        if (session.taskPhase === "failed") {
            markSessionFailed(session, session.failureCode ?? CheckpointInvalidErrorCode);
            releaseIdentityWriterLease(session);
            return false;
        }
        const persistedHead = session.pendingEventBodies[0];
        if (persistedHead !== acknowledgedBody) {
            failPermanentDelivery(session);
            return false;
        }

        const acknowledgedEvent = parseEventBody(persistedHead);
        session.pendingEventBodies.shift();
        session.acknowledgedEventBodies.push(acknowledgedBody);
        session.retryAttempt = 0;

        if (acknowledgedEvent.event === "task_end") {
            session.taskPhase = "accepted";
            if (!persistCheckpoint(session)) {
                return false;
            }
            session.terminalDelivery?.resolve();
            return false;
        }

        return persistCheckpoint(session);
    };

    const drainPendingEvents = async (session: EvaluationSession): Promise<void> => {
        let checkpoint: EvaluationSessionCheckpoint | null;
        try {
            checkpoint = readCheckpoint(session.checkpointKey);
        } catch (error) {
            markSessionFailed(session, error instanceof Error ? error.message : StorageUnavailableErrorCode);
            return;
        }

        if (!checkpoint) {
            markSessionFailed(session, CheckpointMissingErrorCode);
            return;
        }
        applyCheckpoint(session, checkpoint);
        if (session.taskPhase === "failed") {
            markSessionFailed(session, session.failureCode ?? CheckpointInvalidErrorCode);
            releaseIdentityWriterLease(session);
            return;
        }

        while (session.taskPhase !== "ended" && mayDeliverIdentity(session.identity)) {
            if (session.pendingEventBodies.length === 0) {
                if (session.taskPhase !== "ending" || !session.terminalIntent) {
                    return;
                }

                session.pendingEventBodies.push(JSON.stringify(createTaskEndEvent(session, session.terminalIntent)));
                if (!persistCheckpoint(session)) {
                    return;
                }
            }

            const serializedEvent = session.pendingEventBodies[0];
            let response: Response;
            try {
                response = await fetch(getTelemetryEndpoint(session.identity), {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: serializedEvent,
                    keepalive: true
                });
            } catch (reason) {
                if (typeof console !== "undefined" && typeof console.warn === "function") {
                    console.warn("evaluationTelemetry: delivery not acknowledged; event remains pending", reason);
                }
                scheduleRetry(session);
                return;
            }

            if (response.status !== 204) {
                if (typeof console !== "undefined" && typeof console.warn === "function") {
                    const disposition = isTransientDeliveryFailure(response.status)
                        ? "event remains pending for retry"
                        : "event is preserved in a faulted checkpoint without retry";
                    console.warn(`evaluationTelemetry: endpoint returned ${response.status}; ${disposition}`);
                }
                if (isTransientDeliveryFailure(response.status)) {
                    scheduleRetry(session);
                } else {
                    failPermanentDelivery(session);
                }
                return;
            }

            if (!acknowledgePersistedHead(session, serializedEvent)) {
                return;
            }
        }
    };

    const pumpPendingEvents = (session: EvaluationSession): Promise<void> => {
        if (!mayDeliverIdentity(session.identity)) {
            return Promise.resolve();
        }
        if (session.deliveryTask) {
            return session.deliveryTask;
        }

        if (session.identity.recordProvenance !== "demonstration"
            && !session.identityWriterLease) {
            return Promise.resolve();
        }

        const trackedDeliveryTask = drainPendingEvents(session).catch((reason) => {
            if (typeof console !== "undefined" && typeof console.warn === "function") {
                console.warn("evaluationTelemetry: delivery pump failed", reason);
            }
            scheduleRetry(session);
        }).finally(() => {
            session.deliveryTask = null;
            if (session.releaseIdentityLeaseWhenCaughtUp
                && ((session.taskPhase === "active"
                        && session.pendingEventBodies.length === 0
                        && session.retryTimer === null)
                    || session.taskPhase === "accepted"
                    || session.taskPhase === "committed")) {
                releaseIdentityWriterLease(session);
                return;
            }
            if (session.retryTimer === null
                && session.taskPhase !== "failed"
                && session.taskPhase !== "ended"
                && (session.pendingEventBodies.length > 0 || session.taskPhase === "ending")) {
                window.setTimeout(() => void pumpPendingEvents(session), 0);
            }
        });
        session.deliveryTask = trackedDeliveryTask;
        return trackedDeliveryTask;
    };

    const drainRegisteredOrphan = async (session: EvaluationSession): Promise<void> => {
        if (!mayDeliverIdentity(session.identity) || session.identity.recordProvenance === "operational_probe") {
            return;
        }
        if (session.identityWriterLease
            || session.taskPhase === "failed"
            || session.taskPhase === "ended"
            || (session.pendingEventBodies.length === 0 && session.taskPhase !== "ending")) {
            return;
        }

        let lease: EvaluationIdentityWriterLease;
        try {
            lease = await acquireIdentityWriterLease(session.identity);
        } catch (reason) {
            if (reason instanceof Error && reason.message === IdentityWriterUnavailableErrorCode) {
                // Ein anderer Tab hält die Schreibsperre. Seine Warteschlange
                // darf nicht aus diesem lokalen Sitzungsobjekt versendet werden.
                return;
            }
            if (typeof console !== "undefined" && typeof console.warn === "function") {
                console.warn("evaluationTelemetry: orphan writer lease request failed", reason);
            }
            return;
        }

        session.identityWriterLease = lease;
        // Ohne zugehörige Oberfläche kann der Abschluss nicht lokal festgeschrieben werden.
        // Nach Versand der offenen Ereignisse beziehungsweise bestätigtem Aufgabenende
        // die Sperre freigeben, damit die passende Seite fortsetzen kann.
        session.releaseIdentityLeaseWhenCaughtUp = true;
        let checkpoint: EvaluationSessionCheckpoint | null;
        try {
            checkpoint = readCheckpoint(session.checkpointKey);
        } catch (reason) {
            markSessionFailed(session, reason instanceof Error ? reason.message : StorageUnavailableErrorCode);
            return;
        }

        if (!checkpoint) {
            markSessionFailed(session, CheckpointMissingErrorCode);
            return;
        }

        applyCheckpoint(session, checkpoint);
        if ((session.taskPhase as EvaluationTaskPhase) === "failed") {
            releaseIdentityWriterLease(session);
            return;
        }

        void pumpPendingEvents(session);
    };

    /** Ereignisname und Nutzdaten vor Ergänzung der gemeinsamen Identitätsangaben. */
    interface TelemetryEventInput {
        eventName: string;
        data?: Record<string, unknown>;
    }

    const enqueueEvents = (
        session: EvaluationSession,
        inputs: TelemetryEventInput[]
    ): void => {
        if (inputs.length === 0) {
            return;
        }

        if (session.identity.recordProvenance === "demonstration") {
            return;
        }

        if (!mayDeliverIdentity(session.identity)) {
            throw new Error("telemetry.probe.context_unavailable");
        }
        if (session.taskPhase === "ended" || session.taskPhase === "failed") {
            return;
        }

        if (session.taskPhase === "ending"
            || session.taskPhase === "accepted"
            || session.taskPhase === "committed") {
            throw new Error(`Cannot record ${inputs[0].eventName} while the evaluation task is ${session.taskPhase}.`);
        }

        const events = createEvents(session, inputs);
        session.pendingEventBodies.push(...events.map((event) => JSON.stringify(event)));
        if (persistCheckpoint(session)) {
            void pumpPendingEvents(session);
        } else {
            throw createDeliveryError(session.failureCode ?? StorageUnavailableErrorCode);
        }
    };

    const enqueueEvent = (
        session: EvaluationSession,
        eventName: string,
        data?: Record<string, unknown>
    ): void => enqueueEvents(session, [{ eventName, data }]);

    // Die Übergangsidentität enthält keine Zustandsversion, da sie erst zur
    // Entscheidung über eine neue Version verwendet wird.
    const transitionIdentity = (transition: CanonicalTransition): string =>
        [transition.canonical_class, transition.scope, transition.object_id, transition.resulting_state].join("\u001F");

    const transitionDimension = (identity: string): string => {
        const members = identity.split("\u001F");
        if (members.length !== 4 || members.some((member) => member.trim().length === 0)) {
            throw new Error("A canonical transition adoption identity is malformed.");
        }

        switch (members[0]) {
            case "field_target_changed":
            case "field_group_target_changed":
                return members.slice(0, 2).join("\u001F");
            case "field_completion_changed":
            case "field_group_completion_changed":
            case "confirmed_field_value_changed":
                return members.slice(0, 3).join("\u001F");
            default:
                throw new Error(`Canonical transition adoption class '${members[0]}' is not supported.`);
        }
    };

    const belongsToCurrentAdoption = (adopted: string[], incoming: string[]): boolean => {
        const hasConflictingDimension = incoming.some((candidate) => adopted.some((current) =>
            transitionDimension(candidate) === transitionDimension(current) && candidate !== current));
        return !hasConflictingDimension && incoming.some((candidate) => adopted.includes(candidate));
    };

    const isOccurrenceTransition = (transition: CanonicalTransition): boolean =>
        transition.canonical_class === "confirmed_field_value_changed";

    const primeCompletionAdoption = (
        adoptedTransitions: string[] | null,
        completionTransitionIdentities: string[]
    ): string[] | null => {
        const dimensions = completionTransitionIdentities.map(transitionDimension);
        if (new Set(dimensions).size !== dimensions.length
            || !completionTransitionIdentities.every(isCompletionTransitionIdentity)) {
            throw new Error("A completion baseline contains an invalid transition adoption identity.");
        }

        if (completionTransitionIdentities.length === 0) {
            return adoptedTransitions;
        }

        const primed = adoptedTransitions === null ? [] : [...adoptedTransitions];
        const adoptedByDimension = new Map(
            primed.map((identity) => [transitionDimension(identity), identity]));
        for (const identity of completionTransitionIdentities) {
            const dimension = transitionDimension(identity);
            const current = adoptedByDimension.get(dimension);
            if (current !== undefined && current !== identity) {
                throw new Error("A completion baseline conflicts with the durably adopted state.");
            }
            if (current === undefined) {
                adoptedByDimension.set(dimension, identity);
                primed.push(identity);
            }
        }

        return primed;
    };

    /** Kanonischer Folgezustand und die dafür neu gezählten Übergänge. */
    interface CanonicalTransitionAdoption {
        stateVersion: number;
        adoptedTransitions: string[] | null;
        emittedTransitions: CanonicalTransition[];
    }

    // Eine Navigationsnutzlast ist eine Momentaufnahme, während `canonical_transitions` die Differenz beschreibt.
    // Wechselt ein Feld in derselben Gruppe, muss die unveränderte Gruppenidentität im übernommenen Zustand bleiben,
    // darf aber in der neuen Zustandsversion nicht noch einmal zählen.
    const advanceAdoption = (
        stateVersion: number,
        adoptedTransitions: string[] | null,
        projectedTransitions: CanonicalTransition[]
    ): CanonicalTransitionAdoption => {
        const incoming = projectedTransitions.map(transitionIdentity);
        if (incoming.length === 0) {
            return { stateVersion, adoptedTransitions, emittedTransitions: [] };
        }

        if (adoptedTransitions === null) {
            return {
                stateVersion,
                adoptedTransitions: incoming,
                emittedTransitions: projectedTransitions
            };
        }

        const adoptedByDimension = new Map(
            adoptedTransitions.map((identity) => [transitionDimension(identity), identity]));
        if (projectedTransitions.some(isOccurrenceTransition)) {
            for (const identity of incoming) {
                adoptedByDimension.set(transitionDimension(identity), identity);
            }

            return {
                stateVersion: stateVersion + 1,
                adoptedTransitions: Array.from(adoptedByDimension.values()),
                emittedTransitions: projectedTransitions
            };
        }

        if (belongsToCurrentAdoption(adoptedTransitions, incoming)) {
            return {
                stateVersion,
                adoptedTransitions: adoptedTransitions.concat(
                    incoming.filter((identity) => !adoptedTransitions.includes(identity))),
                emittedTransitions: projectedTransitions.filter((transition) =>
                    !adoptedTransitions.includes(transitionIdentity(transition)))
            };
        }

        const emittedTransitions = projectedTransitions.filter((transition) =>
            adoptedByDimension.get(transitionDimension(transitionIdentity(transition)))
                !== transitionIdentity(transition));
        for (const identity of incoming) {
            adoptedByDimension.set(transitionDimension(identity), identity);
        }

        return {
            stateVersion: stateVersion + 1,
            adoptedTransitions: Array.from(adoptedByDimension.values()),
            emittedTransitions
        };
    };

    const createEvents = (
        session: EvaluationSession,
        inputs: TelemetryEventInput[]
    ): TelemetryEventEnvelope[] => {
        // Übergänge vor der Versionsvergabe projizieren. AdvanceAdoption entscheidet
        // anschließend über Zustandsversion und tatsächlich neue Übergänge.
        const projectedByEvent = inputs.map(({ eventName, data }) =>
            projectCanonicalTransitions(eventName, data));
        const projectedTransitions = projectedByEvent.flat();

        const adoption = advanceAdoption(
            session.stateVersion,
            session.adoptedTransitions,
            projectedTransitions);
        session.stateVersion = adoption.stateVersion;
        session.adoptedTransitions = adoption.adoptedTransitions;
        const emittedIdentities = new Set(adoption.emittedTransitions.map(transitionIdentity));

        const events = inputs.map(({ eventName, data }, index) => ({
            event_id: generateOpaqueId(),
            session_id: session.sessionId,
            client_offset_ms: readClientOffsetMs(session),
            participant_id: session.identity.participantId,
            condition: session.identity.condition,
            application_id: session.applicationId,
            task_id: session.identity.taskId,
            // Die gebundene Herkunft bleibt beim nativen Ereignis und seinem Checkpoint.
            // Ereignisse aus Testläufen werden ausschließlich über den aktivierten Probe-Pfad zugestellt.
            record_provenance: session.identity.recordProvenance,
            event: eventName,
            state_version: adoption.stateVersion,
            // Jedes Ereignis erhält seine kanonischen Übergänge, gegebenenfalls als leere Liste.
            // Die zentrale Projektion hält die Definition von Zustandswechseln einheitlich.
            data: {
                ...(data ?? {}),
                canonical_transitions: projectedByEvent[index].filter((transition) =>
                    emittedIdentities.has(transitionIdentity(transition)))
            }
        }));
        if (session.transitionAdoptionEntries.length >= MaximumTransitionAdoptionEntries) {
            throw createDeliveryError(CheckpointInvalidErrorCode);
        }
        session.transitionAdoptionEntries.push({
            kind: "event_batch",
            stateVersion: adoption.stateVersion,
            eventIds: events.map((event) => event.event_id)
        });
        return events;
    };

    const createEvent = (
        session: EvaluationSession,
        eventName: string,
        data?: Record<string, unknown>
    ): TelemetryEventEnvelope => createEvents(session, [{ eventName, data }])[0];

    const startSession = async (
        options: StartSessionOptions,
        resetApplicationState?: () => void
    ): Promise<EvaluationSession> => {
        const hasBoundParticipantToken = studyRunIsParticipantInvited(options);
        if ((options.operationalProbe && hasBoundParticipantToken)
            || (operationalEntryRequested && !options.operationalProbe)) {
            throw new Error("telemetry.probe.context_invalid");
        }
        const configuredParticipantId = normalizeParticipantId(options.participantId);
        const identity: EvaluationTaskIdentity = options.operationalProbe
            ? getOperationalProbeIdentity(options.applicationId, options.formId)
            : {
            applicationId: options.applicationId.trim(),
            participantId: hasBoundParticipantToken
                ? configuredParticipantId
                : telemetryUnknownParticipantId,
            taskId: normalizeTaskId(options.formId),
            condition: normalizeCondition(options.condition, hasBoundParticipantToken),
            recordProvenance: hasBoundParticipantToken ? "participant" : "demonstration"
        };
        if (!hasBoundParticipantToken && !options.operationalProbe) {
            const sessionId = generateOpaqueId();
            return {
                sessionId,
                checkpointKey: getCheckpointKey(identity, sessionId),
                applicationId: identity.applicationId,
                formId: options.formId.trim(),
                identity,
                stateVersion: 1,
                adoptedTransitions: null,
                transitionAdoptionEntries: [],
                acknowledgedEventBodies: [],
                pendingEventBodies: [],
                taskPhase: "idle",
                terminalDelivery: null,
                deliveryTask: null,
                retryAttempt: 0,
                retryTimer: null,
                identityWriterLease: null,
                releaseIdentityLeaseWhenCaughtUp: false,
                clientOffsetOriginMs: 0
            };
        }

        if (!navigator.locks || typeof navigator.locks.request !== "function") {
            throw createDeliveryError(WebLocksUnavailableErrorCode);
        }

        const lease = await acquireIdentityWriterLease(identity);
        try {
            if (resetApplicationState) {
                // Beim sichtbaren Neustart offene Ereignisse eines alten Laufs unter
                // dessen Sitzungskennung erhalten und anschließend weiter versenden.
                const prefix = getCheckpointKey(identity, "");
                try {
                    const keys = Array.from({ length: localStorage.length }, (_, index) => localStorage.key(index))
                        .filter((key): key is string => typeof key === "string" && key.startsWith(prefix));
                    for (const key of keys) {
                        let checkpoint: EvaluationSessionCheckpoint | null = null;
                        try {
                            checkpoint = readCheckpoint(key);
                        } catch {
                            checkpoint = null;
                        }
                        if (checkpoint
                            && identitiesEqual(checkpoint.identity, identity)
                            && checkpoint.taskPhase !== "failed"
                            && (checkpoint.pendingEventBodies.length > 0
                                || checkpoint.taskPhase === "ending")) {
                            continue;
                        }
                        localStorage.removeItem(key);
                        if (localStorage.getItem(key) !== null) {
                            throw createDeliveryError(StorageUnavailableErrorCode);
                        }
                    }
                } catch (reason) {
                    if (reason instanceof Error && reason.name === "EvaluationTelemetryDeliveryError") {
                        throw reason;
                    }
                    throw createDeliveryError(StorageUnavailableErrorCode);
                }
                for (const [key, registered] of sessionsByCheckpointKey) {
                    if (!identitiesEqual(registered.identity, identity)) {
                        continue;
                    }
                    if (registered.taskPhase !== "failed"
                        && (registered.pendingEventBodies.length > 0
                            || registered.taskPhase === "ending")) {
                        continue;
                    }
                    clearRetryTimer(registered);
                    sessionsByCheckpointKey.delete(key);
                }
                resetApplicationState();
            }
            restoreCheckpointRegistry(identity);
            const candidates = resetApplicationState
                ? []
                : Array.from(sessionsByCheckpointKey.values())
                    .filter((candidate) => identitiesEqual(candidate.identity, identity)
                        && candidate.taskPhase !== "ended");

            const liveCandidates = candidates.filter((candidate) =>
                candidate.pendingEventBodies.length > 0
                || candidate.taskPhase === "ending"
                || candidate.taskPhase === "active"
                || candidate.taskPhase === "idle");
            if (!resetApplicationState && liveCandidates.length > 1) {
                throw createDeliveryError(CheckpointInvalidErrorCode);
            }

            let session = resetApplicationState ? undefined : candidates[0];
            if (session) {
                session.formId = options.formId.trim();
                session.identityWriterLease = lease;
                session.releaseIdentityLeaseWhenCaughtUp = false;
                if (session.taskPhase === "failed") {
                    releaseIdentityWriterLease(session);
                    throw createDeliveryError(session.failureCode ?? PermanentRejectionErrorCode);
                }
                if (!persistCheckpoint(session)) {
                    throw createDeliveryError(StorageUnavailableErrorCode);
                }
            } else {
                const sessionId = generateOpaqueId();
                const checkpointKey = getCheckpointKey(identity, sessionId);
                session = {
                    sessionId,
                    checkpointKey,
                    applicationId: identity.applicationId,
                    formId: options.formId.trim(),
                    identity,
                    stateVersion: 1,
                    adoptedTransitions: null,
                    transitionAdoptionEntries: [],
                    acknowledgedEventBodies: [],
                    pendingEventBodies: [],
                    taskPhase: "idle",
                    terminalDelivery: null,
                    deliveryTask: null,
                    retryAttempt: 0,
                    retryTimer: null,
                    identityWriterLease: lease,
                    releaseIdentityLeaseWhenCaughtUp: false,
                    clientOffsetOriginMs: 0
                };
                sessionsByCheckpointKey.set(checkpointKey, session);
                if (!persistCheckpoint(session)) {
                    throw createDeliveryError(StorageUnavailableErrorCode);
                }
            }

            for (const registeredSession of sessionsByCheckpointKey.values()) {
                if (registeredSession === session) {
                    continue;
                }
                if (identitiesEqual(registeredSession.identity, session.identity)
                    && (registeredSession.pendingEventBodies.length > 0
                        || registeredSession.taskPhase === "ending")) {
                    // Die vorhandene Identitätssperre gilt auch für den noch offenen alten Lauf.
                    registeredSession.identityWriterLease = lease;
                    registeredSession.releaseIdentityLeaseWhenCaughtUp = false;
                    void pumpPendingEvents(registeredSession);
                    continue;
                }
                void drainRegisteredOrphan(registeredSession);
            }

            if (session.pendingEventBodies.length > 0 || session.taskPhase === "ending") {
                void pumpPendingEvents(session);
            }

            return session;
        } catch (reason) {
            if (!lease.released) {
                lease.release();
            }
            throw reason;
        }
    };

    const endSession = (session: EvaluationSession, _outcome: SessionOutcome): boolean => {
        // Der Telemetrievertrag enthält keine eigenen Ereignisse für den Sitzungsabschluss.
        lastFocusTokenBySession.delete(session.sessionId);
        if (session.taskPhase === "ended") {
            return true;
        }
        if ((session.taskPhase === "idle" || session.taskPhase === "committed")
            && (session.identity.participantId === telemetryUnknownParticipantId
                || clearCheckpoint(session))) {
            session.taskPhase = "ended";
            session.terminalIntent = undefined;
            session.failureCode = undefined;
            session.acknowledgedEventBodies = [];
            session.pendingEventBodies = [];
            session.transitionAdoptionEntries = [];
            releaseIdentityWriterLease(session);
            return true;
        }
        return false;
    };

    const startTask = (session: EvaluationSession): void => {
        if (session.identity.participantId === telemetryUnknownParticipantId) {
            return;
        }

        if (!session.identityWriterLease) {
            throw createDeliveryError(IdentityWriterUnavailableErrorCode);
        }

        if (session.taskPhase === "active" || session.taskPhase === "ending") {
            return;
        }

        if (session.taskPhase === "failed" || session.taskPhase === "accepted"
            || session.taskPhase === "committed" || session.taskPhase === "ended") {
            if (session.taskPhase === "failed") {
                throw createDeliveryError(session.failureCode ?? PermanentRejectionErrorCode);
            }
            return;
        }

        session.acknowledgedEventBodies = [];
        session.pendingEventBodies = [];
        session.transitionAdoptionEntries = [];
        session.taskPhase = "active";
        session.terminalIntent = undefined;
        session.clientOffsetOriginMs = performance.now();
        enqueueEvent(session, "task_start");
    };

    const endTask = (session: EvaluationSession, options: TaskEndOptions): Promise<void> => {
        if (session.identity.participantId === telemetryUnknownParticipantId) {
            return Promise.resolve();
        }

        if (session.taskPhase === "ending" || session.taskPhase === "accepted" || session.taskPhase === "committed") {
            const normalized = normalizeTaskEndOptions(options);
            if (JSON.stringify(normalized) !== JSON.stringify(session.terminalIntent)) {
                throw new Error("Cannot replace an evaluation task's persisted terminal intent.");
            }

            const terminal = ensureTerminalDelivery(session);
            if (session.taskPhase === "ending") {
                void pumpPendingEvents(session);
            }
            return terminal.promise;
        }

        if (session.taskPhase === "failed") {
            return Promise.reject(createDeliveryError(
                session.failureCode ?? PermanentRejectionErrorCode));
        }

        if (session.taskPhase !== "active") {
            return Promise.resolve();
        }

        const terminalIntent = normalizeTaskEndOptions(options);
        session.taskPhase = "ending";
        session.terminalIntent = terminalIntent;
        const terminal = ensureTerminalDelivery(session);
        if (!persistCheckpoint(session)) {
            return terminal.promise;
        }

        void pumpPendingEvents(session);
        return terminal.promise;
    };

    const commitTask = (session: EvaluationSession): void => {
        if (session.identity.participantId === telemetryUnknownParticipantId) {
            session.taskPhase = "committed";
            return;
        }
        if (session.taskPhase === "committed" || session.taskPhase === "ended") {
            return;
        }
        if (session.taskPhase !== "accepted") {
            throw new Error(`Cannot commit an evaluation task while it is ${session.taskPhase}.`);
        }

        session.taskPhase = "committed";
        if (!persistCheckpoint(session)) {
            throw createDeliveryError(session.failureCode ?? StorageUnavailableErrorCode);
        }
    };

    const switchForm = (session: EvaluationSession, newFormId: string): EvaluationSession => {
        if (session.identity.recordProvenance === "operational_probe"
            && newFormId.trim() !== session.identity.taskId) {
            throw new Error("telemetry.probe.task_mismatch");
        }
        session.formId = newFormId.trim();
        return session;
    };

    const trackNavigationChanged = (session: EvaluationSession, data: StatusNavigationData): void => {
        enqueueEvent(session, "navigation_changed", {
            current_field_id: sanitizeId(data.current_field_id),
            current_field_group_id: sanitizeId(data.current_field_group_id),
            navigation_index: sanitizePositiveNumber(data.navigation_index),
            navigation_total: sanitizePositiveNumber(data.navigation_total)
        });
    };

    const primeFieldAndGroupCompletionBaseline = (
        session: EvaluationSession,
        baseline: FieldAndGroupCompletionBaseline
    ): void => {
        if (session.identity.participantId === telemetryUnknownParticipantId) {
            return;
        }
        if (session.taskPhase !== "active") {
            throw new Error(`Cannot prime a completion baseline while the evaluation task is ${session.taskPhase}.`);
        }

        try {
            if (!baseline || !Array.isArray(baseline.fields) || !Array.isArray(baseline.fieldGroups)) {
                throw new Error("A completion baseline must contain field and field-group snapshots.");
            }
            const transitions: CanonicalTransition[] = [];
            for (const field of baseline.fields) {
                const projected = field && typeof field.completed === "boolean"
                    ? projectCanonicalTransitions("field_completion_changed", {
                        field_id: sanitizeId(field.fieldId),
                        completed: field.completed
                    })
                    : [];
                if (projected.length !== 1) {
                    throw new Error("A field completion baseline requires one modeled object identity.");
                }
                transitions.push(projected[0]);
            }
            for (const fieldGroup of baseline.fieldGroups) {
                const projected = fieldGroup && typeof fieldGroup.completed === "boolean"
                    ? projectCanonicalTransitions("field_group_completion_changed", {
                        field_group_id: sanitizeId(fieldGroup.fieldGroupId),
                        completed: fieldGroup.completed
                    })
                    : [];
                if (projected.length !== 1) {
                    throw new Error("A field-group completion baseline requires one modeled object identity.");
                }
                transitions.push(projected[0]);
            }
            const transitionIdentities = transitions.map(transitionIdentity);
            if (transitionIdentities.length === 0) {
                return;
            }
            session.adoptedTransitions = primeCompletionAdoption(
                session.adoptedTransitions,
                transitionIdentities);
            if (session.transitionAdoptionEntries.length >= MaximumTransitionAdoptionEntries) {
                throw createDeliveryError(CheckpointInvalidErrorCode);
            }
            session.transitionAdoptionEntries.push({
                kind: "completion_baseline",
                stateVersion: session.stateVersion,
                transitionIdentities
            });
        } catch {
            markSessionFailed(session, CheckpointInvalidErrorCode);
            persistCheckpoint(session);
            throw createDeliveryError(CheckpointInvalidErrorCode);
        }

        if (!persistCheckpoint(session)) {
            throw createDeliveryError(session.failureCode ?? StorageUnavailableErrorCode);
        }
    };

    const trackFieldAndGroupStatusChanges = (
        session: EvaluationSession,
        changes: FieldAndGroupStatusChanges
    ): void => {
        const events: TelemetryEventInput[] = [];
        for (const field of changes.fields) {
            const common = {
                field_id: sanitizeId(field.fieldId),
                field_group_id: sanitizeId(field.fieldGroupId),
                relevant: field.relevant
            };
            if (field.relevanceChanged) {
                events.push({ eventName: "field_relevance_changed", data: common });
            }
            if (field.completionChanged) {
                const data: Record<string, unknown> = {
                    ...common,
                    required: field.required,
                    completed: field.completed
                };
                const blockingErrorCodes = sanitizeRelatedFieldIds(field.blockingErrorCodes);
                if (blockingErrorCodes) {
                    data.blocking_error_codes = blockingErrorCodes;
                }
                events.push({ eventName: "field_completion_changed", data });
            }
        }

        for (const fieldGroup of changes.fieldGroups) {
            const common = {
                field_group_id: sanitizeId(fieldGroup.fieldGroupId),
                relevant: fieldGroup.relevant
            };
            if (fieldGroup.relevanceChanged) {
                events.push({ eventName: "field_group_relevance_changed", data: common });
            }
            if (fieldGroup.completionChanged) {
                const data: Record<string, unknown> = {
                    ...common,
                    completed: fieldGroup.completed
                };
                const openGroupErrorCodes = sanitizeRelatedFieldIds(fieldGroup.openGroupErrorCodes);
                if (openGroupErrorCodes) {
                    data.open_group_error_codes = openGroupErrorCodes;
                }
                events.push({ eventName: "field_group_completion_changed", data });
            }
        }

        enqueueEvents(session, events);
    };

    const trackStatusSnapshot = (session: EvaluationSession, data: StatusSnapshotData): void => {
        const payload: Record<string, unknown> = {
            navigation: {
                current_field_id: sanitizeId(data.navigation.current_field_id),
                current_field_group_id: sanitizeId(data.navigation.current_field_group_id),
                navigation_index: sanitizePositiveNumber(data.navigation.navigation_index),
                navigation_total: sanitizePositiveNumber(data.navigation.navigation_total)
            },
            progress: {
                allFields: {
                    completed: sanitizePositiveNumber(data.progress.allFields.completed),
                    total: sanitizePositiveNumber(data.progress.allFields.total)
                },
                requiredFieldsOnly: {
                    completed: sanitizePositiveNumber(data.progress.requiredFieldsOnly.completed),
                    total: sanitizePositiveNumber(data.progress.requiredFieldsOnly.total)
                },
                fieldGroupBased: {
                    completed: sanitizePositiveNumber(data.progress.fieldGroupBased.completed),
                    total: sanitizePositiveNumber(data.progress.fieldGroupBased.total)
                }
            }
        };

        if (data.zero_denominator_mode) {
            payload.zero_denominator_mode = data.zero_denominator_mode;
        }

        enqueueEvent(session, "status_snapshot", payload);
    };

    const trackFieldFocus = (session: EvaluationSession, fieldId: string, fieldGroupId?: string): void => {
        const normalizedFieldId = sanitizeId(fieldId);
        const normalizedFieldGroupId = sanitizeOptionalString(fieldGroupId) ?? telemetryUnidentifiedObject;
        const focusToken = `${session.formId}${FocusTokenSeparator}${normalizedFieldGroupId}${FocusTokenSeparator}${normalizedFieldId}`;

        if (lastFocusTokenBySession.get(session.sessionId) === focusToken) {
            return;
        }

        lastFocusTokenBySession.set(session.sessionId, focusToken);
        enqueueEvent(session, "focus_change", {
            field_id: normalizedFieldId
        });
    };

    const trackFieldAttempt = (session: EvaluationSession, fieldId: string, _inputMode: string, success: boolean): void => {
        if (!success) {
            return;
        }

        enqueueEvent(session, "value_change", {
            field_id: sanitizeId(fieldId)
        });
    };

    // Den gemeldeten Regelcode über die Modellzuordnung einer Studiendiagnose zuordnen.
    // Das Ereignis trägt die Studiendiagnose, nicht den ursprünglichen Regelcode.
    const trackValidationErrorOpened = (
        session: EvaluationSession,
        errorCode: string,
        severity: string,
        trigger: ErrorTrigger,
        scope?: ContractScope | string,
        fieldId?: string,
        fieldGroupId?: string,
        relatedFieldIds?: string[]
    ): void => {
        if (telemetryStudyErrorRoute(errorCode) !== "validation") {
            return;
        }

        enqueueEvent(session, "validation_error_opened", createErrorData(
            telemetryStudyValidationDiagnosis,
            severity,
            scope,
            fieldId,
            fieldGroupId,
            relatedFieldIds,
            trigger
        ));
    };

    const trackValidationErrorCleared = (
        session: EvaluationSession,
        errorCode: string,
        severity: string,
        scope?: ContractScope | string,
        fieldId?: string,
        fieldGroupId?: string,
        relatedFieldIds?: string[]
    ): void => {
        if (telemetryStudyErrorRoute(errorCode) !== "validation") {
            return;
        }

        enqueueEvent(session, "validation_error_cleared", createErrorData(
            telemetryStudyValidationDiagnosis,
            severity,
            scope,
            fieldId,
            fieldGroupId,
            relatedFieldIds
        ));
    };

    const trackNavigationError = (session: EvaluationSession, scope?: ContractScope | string): void => {
        enqueueEvent(session, "navigation_error", createErrorData(telemetryStudyNavigationDiagnosis, "error", scope));
    };

    const trackAmbiguityResolved = (
        session: EvaluationSession,
        ambiguityType: AmbiguityType,
        resolutionStrategy: AmbiguityResolutionStrategy,
        signalClass: AmbiguitySignalClass,
        result: AmbiguityResult,
        fieldId?: string,
        commandId?: string
    ): void => {
        const data: Record<string, unknown> = {
            ambiguity_type: sanitizeVocabularyValue(ambiguityType),
            resolution_strategy: sanitizeVocabularyValue(resolutionStrategy),
            signal_class: signalClass,
            result
        };

        const normalizedFieldId = sanitizeOptionalString(fieldId);
        if (normalizedFieldId) {
            data.field_id = normalizedFieldId;
        }

        const normalizedCommandId = sanitizeOptionalString(commandId);
        if (normalizedCommandId) {
            data.command_id = normalizedCommandId;
        }

        enqueueEvent(session, "ambiguity_resolved", data);
    };

    const retryRegisteredSessions = (): void => {
        for (const session of sessionsByCheckpointKey.values()) {
            if (session.taskPhase === "failed" || session.taskPhase === "ended") {
                continue;
            }

            if (session.pendingEventBodies.length > 0 || session.taskPhase === "ending") {
                clearRetryTimer(session);
                if (session.identityWriterLease) {
                    void pumpPendingEvents(session);
                } else {
                    void drainRegisteredOrphan(session);
                }
            }
        }
    };

    window.addEventListener("online", retryRegisteredSessions);
    document.addEventListener("visibilitychange", () => {
        if (document.visibilityState === "visible") {
            retryRegisteredSessions();
        }
    });

    window.formsTelemetry = {
        initializeOperationalProbe,
        getOperationalProbeIdentity,
        startSession,
        subscribeToDeliveryFailures,
        endSession,
        startTask,
        endTask,
        commitTask,
        switchForm,
        trackNavigationChanged,
        primeFieldAndGroupCompletionBaseline,
        trackFieldAndGroupStatusChanges,
        trackStatusSnapshot,
        trackFieldFocus,
        trackFieldAttempt,
        trackValidationErrorOpened,
        trackValidationErrorCleared,
        trackNavigationError,
        trackAmbiguityResolved
    };
})();
