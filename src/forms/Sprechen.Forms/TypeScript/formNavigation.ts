/**
 * Formularnavigation und Regelauswertung für HTML-Formulare.
 * Speichert Formularwerte und Abschlusszustände unter der jeweiligen Sitzungsidentität.
 * Wendet Vertragsregeln und bedingte Validierungsregeln an und meldet Änderungen an die Telemetrie.
 */

interface FormNavigationConfig {
    operationalProbe?: boolean;
    formSelector: string;
    applicationId: string;
    formId: string;
    requestedLanguage: string;
    participantId?: string;
    condition?: string;
}

/** Öffentliche Funktionen zum Initialisieren, Navigieren, Absenden und Abbrechen. */
interface FormNavigationApi {
    init(config: FormNavigationConfig): Promise<void>;
    validateAndNavigate(linkElement: HTMLAnchorElement): boolean;
    submit(): boolean;
    cancel(): void;
    getSession(): EvaluationSessionHandle | null;
}

/** Ergänzt window um die Formularnavigation für klassische Skripte. */
interface Window {
    formNavigation: FormNavigationApi;
}

/** Lokalisierte Texte nach Sprachschlüssel. */
type LocalizedText = Record<string, string>;
/** Von der Telemetrie gelieferte Sitzungsreferenz. */
type EvaluationSessionHandle = Awaited<ReturnType<typeof window.formsTelemetry.startSession>>;

/** Wertausdruck als Variablenreferenz oder Konstante. */
type ValueExpression = VarValueExpression | ConstValueExpression;

/** Referenz auf eine Vertragsvariable. */
interface VarValueExpression {
    kind: "var";
    variableId: string;
}

/** Konstanter Wert eines Regelausdrucks. */
interface ConstValueExpression {
    kind: "const";
    value: unknown;
}

/** Unterstützte Arten von Regelbedingungen. */
type Expression = AndExpression | OrExpression | NotExpression | EqualsExpression | IsSetExpression | IsNotSetExpression;

/** Bedingung, deren Teilausdrücke alle erfüllt sein müssen. */
interface AndExpression {
    kind: "and";
    expressions: Expression[];
}

/** Bedingung, bei der mindestens ein Teilausdruck erfüllt sein muss. */
interface OrExpression {
    kind: "or";
    expressions: Expression[];
}

/** Kehrt den Wahrheitswert einer Teilbedingung um. */
interface NotExpression {
    kind: "not";
    expression: Expression;
}

/** Gleichheitsprüfung. Nicht gesetzte Operanden erfüllen sie nicht. */
interface EqualsExpression {
    kind: "equals";
    left: ValueExpression;
    right: ValueExpression;
}

/** Prüft, ob eine Variable einen Wert ungleich undefined hat. */
interface IsSetExpression {
    kind: "isSet";
    variableId: string;
}

/** Prüft, ob der Wert einer Variable undefined ist. */
interface IsNotSetExpression {
    kind: "isNotSet";
    variableId: string;
}

/** Ziel einer Sichtbarkeitsaktion: Feld oder Feldgruppe. */
type VisibilityTargetRef = FieldVisibilityTargetRef | FieldGroupVisibilityTargetRef;

/** Feldkennung als Ziel einer Sichtbarkeitsänderung. */
interface FieldVisibilityTargetRef {
    kind: "field";
    fieldId: string;
}

/** Feldgruppenkennung als Ziel einer Sichtbarkeitsänderung. */
interface FieldGroupVisibilityTargetRef {
    kind: "fieldGroup";
    fieldGroupId: string;
}

/** Unterstützte Regelaktionen des Formularvertrags im Browser. */
type RuleAction =
    | SetVisibleAction
    | SetEnabledAction
    | SetReadOnlyAction
    | SetValueAction
    | ClearValueAction
    | CopyValueAction
    | NavigateToFieldGroupAction
    | FocusFieldAction
    | SpeakAction;

/** Aktion zum Setzen der Sichtbarkeit eines Feldes oder einer Gruppe. */
interface SetVisibleAction {
    kind: "setVisible";
    target: VisibilityTargetRef;
    value: boolean;
}

/** Aktion zum Setzen der Bedienbarkeit eines Feldes. */
interface SetEnabledAction {
    kind: "setEnabled";
    fieldId: string;
    value: boolean;
}

/** Aktion zum Setzen des Nur-Lesen-Zustands eines Feldes. */
interface SetReadOnlyAction {
    kind: "setReadOnly";
    fieldId: string;
    value: boolean;
}

/** Aktion zum Setzen eines Variablenwerts. */
interface SetValueAction {
    kind: "setValue";
    variableId: string;
    value: unknown;
}

/** Aktion zum Löschen eines Variablenwerts. */
interface ClearValueAction {
    kind: "clearValue";
    variableId: string;
}

/** Aktion zum Kopieren eines Werts zwischen Variablen. */
interface CopyValueAction {
    kind: "copyValue";
    fromVariableId: string;
    toVariableId: string;
}

/** Aktion zum Anzeigen einer Feldgruppe. */
interface NavigateToFieldGroupAction {
    kind: "navigateToFieldGroup";
    fieldGroupId: string;
}

/** Aktion zum Fokussieren eines Feldes. */
interface FocusFieldAction {
    kind: "focusField";
    fieldId: string;
}

/** Aktion zum Ausgeben eines Prompts über die Live-Region. */
interface SpeakAction {
    kind: "speak";
    promptId: string;
}

/** Vertragsregel mit Ausführungszeitpunkt, Priorität, Bedingung und Aktionen. */
interface RuleDefinition {
    ruleId: string;
    scope?: string | null;
    priority?: number | null;
    when: Expression;
    then: RuleAction[];
    else?: RuleAction[] | null;
}

/** Ziel einer Validierungsregel: Feld, Variable oder gesamtes Formular. */
type ConstraintTargetRef =
    | { kind: "field"; fieldId: string }
    | { kind: "variable"; variableId: string }
    | { kind: "form" };

/** Validierungsregel mit Ziel, Schweregrad, Fehlercode und Rückmeldung. */
interface ConstraintDefinition {
    constraintId: string;
    target: ConstraintTargetRef;
    severity: string;
    code: string;
    message: LocalizedText;
    remediation?: LocalizedText | null;
    kind: string;
    scope?: string | null;
    when?: Expression | null;
    pattern?: string;
    minLength?: number;
    maxLength?: number;
    values?: unknown[];
    variableIds?: string[];
}

/** Werttyp einer Vertragsvariable mit optionalen Wertebeschränkungen. */
interface VariableValueTypeDefinition {
    kind: string;
    minLength?: number | null;
    maxLength?: number | null;
    min?: number | null;
    max?: number | null;
    values?: { code: string }[] | null;
}

/** Vertragsvariable mit Ausgangswert und Validierungsregeln. */
interface VariableDefinition {
    variableId: string;
    privacyClass?: string | null;
    valueType: VariableValueTypeDefinition;
    initialValue?: unknown;
    invariantConstraints?: ConstraintDefinition[] | null;
}

/** Vertragsfeld mit Beschriftung, Variablenbindung und Validierungsregeln. */
interface FieldDefinition {
    fieldId: string;
    variableId: string;
    label?: LocalizedText | null;
    codeStyle?: boolean | null;
    editability?: string | null;
    fieldConstraints?: ConstraintDefinition[] | null;
}

/** Mitglied einer Feldgruppe: Feld oder untergeordnete Gruppe. */
type FieldGroupMember = { kind: "field"; fieldId: string } | { kind: "fieldGroup"; fieldGroupId: string };

/** Feldgruppe mit geordneten Mitgliedern. */
interface FieldGroupDefinition {
    fieldGroupId: string;
    members: FieldGroupMember[];
}

/** Lokalisierter Prompt aus der Sprachprojektion des Vertrags. */
interface PromptDefinition {
    promptId: string;
    text: LocalizedText;
}

/** Für die Formularnavigation verwendete Vertragsdaten. */
interface FormContract {
    applicationId: string;
    formId: string;
    supportedLanguages: string[];
    defaultLanguage?: string | null;
    strictLocalization: boolean;
    entryFieldGroupId: string;
    variables: VariableDefinition[];
    fields: FieldDefinition[];
    fieldGroups: FieldGroupDefinition[];
    rules: RuleDefinition[];
    constraints: ConstraintDefinition[];
    projections?: {
        voice?: {
            hints?: {
                prompts?: PromptDefinition[] | null;
            } | null;
        } | null;
    } | null;
}

((): void => {
    "use strict";

    /** Unterstützte Eingabeelemente einer Feldbindung. */
    type InputElement = HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;

    /** Aktuelle Werte nach Variablenkennung. */
    type RuntimeVariables = Record<string, RuntimeValue | undefined>;
    /** Boolesche Zustände nach Feld- oder Gruppenkennung. */
    type BoolMap = Record<string, boolean | undefined>;

    /** Ereignis, bei dem eine Formularregel ausgewertet wird. */
    type RuleScope = "onInit" | "onValueChanged" | "onEnterFieldGroup" | "onSubmitAttempt";

    /** Gespeicherte Formularwerte und Abschlusszustände einer Sitzungsidentität. */
    interface PersistedApplicationSession {
        version: 5;
        identity: EvaluationTaskIdentity;
        forms: Record<string, Record<string, RuntimeValue>>;
        completedForms?: Record<string, boolean>;
        deliveryFailureCode?: PersistedDeliveryFailureCode;
        terminalTransition?: PersistedTerminalTransition;
        completed?: PersistedCompletionTombstone;
    }

    /** Fehlercodes, die beim Wiederherstellen der Sitzung erhalten bleiben. */
    type PersistedDeliveryFailureCode = "telemetry.delivery.permanent_rejection"
        | "telemetry.delivery.storage_unavailable"
        | "telemetry.delivery.checkpoint_invalid"
        | "telemetry.delivery.checkpoint_missing";

    /** Gespeicherter, noch nicht abgeschlossener Übergang zum Aufgabenende. */
    interface PersistedTerminalTransition {
        outcome: "submitted" | "cancelled";
        taskEndOptions: TaskEndOptions;
    }

    /** Gespeicherter Abschluss mit Absenden oder Abbrechen als Ergebnis. */
    interface PersistedCompletionTombstone {
        outcome: "submitted" | "cancelled";
    }

    /** Formularkennung mit Beschriftung, vorausgesetzten Formularen und gemeinsam genutzten Variablen. */
    interface AppNavigationManifestFormInfo {
        formId: string;
        displayName?: string;
        prerequisiteForms?: string[];
        sharedVariables?: string[];
    }

    /** Navigationsmanifest einer Anwendung mit den verfügbaren Formularen. */
    interface AppNavigationManifest {
        applicationId: string;
        forms: AppNavigationManifestFormInfo[];
    }

    /** Gesammelte Ausgabe-, Navigations- und Fokusaktionen eines Regellaufs. */
    interface EffectsBuffer {
        speakPromptIds: Set<string>;
        navigateToFieldGroupId: string | null;
        focusFieldId: string | null;
    }

    /** Eingabeelemente und ursprüngliche DOM-Zustände eines Feldes. */
    interface FieldDomBinding {
        fieldId: string;
        wrapper: HTMLElement;
        inputs: InputElement[];
        labelElement: HTMLElement | null;
        baseLabelText: string | null;
        fieldGroupId: string;
        valueTypeKind: string;
        codeStyle: boolean;
        baseA11yStates: Array<{ describedBy: string | null; errorMessage: string | null; invalid: string | null }>;
        baseInputStates: Array<{ disabled: boolean; readOnly: boolean }>;
    }

    // Ein Anzeigefeld besitzt kein Eingabeelement. Sein Text spiegelt den Variablenwert.
    // Es wird nicht als Eingabe validiert. Werte werden im Variablenspeicher geführt.
    // Sichtbarkeitsregeln gelten auch für diese Anzeige.
    /** Bindung eines Anzeigefeldes ohne Eingabeelement. */
    interface ReadOnlyDisplayBinding {
        fieldId: string;
        wrapper: HTMLElement;
        fieldGroupId: string;
        valueElement: HTMLElement;
        emptyText: string;
    }

    /** Indizes für Variablen, Felder, Regeln, Gruppen und Prompts des Vertrags. */
    interface ContractIndex {
        variablesById: Record<string, VariableDefinition>;
        fieldsById: Record<string, FieldDefinition>;
        fieldGroupsById: Record<string, FieldGroupDefinition>;
        variableToFieldIds: Record<string, string[]>;
        rulesByScope: Record<string, RuleDefinition[]>;
        constraintsByFieldId: Record<string, ConstraintDefinition[]>;
        fieldGroupDescendantsById: Record<string, string[]>;
        promptsById: Record<string, PromptDefinition>;
    }

    /** Gemeinsamer Laufzeitzustand für Navigation, Regelauswertung und Telemetrie. */
    interface RuntimeContext {
        config: FormNavigationConfig;
        contract: FormContract;
        index: ContractIndex;
        form: HTMLFormElement;
        bindingsByFieldId: Record<string, FieldDomBinding>;
        readOnlyDisplaysByFieldId: Record<string, ReadOnlyDisplayBinding>;
        variables: RuntimeVariables;
        visibleByFieldId: BoolMap;
        visibleByFieldGroupId: BoolMap;
        enabledByFieldId: BoolMap;
        readOnlyByFieldId: BoolMap;
        requestedLanguage: string;
    }

    /** Offener Validierungsfehler mit den Feldern, aus denen er ermittelt wurde. */
    interface ActiveTelemetryError {
        key: string;
        errorCode: string;
        severity: "error" | "warning" | "info";
        scope: ContractScope;
        fieldId?: string;
        fieldGroupId?: string;
        relatedFieldIds?: string[];
        // Interne Herkunftsliste. Sie wird nicht als Teil des Fehlers gemeldet.
        // Ein formular- oder gruppenübergreifender Fehler kann aus mehreren Feldern stammen.
        // Über diese Liste wird geprüft, ob das gerade bearbeitete Feld beteiligt ist.
        sourceFieldIds: string[];
    }

    /** Relevanz, Pflichtstatus und Vollständigkeit eines Feldes für die Telemetrie. */
    interface FieldTelemetryStatus {
        fieldId: string;
        fieldGroupId: string;
        relevant: boolean;
        required: boolean;
        completed: boolean;
        blockingErrorCodes: string[];
    }

    /** Relevanz und Vollständigkeit einer Feldgruppe mit offenen Fehlercodes. */
    interface FieldGroupTelemetryStatus {
        fieldGroupId: string;
        relevant: boolean;
        completed: boolean;
        openGroupErrorCodes: string[];
    }

    /** Momentaufnahme von Navigation, Fortschritt, Feld- und Gruppenstatus sowie Fehlern. */
    interface TelemetrySnapshot {
        navigation: {
            currentFieldId: string;
            currentFieldGroupId: string;
            navigationIndex: number;
            navigationTotal: number;
        };
        progress: {
            allFields: { completed: number; total: number };
            requiredFieldsOnly: { completed: number; total: number };
            fieldGroupBased: { completed: number; total: number };
        };
        fieldsById: Record<string, FieldTelemetryStatus>;
        groupsById: Record<string, FieldGroupTelemetryStatus>;
        activeErrorsByKey: Record<string, ActiveTelemetryError>;
    }

    /** Ergebnis der Feldprüfung mit Regelkennung und Rückmeldung. */
    interface FieldValidationResult {
        isValid: boolean;
        errorClass?: string;
        violatedConstraintId?: string;
        violatedErrorCode?: string;
        violatedSeverity?: string;
        violatedScope?: ContractScope;
        message?: string;
        remediation?: string;
    }

    /** Validierungsmeldung mit Beschriftung und Fokusziel für die Fehlerübersicht. */
    interface ValidationIssue {
        fieldId: string;
        label: string;
        message: string;
        remediation: string | null;
        focusTarget: InputElement;
    }

    const MaxRuleIterations = 10;
    const StorageKeyPrefix = "Sprechen.Forms.session.v5:";
    const RuntimeErrorCodeRuleLoopMaxIterations = "E_RULE_LOOP_MAX_ITERS";
    // Nicht unterstützte Ausdrucksarten führen zu einem Laufzeitfehler.
    // Sie werden nicht als unerfüllte Bedingung behandelt.
    const RuntimeErrorCodeRuleUnsupportedExpressionKind = "E_RULE_UNSUPPORTED_EXPRESSION_KIND";
    const RuntimeErrorCodeLocalizationStrictMissing = "E_LOCALIZATION_STRICT_MISSING";
    const RuntimeErrorCodeLocalizationUnresolvable = "E_LOCALIZATION_UNRESOLVABLE";
    const RuntimeErrorCodeContractParseFailed = "FORM_CONTRACT_PARSE_FAILED";
    const RuntimeErrorCodeContractPatternInvalid = "FORM_CONTRACT_PATTERN_INVALID";
    // Der Schweregrad bestimmt, ob eine verletzte Regel die Eingabe sperrt.
    // Unbekannte Schweregrade werden als Vertragsfehler abgewiesen.
    const RuntimeErrorCodeContractSeverityInvalid = "FORM_CONTRACT_SEVERITY_INVALID";
    // Eine nicht lesbare Wertebeschränkung ist ein Vertragsfehler.
    const RuntimeErrorCodeContractBoundInvalid = "FORM_CONTRACT_BOUND_INVALID";
    const ConstraintScopeCrossGroup: ContractScope = "crossGroup";

    let navigationManifest: AppNavigationManifest | null = null;
    let lastSharedVariableImports: string[] = [];
    let shouldShowValidationFeedback = false;

    let currentSession: EvaluationSessionHandle | null = null;
    let persistedSession: PersistedApplicationSession | null = null;
    let runtime: RuntimeContext | null = null;
    let groupPager: FormGroupPager | null = null;
    let lastEnteredFieldGroupId: string | null = null;
    let internalNavigationInProgress = false;
    let lastTelemetrySnapshot: TelemetrySnapshot | null = null;
    let boundHandlers: Array<{ element: EventTarget; type: string; handler: EventListener }> = [];
    let unsubscribeFromTelemetryFailures: (() => void) | null = null;
    let pendingTelemetryFailure: Error | null = null;
    let initializationState: "notStarted" | "pending" | "ready" | "failed" = "notStarted";
    let requestedUiLanguage: string = formNavigationUiLanguageFallback;
    let terminalTransitionPending = false;
    let demonstrationTerminalStateDiscarded: "cancelled" | "submitted" | null = null;

    const cssEscape = (value: string): string => {
        // `CSS.escape` fehlt in manchen Zielumgebungen.
        // Ersatz für Umgebungen ohne CSS.escape: Sonderzeichen werden mit einem Backslash maskiert.
        if (typeof (CSS as unknown as { escape?: (v: string) => string }).escape === "function") {
            return (CSS as unknown as { escape: (v: string) => string }).escape(value);
        }

        return value.replace(/[^a-zA-Z0-9_-]/g, (c) => `\\${c}`);
    };

    const identityFromConfig = (config: FormNavigationConfig): EvaluationTaskIdentity => {
        if (config.operationalProbe) {
            return window.formsTelemetry.getOperationalProbeIdentity(config.applicationId, config.formId);
        }
        const hasBoundParticipantToken = studyRunIsParticipantInvited(config);
        const configuredParticipantId = config.participantId?.trim() || telemetryUnknownParticipantId;
        return {
            applicationId: config.applicationId.trim(),
            participantId: hasBoundParticipantToken
                ? configuredParticipantId
                : telemetryUnknownParticipantId,
            condition: normalizeCondition(config.condition, hasBoundParticipantToken),
            taskId: config.formId.trim() || telemetryUnknownTaskId,
            recordProvenance: hasBoundParticipantToken ? "participant" : "demonstration"
        };
    };

    const getStorageKey = (identity: EvaluationTaskIdentity): string => {
        const identityMembers = [
            identity.applicationId,
            identity.participantId,
            identity.condition,
            identity.taskId,
            identity.recordProvenance
        ];
        return `${StorageKeyPrefix}${identityMembers.map(encodeURIComponent).join(":")}`;
    };

    const isRuntimeValue = (value: unknown): value is RuntimeValue =>
        typeof value === "string"
        || (typeof value === "number" && Number.isFinite(value))
        || typeof value === "boolean"
        || (Array.isArray(value) && value.every((item) => typeof item === "string"));

    const isTaskEndOptions = (value: unknown): value is TaskEndOptions => {
        if (!value || typeof value !== "object" || Array.isArray(value)) {
            return false;
        }
        const candidate = value as Partial<TaskEndOptions>;
        const keys = Object.keys(candidate).sort();
        const expectedKeys = [
            "processSubmitted", "requiredFieldsTotal", "requiredFieldsCompleted",
            "openBlockingValidationCount", "terminationClassification",
            "mismatchCode", "correctnessBasis"
        ].sort();
        return keys.length === expectedKeys.length
            && keys.every((key, index) => key === expectedKeys[index])
            && typeof candidate.processSubmitted === "boolean"
            && Number.isInteger(candidate.requiredFieldsTotal)
            && (candidate.requiredFieldsTotal ?? -1) >= 0
            && Number.isInteger(candidate.requiredFieldsCompleted)
            && (candidate.requiredFieldsCompleted ?? -1) >= 0
            && Number.isInteger(candidate.openBlockingValidationCount)
            && (candidate.openBlockingValidationCount ?? -1) >= 0
            && (candidate.terminationClassification === "completed"
                || candidate.terminationClassification === "participant_cancelled"
                || candidate.terminationClassification === "session_abandoned"
                || candidate.terminationClassification === "technical_failure")
            && typeof candidate.mismatchCode === "string"
            && (candidate.mismatchCode === "none"
                || candidate.mismatchCode === "task_card_mismatch"
                || candidate.mismatchCode === "missing_required"
                || candidate.mismatchCode === "wrong_field_class"
                || candidate.mismatchCode === "wrong_dependency_state"
                || candidate.mismatchCode === "validation_error_open"
                || candidate.mismatchCode === "not_submitted"
                || candidate.mismatchCode === "not_assessed")
            && candidate.correctnessBasis === "log_and_system_check";
    };

    const isPersistedForms = (value: unknown): value is Record<string, Record<string, RuntimeValue>> => {
        if (!value || typeof value !== "object" || Array.isArray(value)) {
            return false;
        }
        return Object.entries(value).every(([formId, variables]) => formId.trim().length > 0
            && variables !== null
            && typeof variables === "object"
            && !Array.isArray(variables)
            && Object.entries(variables).every(([variableId, variableValue]) =>
                variableId.trim().length > 0 && isRuntimeValue(variableValue)));
    };

    const isBooleanMap = (value: unknown): value is Record<string, boolean> =>
        value !== null
        && typeof value === "object"
        && !Array.isArray(value)
        && Object.entries(value).every(([key, flag]) => key.trim().length > 0 && typeof flag === "boolean");

    const isPersistedDeliveryFailureCode = (value: unknown): value is PersistedDeliveryFailureCode =>
        value === "telemetry.delivery.permanent_rejection"
        || value === "telemetry.delivery.storage_unavailable"
        || value === "telemetry.delivery.checkpoint_invalid"
        || value === "telemetry.delivery.checkpoint_missing";

    const loadPersistedSession = (config: FormNavigationConfig): PersistedApplicationSession | null => {
        try {
            const expectedIdentity = identityFromConfig(config);
            const key = getStorageKey(expectedIdentity);
            const raw = localStorage.getItem(key);
            if (!raw) {
                return null;
            }

            const parsed = JSON.parse(raw) as Partial<PersistedApplicationSession> | null;
            const sessionKeys = parsed && typeof parsed === "object"
                ? Object.keys(parsed).sort()
                : [];
            const expectedSessionKeys = [
                "version", "identity", "forms",
                ...(parsed?.completedForms === undefined ? [] : ["completedForms"]),
                ...(parsed?.deliveryFailureCode === undefined ? [] : ["deliveryFailureCode"]),
                ...(parsed?.terminalTransition === undefined ? [] : ["terminalTransition"]),
                ...(parsed?.completed === undefined ? [] : ["completed"])
            ].sort();
            if (!parsed
                || sessionKeys.length !== expectedSessionKeys.length
                || !sessionKeys.every((keyName, index) => keyName === expectedSessionKeys[index])
                || parsed.version !== 5
                || parsed.identity === null
                || typeof parsed.identity !== "object"
                || Array.isArray(parsed.identity)
                || Object.keys(parsed.identity).sort().join("|")
                    !== [
                        "applicationId", "condition", "participantId", "recordProvenance",
                        "taskId"
                    ].sort().join("|")
                || typeof parsed.identity.applicationId !== "string"
                || typeof parsed.identity.participantId !== "string"
                || (parsed.identity.condition !== "A" && parsed.identity.condition !== "B")
                || typeof parsed.identity.taskId !== "string"
                || typeof parsed.identity.recordProvenance !== "string"
                || parsed.identity.applicationId !== expectedIdentity.applicationId
                || parsed.identity.participantId !== expectedIdentity.participantId
                || parsed.identity.condition !== expectedIdentity.condition
                || parsed.identity.taskId !== expectedIdentity.taskId
                || parsed.identity.recordProvenance !== expectedIdentity.recordProvenance
                || !isPersistedForms(parsed.forms)
                || (parsed.completedForms !== undefined && !isBooleanMap(parsed.completedForms))
                || (parsed.deliveryFailureCode !== undefined
                    && !isPersistedDeliveryFailureCode(parsed.deliveryFailureCode))
                || (parsed.terminalTransition !== undefined
                    && (!parsed.terminalTransition
                        || typeof parsed.terminalTransition !== "object"
                        || Array.isArray(parsed.terminalTransition)
                        || Object.keys(parsed.terminalTransition).sort().join("|") !== "outcome|taskEndOptions"
                        || (parsed.terminalTransition.outcome !== "submitted"
                            && parsed.terminalTransition.outcome !== "cancelled")
                        || !isTaskEndOptions(parsed.terminalTransition.taskEndOptions)))
                || (parsed.completed !== undefined
                    && (!parsed.completed
                        || typeof parsed.completed !== "object"
                        || Array.isArray(parsed.completed)
                        || Object.keys(parsed.completed).length !== 1
                        || !Object.prototype.hasOwnProperty.call(parsed.completed, "outcome")
                        || (parsed.completed.outcome !== "submitted" && parsed.completed.outcome !== "cancelled")))
                || (parsed.terminalTransition !== undefined && parsed.completed !== undefined)) {
                return null;
            }

            return {
                version: 5,
                identity: { ...parsed.identity } as EvaluationTaskIdentity,
                forms: parsed.forms,
                completedForms: { ...(parsed.completedForms ?? {}) },
                ...(parsed.deliveryFailureCode
                    ? { deliveryFailureCode: parsed.deliveryFailureCode }
                    : {}),
                ...(parsed.terminalTransition ? {
                    terminalTransition: {
                        outcome: parsed.terminalTransition.outcome,
                        taskEndOptions: { ...parsed.terminalTransition.taskEndOptions }
                    }
                } : {}),
                ...(parsed.completed ? { completed: { outcome: parsed.completed.outcome } } : {})
            };
        } catch {
            return null;
        }
    };

    const savePersistedSession = (session: PersistedApplicationSession): boolean => {
        const key = getStorageKey(session.identity);

        try {
            localStorage.setItem(key, JSON.stringify(session));
            return true;
        } catch {
            return false;
        }
    };

    const addTrackedListener = (element: EventTarget, type: string, handler: EventListener): void => {
        element.addEventListener(type, handler);
        boundHandlers.push({ element, type, handler });
    };

    const removeTrackedListeners = (): void => {
        for (const { element, type, handler } of boundHandlers) {
            element.removeEventListener(type, handler);
        }

        boundHandlers = [];
    };

    const removeTelemetryFailureSubscription = (): void => {
        unsubscribeFromTelemetryFailures?.();
        unsubscribeFromTelemetryFailures = null;
    };

    const normalizeLanguageCode = (language: string | null | undefined): string => {
        return (language ?? "").trim().replace(/_/g, "-").toLowerCase();
    };

    const tryResolveLocalizedValueForLanguage = (text: LocalizedText, language: string): string | null => {
        const normalizedLanguage = normalizeLanguageCode(language);
        if (normalizedLanguage.length === 0) {
            return null;
        }

        for (const [candidateLanguage, candidateValue] of Object.entries(text)) {
            if (normalizeLanguageCode(candidateLanguage) !== normalizedLanguage) {
                continue;
            }

            if (typeof candidateValue !== "string") {
                continue;
            }

            const normalizedValue = candidateValue.trim();
            if (normalizedValue.length > 0) {
                return normalizedValue;
            }
        }

        return null;
    };

    const resolveLocalizedTextDeterministic = (
        text: LocalizedText | null | undefined,
        contract: FormContract,
        requestedLanguage: string
    ): { value: string | null; errorCode: string | null } => {
        if (!text) {
            return { value: null, errorCode: null };
        }

        const normalizedRequestedLanguage = normalizeLanguageCode(requestedLanguage);
        const directMatch = tryResolveLocalizedValueForLanguage(text, normalizedRequestedLanguage);
        if (directMatch) {
            return { value: directMatch, errorCode: null };
        }

        const separatorIndex = normalizedRequestedLanguage.indexOf("-");
        if (separatorIndex > 0) {
            const baseLanguage = normalizedRequestedLanguage.slice(0, separatorIndex);
            const baseLanguageMatch = tryResolveLocalizedValueForLanguage(text, baseLanguage);
            if (baseLanguageMatch) {
                return { value: baseLanguageMatch, errorCode: null };
            }
        }

        if (contract.strictLocalization) {
            return { value: null, errorCode: RuntimeErrorCodeLocalizationStrictMissing };
        }

        const defaultLanguageMatch = tryResolveLocalizedValueForLanguage(text, contract.defaultLanguage ?? "");
        if (defaultLanguageMatch) {
            return { value: defaultLanguageMatch, errorCode: null };
        }

        const firstSupportedLanguage = Array.isArray(contract.supportedLanguages) && contract.supportedLanguages.length > 0
            ? contract.supportedLanguages[0]
            : "";
        const supportedLanguageMatch = tryResolveLocalizedValueForLanguage(text, firstSupportedLanguage);
        if (supportedLanguageMatch) {
            return { value: supportedLanguageMatch, errorCode: null };
        }

        return { value: null, errorCode: RuntimeErrorCodeLocalizationUnresolvable };
    };

    const resolveLocalizedText = (text: LocalizedText | null | undefined, contract: FormContract, language: string): string | null => {
        return resolveLocalizedTextDeterministic(text, contract, language).value;
    };

    const validateConstraintLocalization = (
        contract: FormContract,
        language: string,
        constraint: ConstraintDefinition | null | undefined,
        context: string
    ): string | null => {
        if (!constraint) {
            return null;
        }

        const messageResolution = resolveLocalizedTextDeterministic(constraint.message, contract, language);
        if (messageResolution.errorCode) {
            console.warn(`[formNavigation] ${messageResolution.errorCode} at ${context}.message`);
            return messageResolution.errorCode;
        }

        const remediationResolution = resolveLocalizedTextDeterministic(constraint.remediation, contract, language);
        if (remediationResolution.errorCode) {
            console.warn(`[formNavigation] ${remediationResolution.errorCode} at ${context}.remediation`);
            return remediationResolution.errorCode;
        }

        return null;
    };

    const validateContractLocalizationCoverage = (contract: FormContract, language: string): string | null => {
        for (const field of contract.fields ?? []) {
            if (!field) {
                continue;
            }

            const fieldId = (field.fieldId ?? "").trim();
            const labelResolution = resolveLocalizedTextDeterministic(field.label, contract, language);
            if (labelResolution.errorCode) {
                console.warn(`[formNavigation] ${labelResolution.errorCode} at field:${fieldId}.label`);
                return labelResolution.errorCode;
            }

            for (const constraint of field.fieldConstraints ?? []) {
                const error = validateConstraintLocalization(contract, language, constraint, `field:${fieldId}`);
                if (error) {
                    return error;
                }
            }
        }

        for (const variable of contract.variables ?? []) {
            if (!variable) {
                continue;
            }

            const variableId = (variable.variableId ?? "").trim();
            for (const constraint of variable.invariantConstraints ?? []) {
                const error = validateConstraintLocalization(contract, language, constraint, `variable:${variableId}`);
                if (error) {
                    return error;
                }
            }
        }

        for (const constraint of contract.constraints ?? []) {
            const error = validateConstraintLocalization(contract, language, constraint, "form");
            if (error) {
                return error;
            }
        }

        for (const prompt of contract.projections?.voice?.hints?.prompts ?? []) {
            if (!prompt) {
                continue;
            }

            const promptId = (prompt.promptId ?? "").trim();
            const promptResolution = resolveLocalizedTextDeterministic(prompt.text, contract, language);
            if (promptResolution.errorCode) {
                console.warn(`[formNavigation] ${promptResolution.errorCode} at prompt:${promptId}.text`);
                return promptResolution.errorCode;
            }
        }

        return null;
    };

    const createOrGetLiveRegion = (form: HTMLElement): HTMLElement => {
        const existing = form.querySelector<HTMLElement>("[data-mda-live-region]");
        if (existing) {
            return existing;
        }

        const region = document.createElement("div");
        region.setAttribute("data-mda-live-region", "true");
        region.className = "sr-only";
        region.setAttribute("role", "status");
        region.setAttribute("aria-live", "polite");
        region.setAttribute("aria-atomic", "true");
        form.prepend(region);
        return region;
    };

    const createOrGetRuntimeErrorBox = (form: HTMLElement): HTMLElement => {
        const existing = form.querySelector<HTMLElement>("[data-mda-runtime-error]");
        if (existing) {
            return existing;
        }

        const box = document.createElement("div");
        box.setAttribute("data-mda-runtime-error", "true");
        box.className = "error";
        box.setAttribute("role", "alert");
        box.setAttribute("aria-live", "assertive");
        box.setAttribute("tabindex", "-1");
        box.style.display = "none";
        form.prepend(box);
        return box;
    };

    const showRuntimeError = (message: string): void => {
        if (!runtime) {
            return;
        }

        const box = createOrGetRuntimeErrorBox(runtime.form);
        box.textContent = message;
        box.style.display = "";
        box.focus?.();
    };

    const makeFormReadOnly = (form: HTMLElement): void => {
        form.removeAttribute("aria-busy");
        form.removeAttribute("inert");
        form.setAttribute("aria-disabled", "true");
        const readOnlyInputTypes = new Set([
            "text", "search", "tel", "url", "email", "password", "date", "month", "week",
            "time", "datetime-local", "number"
        ]);
        for (const control of form.querySelectorAll<HTMLElement>(
            "input, select, textarea, button, a, [contenteditable]")) {
            if (control instanceof HTMLInputElement) {
                if (readOnlyInputTypes.has(control.type)) {
                    control.readOnly = true;
                    control.setAttribute("aria-readonly", "true");
                } else {
                    control.disabled = true;
                    control.setAttribute("aria-disabled", "true");
                }
            } else if (control instanceof HTMLTextAreaElement) {
                control.readOnly = true;
                control.setAttribute("aria-readonly", "true");
            } else if (control instanceof HTMLSelectElement || control instanceof HTMLButtonElement) {
                control.disabled = true;
                control.setAttribute("aria-disabled", "true");
            } else if (control instanceof HTMLAnchorElement) {
                control.removeAttribute("href");
                control.setAttribute("aria-disabled", "true");
                control.tabIndex = -1;
            } else if (control.hasAttribute("contenteditable")) {
                control.setAttribute("contenteditable", "false");
                control.setAttribute("aria-readonly", "true");
            }
        }
    };

    const showTelemetryError = (form: HTMLElement, textKey: UiTextKey): void => {
        makeFormReadOnly(form);
        const box = createOrGetRuntimeErrorBox(form);
        box.textContent = `${uiText("telemetryErrorTitle")} ${uiText(textKey)}`;
        box.style.display = "";
        box.focus?.();
    };

    const showTerminalTelemetryError = (form: HTMLFormElement, snapshotStored = true): void => {
        removeTrackedListeners();
        makeFormReadOnly(form);
        form.setAttribute("data-terminal-snapshot", "true");
        form.classList.add("terminal-snapshot");
        const container = form.parentElement;
        if (!container) {
            return;
        }

        container.querySelector("[data-mda-terminal-status]")?.remove();
        let box = container.querySelector<HTMLElement>("[data-mda-terminal-error]");
        if (!box) {
            box = document.createElement("div");
            box.setAttribute("data-mda-terminal-error", "true");
            box.setAttribute("role", "alert");
            box.setAttribute("aria-live", "assertive");
            box.setAttribute("aria-atomic", "true");
            box.setAttribute("tabindex", "-1");
            box.className = "error";
            container.insertBefore(box, form);
        }
        const textKey: UiTextKey = snapshotStored
            ? "telemetryTerminalErrorText"
            : "telemetryTerminalVolatileErrorText";
        box.textContent = `${uiText("telemetryErrorTitle")} ${uiText(textKey)}`;
        box.focus();
    };

    const throwRuntimeError = (code: string): never => {
        showRuntimeError(code);

        const error = new Error(code) as Error & { code: string };
        error.code = code;
        throw error;
    };

    const evalValueExpression = (expr: ValueExpression, variables: RuntimeVariables): RuntimeValue | undefined => {
        if (expr.kind === "var") {
            return variables[expr.variableId];
        }

        if (expr.kind === "const") {
            return normalizeRuntimeValue(expr.value);
        }

        return undefined;
    };

    const evalExpression = (expr: Expression, variables: RuntimeVariables): boolean => {
        switch (expr.kind) {
            case "and":
                for (const inner of expr.expressions) {
                    if (!evalExpression(inner, variables)) {
                        return false;
                    }
                }
                return true;

            case "or":
                for (const inner of expr.expressions) {
                    if (evalExpression(inner, variables)) {
                        return true;
                    }
                }
                return false;

            case "not":
                return !evalExpression(expr.expression, variables);

            case "equals": {
                const left = evalValueExpression(expr.left, variables);
                const right = evalValueExpression(expr.right, variables);
                if (left === undefined || right === undefined) {
                    // `equals(var, const)` mit unbelegtem Operanden wertet zu falsch aus.
                    return false;
                }

                return valuesEqual(left, right);
            }

            case "isSet":
                return variables[expr.variableId] !== undefined;

            case "isNotSet":
                return variables[expr.variableId] === undefined;

            default:
                return throwRuntimeError(RuntimeErrorCodeRuleUnsupportedExpressionKind);
        }
    };

    const readEmbeddedContract = (): FormContract | null => {
        const el = document.getElementById("form-contract");
        const json = el?.textContent;
        if (!json || json.trim().length === 0) {
            return null;
        }

        try {
            return JSON.parse(json) as FormContract;
        } catch {
            return null;
        }
    };

    const readEmbeddedNavigationManifest = (): AppNavigationManifest | null => {
        const el = document.getElementById("app-navigation-manifest");
        const json = el?.textContent;
        if (!json || json.trim().length === 0) {
            return null;
        }

        try {
            const parsed = JSON.parse(json) as AppNavigationManifest;
            if (!parsed || typeof parsed.applicationId !== "string") {
                return null;
            }

            if (!Array.isArray(parsed.forms)) {
                parsed.forms = [];
            }

            return parsed;
        } catch {
            return null;
        }
    };

    const buildFieldGroupDescendantsById = (fieldGroupsById: Record<string, FieldGroupDefinition>): Record<string, string[]> => {
        const childrenById: Record<string, string[]> = {};

        for (const fieldGroup of Object.values(fieldGroupsById)) {
            for (const member of fieldGroup.members) {
                if (member.kind !== "fieldGroup") {
                    continue;
                }

                const childId = member.fieldGroupId;
                if (!childrenById[fieldGroup.fieldGroupId]) {
                    childrenById[fieldGroup.fieldGroupId] = [];
                }

                childrenById[fieldGroup.fieldGroupId].push(childId);
            }
        }

        const descendantsById: Record<string, string[]> = {};
        const visiting = new Set<string>();

        const visit = (fieldGroupId: string): string[] => {
            if (descendantsById[fieldGroupId]) {
                return descendantsById[fieldGroupId];
            }

            if (visiting.has(fieldGroupId)) {
                return [fieldGroupId];
            }

            visiting.add(fieldGroupId);

            const descendants: string[] = [fieldGroupId];
            for (const childId of childrenById[fieldGroupId] ?? []) {
                descendants.push(...visit(childId));
            }

            visiting.delete(fieldGroupId);
            descendantsById[fieldGroupId] = Array.from(new Set(descendants));
            return descendantsById[fieldGroupId];
        };

        for (const fieldGroupId of Object.keys(fieldGroupsById)) {
            visit(fieldGroupId);
        }

        return descendantsById;
    };

    const buildContractIndex = (contract: FormContract): ContractIndex => {
        const variablesById: Record<string, VariableDefinition> = {};
        for (const variable of contract.variables ?? []) {
            if (variable && typeof variable.variableId === "string" && variable.variableId.trim().length > 0) {
                variablesById[variable.variableId.trim()] = variable;
            }
        }

        const fieldsById: Record<string, FieldDefinition> = {};
        const variableToFieldIds: Record<string, string[]> = {};
        for (const field of contract.fields ?? []) {
            if (!field || typeof field.fieldId !== "string") {
                continue;
            }

            const fieldId = field.fieldId.trim();
            if (fieldId.length === 0) {
                continue;
            }

            fieldsById[fieldId] = field;

            const variableId = (field.variableId ?? "").trim();
            if (variableId.length > 0) {
                if (!variableToFieldIds[variableId]) {
                    variableToFieldIds[variableId] = [];
                }

                variableToFieldIds[variableId].push(fieldId);
            }
        }

        const fieldGroupsById: Record<string, FieldGroupDefinition> = {};
        for (const fieldGroup of contract.fieldGroups ?? []) {
            if (fieldGroup && typeof fieldGroup.fieldGroupId === "string" && fieldGroup.fieldGroupId.trim().length > 0) {
                fieldGroupsById[fieldGroup.fieldGroupId.trim()] = fieldGroup;
            }
        }

        const rulesByScope: Record<string, RuleDefinition[]> = {};
        for (const rule of contract.rules ?? []) {
            if (!rule || typeof rule.ruleId !== "string" || rule.ruleId.trim().length === 0) {
                continue;
            }

            const scopeRaw = (rule.scope ?? "onValueChanged").toString().trim();
            const scope: RuleScope = (scopeRaw.length > 0 ? scopeRaw : "onValueChanged") as RuleScope;

            if (!rulesByScope[scope]) {
                rulesByScope[scope] = [];
            }

            rulesByScope[scope].push(rule);
        }

        for (const scope of Object.keys(rulesByScope)) {
            rulesByScope[scope].sort((a, b) => {
                const pa = a.priority ?? 0;
                const pb = b.priority ?? 0;
                if (pa !== pb) {
                    return pa - pb;
                }

                return compareOrdinal(a.ruleId, b.ruleId);
            });
        }

        const constraintsByFieldId: Record<string, ConstraintDefinition[]> = {};
        const addConstraint = (fieldId: string, constraint: ConstraintDefinition): void => {
            const normalizedFieldId = fieldId.trim();
            if (normalizedFieldId.length === 0) {
                return;
            }

            if (!constraintsByFieldId[normalizedFieldId]) {
                constraintsByFieldId[normalizedFieldId] = [];
            }

            constraintsByFieldId[normalizedFieldId].push(constraint);
        };

        const mapConstraintToFields = (constraint: ConstraintDefinition): void => {
            if (!constraint || !constraint.target) {
                return;
            }

            if (constraint.target.kind === "field") {
                addConstraint(constraint.target.fieldId, constraint);
                return;
            }

            if (constraint.target.kind === "variable") {
                const variableId = constraint.target.variableId;
                for (const fieldId of variableToFieldIds[variableId] ?? []) {
                    addConstraint(fieldId, constraint);
                }

                return;
            }

            if (constraint.target.kind === "form") {
                for (const fieldId of Object.keys(fieldsById)) {
                    addConstraint(fieldId, constraint);
                }
            }
        };

        for (const constraint of contract.constraints ?? []) {
            if (constraint) {
                mapConstraintToFields(constraint);
            }
        }

        for (const field of Object.values(fieldsById)) {
            for (const constraint of field.fieldConstraints ?? []) {
                if (constraint) {
                    mapConstraintToFields(constraint);
                }
            }
        }

        for (const variable of Object.values(variablesById)) {
            for (const constraint of variable.invariantConstraints ?? []) {
                if (constraint) {
                    mapConstraintToFields(constraint);
                }
            }

            // Wertebeschränkungen des Variablentyps werden ebenfalls den gebundenen Feldern zugeordnet.
            // Dadurch stehen sie der Validierung und der Telemetrie über denselben Index zur Verfügung.
            for (const constraint of valueTypeStatedRules(variable.variableId, variable.valueType)) {
                mapConstraintToFields(constraint);
            }
        }

        for (const fieldId of Object.keys(constraintsByFieldId)) {
            constraintsByFieldId[fieldId].sort((a, b) => compareOrdinal(a.constraintId, b.constraintId));
        }

        const promptsById: Record<string, PromptDefinition> = {};
        for (const prompt of contract.projections?.voice?.hints?.prompts ?? []) {
            if (prompt && typeof prompt.promptId === "string" && prompt.promptId.trim().length > 0) {
                promptsById[prompt.promptId.trim()] = prompt;
            }
        }

        const fieldGroupDescendantsById = buildFieldGroupDescendantsById(fieldGroupsById);

        return {
            variablesById,
            fieldsById,
            fieldGroupsById,
            variableToFieldIds,
            rulesByScope,
            constraintsByFieldId,
            fieldGroupDescendantsById,
            promptsById
        };
    };

    const buildFieldBindingsById = (form: HTMLFormElement): Record<string, FieldDomBinding> => {
        const wrappers = Array.from(form.querySelectorAll<HTMLElement>("[data-field-id]"));
        const bindings: Record<string, FieldDomBinding> = {};

        for (const wrapper of wrappers) {
            const fieldIdRaw = wrapper.dataset.fieldId;
            if (!fieldIdRaw || fieldIdRaw.trim().length === 0) {
                continue;
            }

            const fieldId = fieldIdRaw.trim();
            const inputs = Array.from(wrapper.querySelectorAll<InputElement>("input, select, textarea"));
            if (inputs.length === 0) {
                continue;
            }

            const fieldGroupId = (wrapper.dataset.fieldGroupId ?? "").trim();
            const valueTypeKind = (wrapper.dataset.valueTypeKind ?? "").trim();
            const codeStyle = (wrapper.dataset.codeStyle ?? "").trim().toLowerCase() === "true";

            const labelElement =
                wrapper instanceof HTMLFieldSetElement
                    ? wrapper.querySelector<HTMLElement>("legend")
                    : wrapper.querySelector<HTMLElement>("label");

            const baseLabelText = labelElement?.textContent?.trim() ?? null;

            bindings[fieldId] = {
                fieldId,
                wrapper,
                inputs,
                labelElement,
                baseLabelText,
                fieldGroupId,
                valueTypeKind,
                codeStyle,
                baseA11yStates: inputs.map((input) => ({
                    describedBy: input.getAttribute("aria-describedby"),
                    errorMessage: input.getAttribute("aria-errormessage"),
                    invalid: input.getAttribute("aria-invalid")
                })),
                baseInputStates: inputs.map((input) => ({
                    disabled: input.disabled,
                    readOnly: input instanceof HTMLInputElement || input instanceof HTMLTextAreaElement ? input.readOnly : false
                }))
            };
        }

        return bindings;
    };

    const buildReadOnlyDisplayBindingsById = (form: HTMLFormElement): Record<string, ReadOnlyDisplayBinding> => {
        const wrappers = Array.from(form.querySelectorAll<HTMLElement>("[data-field-id][data-readonly-display]"));
        const displays: Record<string, ReadOnlyDisplayBinding> = {};

        for (const wrapper of wrappers) {
            const fieldId = (wrapper.dataset.fieldId ?? "").trim();
            if (fieldId.length === 0) {
                continue;
            }

            const valueElement = wrapper.querySelector<HTMLElement>("[data-readonly-display-value]");
            if (!valueElement) {
                continue;
            }

            displays[fieldId] = {
                fieldId,
                wrapper,
                fieldGroupId: (wrapper.dataset.fieldGroupId ?? "").trim(),
                valueElement,
                emptyText: valueElement.dataset.emptyText ?? ""
            };
        }

        return displays;
    };

    const formatReadOnlyDisplayValue = (value: RuntimeValue | undefined, emptyText: string): string => {
        if (value === undefined) {
            return emptyText;
        }

        if (Array.isArray(value)) {
            const joined = value.map((entry) => String(entry).trim()).filter((entry) => entry.length > 0).join(", ");
            return joined.length > 0 ? joined : emptyText;
        }

        const text = String(value).trim();
        return text.length > 0 ? text : emptyText;
    };

    const getFieldIdFromInput = (element: InputElement): string | null => {
        const name = (element.name ?? "").trim();
        if (name.length > 0) {
            return name;
        }

        const id = (element.id ?? "").trim();
        return id.length > 0 ? id : null;
    };

    const tryGetFieldAndVariable = (fieldId: string): { field: FieldDefinition; variable: VariableDefinition } | null => {
        if (!runtime) {
            return null;
        }

        const field = runtime.index.fieldsById[fieldId];
        if (!field) {
            return null;
        }

        const variableId = (field.variableId ?? "").trim();
        if (variableId.length === 0) {
            return null;
        }

        const variable = runtime.index.variablesById[variableId];
        if (!variable) {
            return null;
        }

        return { field, variable };
    };

    // Den Werttyp zuerst aus der gebundenen Vertragsvariable lesen.
    // Fehlt diese Zuordnung, den im DOM hinterlegten Werttyp verwenden.
    const valueTypeKindForField = (fieldId: string): string =>
        tryGetFieldAndVariable(fieldId)?.variable.valueType.kind
            ?? runtime?.bindingsByFieldId[fieldId]?.valueTypeKind
            ?? "";

    const readVariableValueFromField = (fieldId: string): RuntimeValue | undefined => {
        if (!runtime) {
            return undefined;
        }

        const binding = runtime.bindingsByFieldId[fieldId];
        if (!binding) {
            return undefined;
        }

        const resolved = tryGetFieldAndVariable(fieldId);
        const valueTypeKind = resolved?.variable.valueType.kind ?? "";

        if (valueTypeKind === "set" || binding.valueTypeKind === "set") {
            const selected = binding.inputs
                .filter((input) => input instanceof HTMLInputElement && input.type === "checkbox" && input.checked)
                .map((input) => (input as HTMLInputElement).value)
                .filter((v) => v.trim().length > 0);

            return selected.length > 0 ? selected : undefined;
        }

        const first = binding.inputs[0];
        const rawValue = (first instanceof HTMLInputElement || first instanceof HTMLSelectElement || first instanceof HTMLTextAreaElement
            ? first.value
            : "")
            .trim();

        if (rawValue.length === 0) {
            return undefined;
        }

        if (valueTypeKind === "boolean") {
            if (rawValue === "true") {
                return true;
            }

            if (rawValue === "false") {
                return false;
            }

            return undefined;
        }

        if (valueTypeKind === "integer") {
            // Ungültige Ganzzahleingaben als ursprünglichen Text erhalten.
            // So bleiben sie von fehlenden Werten unterscheidbar und werden nicht durch Abschneiden von Nachkommastellen gültig.
            return readWholeNumber(rawValue) ?? rawValue;
        }

        return rawValue;
    };

    const applyVariableValueToField = (fieldId: string, value: RuntimeValue | undefined): void => {
        if (!runtime) {
            return;
        }

        const display = runtime.readOnlyDisplaysByFieldId[fieldId];
        if (display) {
            display.valueElement.textContent = formatReadOnlyDisplayValue(value, display.emptyText);
            return;
        }

        const binding = runtime.bindingsByFieldId[fieldId];
        if (!binding) {
            return;
        }

        // Vor programmierten Wertänderungen eigene Gültigkeitsfehler löschen, damit keine veraltete Meldung bleibt.
        for (const input of binding.inputs) {
            if (input instanceof HTMLInputElement || input instanceof HTMLSelectElement || input instanceof HTMLTextAreaElement) {
                input.setCustomValidity("");
            }
        }

        const resolved = tryGetFieldAndVariable(fieldId);
        const valueTypeKind = resolved?.variable.valueType.kind ?? "";

        if (valueTypeKind === "set" || binding.valueTypeKind === "set") {
            const selected = Array.isArray(value) ? value : [];
            for (const input of binding.inputs) {
                if (input instanceof HTMLInputElement && input.type === "checkbox") {
                    input.checked = selected.includes(input.value);
                }
            }

            return;
        }

        const first = binding.inputs[0];
        if (value === undefined) {
            first.value = "";
            return;
        }

        if (valueTypeKind === "boolean") {
            first.value = value === true ? "true" : "false";
            return;
        }

        first.value = Array.isArray(value) ? "" : String(value);
    };

    const setVariableValue = (variableId: string, value: RuntimeValue | undefined): boolean => {
        if (!runtime) {
            return false;
        }

        const current = runtime.variables[variableId];
        if (current === undefined && value === undefined) {
            return false;
        }

        if (current !== undefined && value !== undefined && valuesEqual(current, value)) {
            return false;
        }

        if (value === undefined) {
            delete runtime.variables[variableId];
        } else {
            runtime.variables[variableId] = value;
        }

        for (const fieldId of runtime.index.variableToFieldIds[variableId] ?? []) {
            applyVariableValueToField(fieldId, value);
        }

        return true;
    };

    const persistCurrentFormValues = (): boolean => {
        if (!runtime || !persistedSession) {
            return false;
        }

        const formValues: Record<string, RuntimeValue> = {};

        for (const variable of runtime.contract.variables ?? []) {
            const variableId = (variable.variableId ?? "").trim();
            if (variableId.length === 0) {
                continue;
            }

            const value = runtime.variables[variableId];
            if (value !== undefined) {
                formValues[variableId] = value;
            }
        }

        persistedSession.forms[runtime.config.formId] = formValues;
        return savePersistedSession(persistedSession);
    };

    const captureCurrentDomValues = (): void => {
        if (!runtime) {
            return;
        }

        const activeElement = runtime.form.ownerDocument.activeElement;
        const activeFieldId = activeElement instanceof HTMLInputElement
            || activeElement instanceof HTMLSelectElement
            || activeElement instanceof HTMLTextAreaElement
            ? getFieldIdFromInput(activeElement)
            : null;
        const orderedFieldIds = [
            ...(activeFieldId ? [activeFieldId] : []),
            ...Object.keys(runtime.bindingsByFieldId).filter((fieldId) => fieldId !== activeFieldId)
        ];
        const capturedVariableIds = new Set<string>();

        for (const fieldId of orderedFieldIds) {
            const resolved = tryGetFieldAndVariable(fieldId);
            const variableId = resolved?.variable.variableId.trim() ?? "";
            if (variableId.length === 0 || capturedVariableIds.has(variableId)) {
                continue;
            }

            capturedVariableIds.add(variableId);
            const value = readVariableValueFromField(fieldId);
            if (value === undefined) {
                delete runtime.variables[variableId];
            } else {
                runtime.variables[variableId] = value;
            }
        }
    };

    const cloneVariables = (variables: RuntimeVariables): RuntimeVariables => {
        const cloned: RuntimeVariables = {};

        for (const [key, value] of Object.entries(variables)) {
            if (Array.isArray(value)) {
                cloned[key] = [...value];
            } else {
                cloned[key] = value;
            }
        }

        return cloned;
    };

    const cloneBoolMap = (map: BoolMap): BoolMap => ({ ...map });

    const syncDomFromVariables = (): void => {
        if (!runtime) {
            return;
        }

        for (const field of Object.values(runtime.index.fieldsById)) {
            const variableId = (field.variableId ?? "").trim();
            if (variableId.length === 0) {
                continue;
            }

            applyVariableValueToField(field.fieldId, runtime.variables[variableId]);
        }
    };

    const speakPrompts = (promptIds: Set<string>): void => {
        if (!runtime || promptIds.size === 0) {
            return;
        }

        const ordered = Array.from(promptIds).sort(compareOrdinal);
        const texts: string[] = [];

        for (const promptId of ordered) {
            const prompt = runtime.index.promptsById[promptId];
            const text = resolveLocalizedText(prompt?.text, runtime.contract, runtime.requestedLanguage);
            if (text) {
                texts.push(text);
            }
        }

        if (texts.length === 0) {
            return;
        }

        const region = createOrGetLiveRegion(runtime.form);
        region.textContent = texts.join(" ");
    };

    const scrollToFieldGroup = (fieldGroupId: string): void => {
        if (!runtime) {
            return;
        }

        const selector = `[data-field-group-id="${cssEscape(fieldGroupId)}"]`;
        const target = runtime.form.querySelector<HTMLElement>(selector) ?? document.querySelector<HTMLElement>(selector);
        target?.scrollIntoView({ behavior: "smooth", block: "start" });
    };

    const focusField = (fieldId: string): void => {
        if (!runtime) {
            return;
        }

        const binding = runtime.bindingsByFieldId[fieldId];
        if (!binding) {
            return;
        }

        const firstInput = binding.inputs[0];
        firstInput?.focus();
    };

    const applyRuleAction = (action: RuleAction, effects: EffectsBuffer): boolean => {
        if (!runtime) {
            return false;
        }

        switch (action.kind) {
            case "setVisible": {
                if (action.target.kind === "field") {
                    const current = runtime.visibleByFieldId[action.target.fieldId];
                    if (current === action.value) {
                        return false;
                    }

                    runtime.visibleByFieldId[action.target.fieldId] = action.value;
                    return true;
                }

                if (action.target.kind === "fieldGroup") {
                    const current = runtime.visibleByFieldGroupId[action.target.fieldGroupId];
                    if (current === action.value) {
                        return false;
                    }

                    runtime.visibleByFieldGroupId[action.target.fieldGroupId] = action.value;
                    return true;
                }

                return false;
            }

            case "setEnabled": {
                const current = runtime.enabledByFieldId[action.fieldId];
                if (current === action.value) {
                    return false;
                }

                runtime.enabledByFieldId[action.fieldId] = action.value;
                return true;
            }

            case "setReadOnly": {
                const current = runtime.readOnlyByFieldId[action.fieldId];
                if (current === action.value) {
                    return false;
                }

                runtime.readOnlyByFieldId[action.fieldId] = action.value;
                return true;
            }

            case "setValue":
                return setVariableValue(action.variableId, normalizeRuntimeValue(action.value));

            case "clearValue":
                return setVariableValue(action.variableId, undefined);

            case "copyValue": {
                const from = runtime.variables[action.fromVariableId];
                return setVariableValue(action.toVariableId, from);
            }

            case "navigateToFieldGroup":
                effects.navigateToFieldGroupId = action.fieldGroupId;
                return false;

            case "focusField":
                effects.focusFieldId = action.fieldId;
                return false;

            case "speak":
                effects.speakPromptIds.add(action.promptId);
                return false;
        }
    };

    const handleRules = (scope: RuleScope): boolean => {
        if (!runtime) {
            return true;
        }

        const rules = runtime.index.rulesByScope[scope] ?? [];
        if (rules.length === 0) {
            return true;
        }

        const snapshot = {
            variables: cloneVariables(runtime.variables),
            visibleByFieldId: cloneBoolMap(runtime.visibleByFieldId),
            visibleByFieldGroupId: cloneBoolMap(runtime.visibleByFieldGroupId),
            enabledByFieldId: cloneBoolMap(runtime.enabledByFieldId),
            readOnlyByFieldId: cloneBoolMap(runtime.readOnlyByFieldId)
        };

        const effects: EffectsBuffer = {
            speakPromptIds: new Set<string>(),
            navigateToFieldGroupId: null,
            focusFieldId: null
        };

        for (let iter = 0; iter < MaxRuleIterations; iter++) {
            let changed = false;

            for (const rule of rules) {
                const cond = evalExpression(rule.when, runtime.variables);
                const actions = cond ? rule.then : rule.else ?? [];

                for (const action of actions) {
                    changed = applyRuleAction(action, effects) || changed;
                }
            }

            if (!changed) {
                // Nebenwirkungen laufen einmal je `handleRules`-Lauf.
                speakPrompts(effects.speakPromptIds);

                if (effects.navigateToFieldGroupId) {
                    groupPager?.revealGroup(effects.navigateToFieldGroupId);
                    scrollToFieldGroup(effects.navigateToFieldGroupId);
                }

                if (effects.focusFieldId) {
                    focusField(effects.focusFieldId);
                }

                return true;
            }
        }

        // Nach Erreichen der Schleifengrenze den vorherigen Zustand wiederherstellen und
        // kontrolliert fehlschlagen.
        runtime.variables = snapshot.variables;
        runtime.visibleByFieldId = snapshot.visibleByFieldId;
        runtime.visibleByFieldGroupId = snapshot.visibleByFieldGroupId;
        runtime.enabledByFieldId = snapshot.enabledByFieldId;
        runtime.readOnlyByFieldId = snapshot.readOnlyByFieldId;

        syncDomFromVariables();
        refreshDomFromState();
        showRuntimeError(RuntimeErrorCodeRuleLoopMaxIterations);

        if (currentSession) {
            try {
                window.formsTelemetry.trackNavigationError(currentSession, ConstraintScopeCrossGroup);
            } catch (error) {
                // Ein Telemetriefehler darf die Fehlerbehandlung nicht unterbrechen.
                // Die Warnung macht sichtbar, dass der Navigationsfehler nicht gemeldet werden konnte.
                if (typeof console !== "undefined" && typeof console.warn === "function") {
                    console.warn("formNavigation: navigation_error dropped at the rule-loop guard", error);
                }
            }
        }

        return false;
    };

    const setOrRemoveAttribute = (element: Element, name: string, value: string | null): void => {
        if (value === null || value === undefined || value.toString().trim().length === 0) {
            element.removeAttribute(name);
            return;
        }

        element.setAttribute(name, value.toString());
    };

    const setBooleanAttribute = (element: Element, name: string, enabled: boolean): void => {
        if (enabled) {
            element.setAttribute(name, "");
        } else {
            element.removeAttribute(name);
        }
    };

    const normalizeSeverity = (severity: string | null | undefined): string => {
        const s = (severity ?? "").trim().toLowerCase();
        if (s === "warning" || s === "info" || s === "error") {
            return s;
        }

        return throwRuntimeError(RuntimeErrorCodeContractSeverityInvalid);
    };

    const normalizeErrorSeverity = (severity: string | null | undefined): "error" | "warning" | "info" => {
        const normalized = normalizeSeverity(severity);
        if (normalized === "warning" || normalized === "info") {
            return normalized;
        }

        return "error";
    };

    const blocksFieldGroupCompletionForError = (
        error: ActiveTelemetryError,
        fieldGroupId: string
    ): boolean => {
        if (error.scope === "form") {
            return true;
        }

        return error.scope === "fieldGroup" && error.fieldGroupId === fieldGroupId;
    };

    const isBlockingConstraint = (constraint: ConstraintDefinition): boolean => normalizeSeverity(constraint.severity) === "error";

    const stripRequiredMarker = (label: string): string => label.replace(/\s*\*+\s*$/, "").trimEnd();

    const updateRequiredIndicator = (binding: FieldDomBinding, required: boolean): void => {
        if (!binding.labelElement || binding.baseLabelText === null) {
            return;
        }

        const base = stripRequiredMarker(binding.baseLabelText);
        binding.labelElement.textContent = required ? `${base} *` : base;
    };

    // Sprachtypen, Auswahlreihenfolge und Oberflächentexte werden aus dem serverseitigen Katalog erzeugt.
    // Die Datei Generated/formNavigationTextMap.generated.ts wird vom Codegen erzeugt.
    const getUiLanguage = (): UiLanguage => {
        const language = (runtime?.requestedLanguage ?? requestedUiLanguage).trim().toLowerCase();
        for (const isoCode of formNavigationUiLanguagePrefixOrder) {
            if (language.startsWith(isoCode)) {
                return isoCode;
            }
        }

        return formNavigationUiLanguageFallback;
    };

    const uiText = (key: UiTextKey): string => {
        const language = getUiLanguage();
        return formNavigationUiTextMap[language][key];
    };

    /** Art der Ersatzmeldung, falls die Validierungsregel keinen Meldungstext liefert. */
    type ValidationFallbackKind = "required" | "invalidValue" | "invalidFormat";

    const getValidationFallbackMessage = (kind: ValidationFallbackKind): string => {
        switch (kind) {
            case "required":
                return uiText("validationRequiredFallback");
            case "invalidFormat":
                return uiText("validationInvalidFormatFallback");
            default:
                return uiText("validationInvalidValueFallback");
        }
    };

    const resolveValidationFeedback = (
        constraint: ConstraintDefinition | null | undefined,
        fallbackKind: ValidationFallbackKind
    ): { message: string; remediation?: string } => {
        const message = runtime
            ? resolveLocalizedText(constraint?.message, runtime.contract, runtime.requestedLanguage)
            : null;
        const remediation = runtime
            ? resolveLocalizedText(constraint?.remediation, runtime.contract, runtime.requestedLanguage)
            : null;

        return {
            message: message ?? getValidationFallbackMessage(fallbackKind),
            remediation: remediation ?? undefined
        };
    };

    const getValidationFallbackKind = (validity: ValidityState): ValidationFallbackKind => {
        if (validity.valueMissing) {
            return "required";
        }

        if (validity.patternMismatch) {
            return "invalidFormat";
        }

        return "invalidValue";
    };

    const getValidationSummaryBox = (): HTMLElement | null => document.getElementById("validation-summary");
    const getRuntimeStatusBox = (): HTMLElement | null => document.getElementById("runtime-status");

    const clearValidationSummary = (): void => {
        const box = getValidationSummaryBox();
        if (!box) {
            return;
        }

        box.replaceChildren();
        box.style.display = "none";
    };

    const setRuntimeStatus = (text: string | null): void => {
        const box = getRuntimeStatusBox();
        if (box) {
            box.textContent = (text ?? "").trim();
        }
    };

    const getOrCreateFieldErrorElement = (binding: FieldDomBinding): HTMLElement => {
        const existing = binding.wrapper.querySelector<HTMLElement>(".field-error");
        if (existing) {
            return existing;
        }

        const el = document.createElement("div");
        el.className = "field-error";
        el.hidden = true;

        const baseId = (binding.inputs[0]?.id ?? binding.fieldId).trim();
        if (baseId.length > 0) {
            el.id = `${baseId}-error`;
        }

        binding.wrapper.appendChild(el);
        return el;
    };

    const setFieldValidationFeedback = (binding: FieldDomBinding, result: FieldValidationResult): void => {
        if (result.isValid) {
            binding.wrapper.classList.remove("field-has-error");
            const errorElement = binding.wrapper.querySelector<HTMLElement>(".field-error");
            if (errorElement) {
                errorElement.textContent = "";
                errorElement.hidden = true;
            }

            for (let i = 0; i < binding.inputs.length; i++) {
                const input = binding.inputs[i];
                const base = binding.baseA11yStates[i];

                if (base.invalid === null) {
                    input.removeAttribute("aria-invalid");
                } else {
                    input.setAttribute("aria-invalid", base.invalid);
                }

                if (base.errorMessage === null) {
                    input.removeAttribute("aria-errormessage");
                } else {
                    input.setAttribute("aria-errormessage", base.errorMessage);
                }

                if (base.describedBy === null) {
                    input.removeAttribute("aria-describedby");
                } else {
                    input.setAttribute("aria-describedby", base.describedBy);
                }
            }

            return;
        }

        const message = (result.message ?? "").trim();
        const remediation = (result.remediation ?? "").trim();
        const combined = remediation.length > 0 ? `${message} ${remediation}`.trim() : message;

        const errorElement = getOrCreateFieldErrorElement(binding);
        errorElement.textContent = combined.length > 0 ? combined : message;
        errorElement.hidden = false;

        binding.wrapper.classList.add("field-has-error");

        const errorId = errorElement.id.trim();
        for (let i = 0; i < binding.inputs.length; i++) {
            const input = binding.inputs[i];
            const base = binding.baseA11yStates[i];

            input.setAttribute("aria-invalid", "true");

            if (errorId.length > 0) {
                input.setAttribute("aria-errormessage", errorId);

                const ids = new Set<string>();
                for (const id of (base.describedBy ?? "").split(/\s+/)) {
                    if (id.trim().length > 0) {
                        ids.add(id.trim());
                    }
                }
                ids.add(errorId);

                input.setAttribute("aria-describedby", Array.from(ids).join(" "));
            }
        }
    };

    const renderValidationSummary = (issues: ValidationIssue[], focus: boolean): void => {
        const box = getValidationSummaryBox();
        if (!box) {
            return;
        }

        if (issues.length === 0) {
            clearValidationSummary();
            return;
        }

        box.replaceChildren();

        const title = document.createElement("strong");
        title.textContent = uiText("validationSummaryTitle");
        box.appendChild(title);

        const list = document.createElement("ul");
        for (const issue of issues) {
            const li = document.createElement("li");

            const anchor = document.createElement("a");
            const focusId = (issue.focusTarget.id ?? "").trim();
            if (focusId.length > 0) {
                anchor.href = `#${encodeURIComponent(focusId)}`;
            } else {
                anchor.href = "#";
            }

            const combined = issue.remediation ? `${issue.message} ${issue.remediation}`.trim() : issue.message;
            anchor.textContent = `${issue.label}: ${combined}`;

            anchor.addEventListener("click", (event) => {
                event.preventDefault();
                // Die Gruppenseite vor dem Fokussieren öffnen, damit das Fehlerziel erreichbar ist.
                const groupId = issue.focusTarget.closest<HTMLElement>("[data-field-group-id]")?.dataset.fieldGroupId;
                if (groupId) {
                    groupPager?.revealGroup(groupId);
                }

                issue.focusTarget.focus();
                issue.focusTarget.scrollIntoView({ block: "center" });
            });

            li.appendChild(anchor);
            list.appendChild(li);
        }
        box.appendChild(list);

        box.style.display = "";

        if (focus) {
            box.focus();
        }
    };

    const getHiddenFieldGroupIds = (): Set<string> => {
        const hidden = new Set<string>();
        if (!runtime) {
            return hidden;
        }

        for (const [fieldGroupId, isVisible] of Object.entries(runtime.visibleByFieldGroupId)) {
            if (isVisible === false) {
                for (const desc of runtime.index.fieldGroupDescendantsById[fieldGroupId] ?? [fieldGroupId]) {
                    hidden.add(desc);
                }
            }
        }

        return hidden;
    };

    const isFieldHiddenByRules = (binding: FieldDomBinding): boolean => {
        if (!runtime) {
            return false;
        }

        const fieldVisible = runtime.visibleByFieldId[binding.fieldId] ?? true;
        return !fieldVisible || getHiddenFieldGroupIds().has(binding.fieldGroupId);
    };

    const renderVisibilityAndInteractivity = (): void => {
        if (!runtime) {
            return;
        }

        const hiddenFieldGroups = getHiddenFieldGroupIds();

        const fieldGroupEls = Array.from(runtime.form.querySelectorAll<HTMLElement>("[data-field-group-id]:not([data-field-id])"));
        for (const el of fieldGroupEls) {
            const fieldGroupId = (el.dataset.fieldGroupId ?? "").trim();
            el.style.display = hiddenFieldGroups.has(fieldGroupId) ? "none" : "";
        }

        for (const binding of Object.values(runtime.bindingsByFieldId)) {
            const fieldVisible = runtime.visibleByFieldId[binding.fieldId] ?? true;
            const sectionVisible = !hiddenFieldGroups.has(binding.fieldGroupId);
            const isVisible = fieldVisible && sectionVisible;

            binding.wrapper.style.display = isVisible ? "" : "none";

            if (!isVisible) {
                for (const input of binding.inputs) {
                    input.disabled = true;
                    if (input instanceof HTMLInputElement || input instanceof HTMLTextAreaElement || input instanceof HTMLSelectElement) {
                        input.setCustomValidity("");
                    }
                }

                setFieldValidationFeedback(binding, { isValid: true });
                continue;
            }

            const desiredEnabled = runtime.enabledByFieldId[binding.fieldId] ?? true;
            const desiredReadOnly = runtime.readOnlyByFieldId[binding.fieldId] ?? false;

            for (let i = 0; i < binding.inputs.length; i++) {
                const input = binding.inputs[i];
                const base = binding.baseInputStates[i];

                const effectiveReadOnly = base.readOnly || desiredReadOnly;
                const effectiveDisabled = base.disabled || !desiredEnabled;

                if (input instanceof HTMLSelectElement) {
                    // `HTMLSelectElement` kennt kein readonly, und `readOnly` wird auf disabled abgebildet.
                    input.disabled = effectiveDisabled || effectiveReadOnly;
                } else {
                    input.disabled = effectiveDisabled;
                    if (input instanceof HTMLInputElement || input instanceof HTMLTextAreaElement) {
                        input.readOnly = effectiveReadOnly;
                    }
                }
            }
        }

        // Anzeigefelder folgen den Sichtbarkeitsregeln für Felder und Gruppen.
        // Bedienbarkeit und Nur-Lesen-Zustand ändern diese Elemente ohne Eingabefunktion nicht.
        for (const display of Object.values(runtime.readOnlyDisplaysByFieldId)) {
            const fieldVisible = runtime.visibleByFieldId[display.fieldId] ?? true;
            const sectionVisible = !hiddenFieldGroups.has(display.fieldGroupId);
            display.wrapper.style.display = fieldVisible && sectionVisible ? "" : "none";
        }

        groupPager?.syncVisibility();
    };

    const getActiveConstraintsForField = (fieldId: string): ConstraintDefinition[] => {
        if (!runtime) {
            return [];
        }

        const constraints = runtime.index.constraintsByFieldId[fieldId] ?? [];
        const active: ConstraintDefinition[] = [];

        for (const constraint of constraints) {
            if (!constraint) {
                continue;
            }

            if (!constraint.when) {
                active.push(constraint);
                continue;
            }

            if (evalExpression(constraint.when, runtime.variables)) {
                active.push(constraint);
            }
        }

        return active;
    };

    /** Aus Vertrag und Werttyp abgeleitete Validierungsregeln mit ihrer jeweiligen Herkunft. */
    interface EffectiveFieldConstraints {
        required: boolean;
        requiredConstraint: ConstraintDefinition | null;
        pattern: string | null;
        patternConstraint: ConstraintDefinition | null;
        minLength: number | null;
        minLengthConstraint: ConstraintDefinition | null;
        maxLength: number | null;
        maxLengthConstraint: ConstraintDefinition | null;
        min: string | null;
        minConstraint: ConstraintDefinition | null;
        max: string | null;
        maxConstraint: ConstraintDefinition | null;
        oneOfValues: RuntimeValue[] | null;
        oneOfConstraint: ConstraintDefinition | null;
    }

    // Validierungsregeln und ihre Herkunft aus Vertrag und Werttyp ermitteln.
    // Bereits gesetzte DOM-Attribute werden dabei nicht als Regelquelle verwendet.
    const computeEffectiveConstraintsForField = (fieldId: string): EffectiveFieldConstraints => {
        if (!runtime) {
            return {
                required: false,
                requiredConstraint: null,
                pattern: null,
                patternConstraint: null,
                minLength: null,
                minLengthConstraint: null,
                maxLength: null,
                maxLengthConstraint: null,
                min: null,
                minConstraint: null,
                max: null,
                maxConstraint: null,
                oneOfValues: null,
                oneOfConstraint: null
            };
        }

        const statedRules = getActiveConstraintsForField(fieldId);
        const carried = surfaceCarriedRules(
            statedRules,
            () => throwRuntimeError(RuntimeErrorCodeContractSeverityInvalid),
            () => throwRuntimeError(RuntimeErrorCodeContractBoundInvalid));

        // oneOf wird hier separat erfasst, weil es kein entsprechendes HTML-Validierungsattribut gibt.
        let oneOfValues: RuntimeValue[] | null = null;
        let oneOfConstraint: ConstraintDefinition | null = null;
        for (const constraint of statedRules) {
            if (!isBlockingConstraint(constraint) || oneOfConstraint !== null) {
                continue;
            }

            if (statesRuleKind(constraint, "oneOf") && Array.isArray(constraint.values)) {
                const normalized = constraint.values.map(normalizeRuntimeValue).filter((v): v is RuntimeValue => v !== undefined);
                oneOfValues = normalized.length > 0 ? normalized : null;
                oneOfConstraint = constraint;
            }
        }

        return {
            required: carried.required,
            requiredConstraint: carried.requiredConstraint,
            pattern: carried.pattern,
            patternConstraint: carried.patternConstraint,
            minLength: carried.minLength,
            minLengthConstraint: carried.minLengthConstraint,
            maxLength: carried.maxLength,
            maxLengthConstraint: carried.maxLengthConstraint,
            min: carried.min,
            minConstraint: carried.minConstraint,
            max: carried.max,
            maxConstraint: carried.maxConstraint,
            oneOfValues,
            oneOfConstraint
        };
    };

    const BoundedInputTypes = new Set(["number", "range", "date", "month", "week", "time", "datetime-local"]);

    const applyConstraintsToField = (fieldId: string): void => {
        if (!runtime) {
            return;
        }

        const binding = runtime.bindingsByFieldId[fieldId];
        if (!binding) {
            return;
        }

        const effective = computeEffectiveConstraintsForField(fieldId);

        // Die Pflichtmarkierung aus den aktuell geltenden Regeln neu setzen.
        // Entfällt die Pflichtregel, wird auch eine ursprünglich gerenderte Markierung entfernt.
        updateRequiredIndicator(binding, effective.required);

        binding.wrapper.dataset.required = effective.required ? "true" : "false";

        const isMultiSelect = valueTypeKindForField(fieldId) === "set";

        for (const input of binding.inputs) {
            // Mehrfachauswahl-Pflicht lässt sich nicht mit nativem required an jeder Checkbox modellieren.
            if (isMultiSelect && input instanceof HTMLInputElement && input.type === "checkbox") {
                setBooleanAttribute(input, "required", false);
                continue;
            }

            if (input instanceof HTMLInputElement || input instanceof HTMLTextAreaElement || input instanceof HTMLSelectElement) {
                setBooleanAttribute(input, "required", effective.required);
                setOrRemoveAttribute(input, "aria-required", effective.required ? "true" : null);
            }

            if (input instanceof HTMLInputElement) {
                setOrRemoveAttribute(input, "pattern", effective.pattern);
            } else if (input instanceof HTMLTextAreaElement) {
                // `HTMLTextAreaElement` unterstützt das pattern-Attribut nicht.
                input.removeAttribute("pattern");
            }

            if (input instanceof HTMLInputElement || input instanceof HTMLTextAreaElement) {
                setOrRemoveAttribute(input, "minlength", effective.minLength !== null ? effective.minLength.toString() : null);
                setOrRemoveAttribute(input, "maxlength", effective.maxLength !== null ? effective.maxLength.toString() : null);
            }

            if (input instanceof HTMLInputElement && BoundedInputTypes.has(input.type)) {
                setOrRemoveAttribute(input, "min", effective.min);
                setOrRemoveAttribute(input, "max", effective.max);
            }
        }
    };

    const applyConstraintsToAllFields = (): void => {
        if (!runtime) {
            return;
        }

        for (const fieldId of Object.keys(runtime.bindingsByFieldId)) {
            applyConstraintsToField(fieldId);
        }
    };

    const getErrorClass = (validity: ValidityState): string => {
        if (validity.valueMissing) {
            return "required_missing";
        }

        if (validity.patternMismatch || validity.typeMismatch || validity.badInput) {
            return "format_invalid";
        }

        if (validity.tooShort || validity.tooLong || validity.rangeUnderflow || validity.rangeOverflow || validity.stepMismatch) {
            return "constraint_violation";
        }

        return "unclear";
    };

    const compareOrdinals = (left: number, right: number, operator: string): boolean => {
        switch (operator) {
            case "eq": return left === right;
            case "neq": return left !== right;
            case "gt": return left > right;
            case "gte": return left >= right;
            case "lt": return left < right;
            case "lte": return left <= right;
            default: return true;
        }
    };

    const findFieldIdForVariable = (variableId: string): string | null => {
        if (!runtime) {
            return null;
        }

        for (const candidateFieldId of Object.keys(runtime.bindingsByFieldId)) {
            if (tryGetFieldAndVariable(candidateFieldId)?.variable.variableId === variableId) {
                return candidateFieldId;
            }
        }

        return null;
    };

    const findViolatedComparison = (fieldId: string): ConstraintDefinition | null => {
        if (!runtime) {
            return null;
        }

        for (const constraint of runtime.contract.constraints ?? []) {
            if (constraint.kind !== "compareVariables") {
                continue;
            }

            if (constraint.target?.kind !== "field" || constraint.target.fieldId !== fieldId) {
                continue;
            }

            const definition = constraint as ConstraintDefinition & {
                leftVariableId?: string;
                rightVariableId?: string;
                operator?: string;
            };
            if (!definition.leftVariableId || !definition.rightVariableId || !definition.operator) {
                continue;
            }

            const leftFieldId = findFieldIdForVariable(definition.leftVariableId);
            const rightFieldId = findFieldIdForVariable(definition.rightVariableId);
            if (!leftFieldId || !rightFieldId) {
                continue;
            }

            const left = toOrdinalOfDeclaredKind(readVariableValueFromField(leftFieldId), valueTypeKindForField(leftFieldId));
            const right = toOrdinalOfDeclaredKind(readVariableValueFromField(rightFieldId), valueTypeKindForField(rightFieldId));
            if (left === null || right === null) {
                // Ohne zwei vergleichbare Werte wird diese Vergleichsregel hier nicht als verletzt gemeldet.
                continue;
            }

            if (!compareOrdinals(left, right, definition.operator)) {
                return constraint;
            }
        }

        return null;
    };

    const isVariableValuePresent = (variableId: string): boolean => {
        if (!runtime) {
            return false;
        }

        const fieldId = findFieldIdForVariable(variableId);
        const value = fieldId ? readVariableValueFromField(fieldId) : runtime.variables[variableId];
        if (value === undefined || value === null) {
            return false;
        }

        if (typeof value === "string") {
            return value.trim().length > 0;
        }

        if (Array.isArray(value)) {
            return value.length > 0;
        }

        return true;
    };

    const findViolatedAllOrNone = (fieldId: string): ConstraintDefinition | null => {
        if (!runtime) {
            return null;
        }

        for (const constraint of runtime.contract.constraints ?? []) {
            if (constraint.kind !== "allOrNone") {
                continue;
            }

            if (constraint.target?.kind !== "field" || constraint.target.fieldId !== fieldId) {
                continue;
            }

            if (constraint.when && !evalExpression(constraint.when, runtime.variables)) {
                continue;
            }

            const relatedVariableIds = constraint.variableIds;
            if (!Array.isArray(relatedVariableIds) || relatedVariableIds.length === 0) {
                continue;
            }

            let anySet = false;
            let anyUnset = false;
            for (const relatedVariableId of relatedVariableIds) {
                if (isVariableValuePresent(relatedVariableId)) {
                    anySet = true;
                } else {
                    anyUnset = true;
                }

                if (anySet && anyUnset) {
                    return constraint;
                }
            }
        }

        return null;
    };

    const errorClassForConstraintKind = (kind: string | null | undefined): string => {
        const normalized = (kind ?? "").trim();
        if (normalized === "required") {
            return "required_missing";
        }

        return normalized === "pattern" ? "format_invalid" : "constraint_violation";
    };

    const collectBrokenRulesThePageDoesNotEnforce = (fieldId: string, value: RuntimeValue | undefined): ConstraintDefinition[] =>
        getActiveConstraintsForField(fieldId)
            .filter((constraint) => !isBlockingConstraint(constraint)
                && isContractRuleBroken(
                    constraint,
                    value,
                    valueTypeKindForField(fieldId),
                    () => throwRuntimeError(RuntimeErrorCodeContractPatternInvalid),
                    () => throwRuntimeError(RuntimeErrorCodeContractBoundInvalid)));

    /** Verletzte Feldregel mit zugehörigem Eingabeelement und Prüfergebnis. */
    interface BrokenFieldRule extends BrokenRuleAtOneField {
        target: HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;
        result: FieldValidationResult;
    }

    /** Ergebnisse für die Eingabesperre und den offenen Telemetriefehler eines Feldes. */
    interface FieldValidationViews {
        // Sperrendes Ergebnis aus Vertragsregeln oder nativer Browservalidierung.
        enforced: FieldValidationResult;
        // Ausgewählter offener Fehler, auch wenn sein Schweregrad die Eingabe nicht sperrt.
        openError: FieldValidationResult | null;
    }

    const validateFieldViews = (fieldId: string, options?: { showErrors?: boolean }): FieldValidationViews => {
        if (!runtime) {
            return { enforced: { isValid: true }, openError: null };
        }

        const binding = runtime.bindingsByFieldId[fieldId];
        if (!binding) {
            return { enforced: { isValid: true }, openError: null };
        }

        const showErrors = options?.showErrors === true;
        const finish = (result: FieldValidationResult): FieldValidationResult => {
            if (showErrors) {
                setFieldValidationFeedback(binding, result);
            }

            return result;
        };

        // Durch Regeln verborgene oder deaktivierte Felder werden nicht geprüft.
        // Auf anderen Gruppenseiten liegende Felder bleiben Teil der vollständigen Formularprüfung.
        if (isFieldHiddenByRules(binding)) {
            return { enforced: finish({ isValid: true }), openError: null };
        }

        if (binding.inputs.every((i) => i.disabled)) {
            return { enforced: finish({ isValid: true }), openError: null };
        }

        for (const input of binding.inputs) {
            input.setCustomValidity("");
        }

        const effective = computeEffectiveConstraintsForField(fieldId);
        const value = readVariableValueFromField(fieldId);

        // Erkannte Regelverletzungen sammeln und anschließend den offenen Fehler auswählen.
        // Die Auswahl soll nicht allein von der Reihenfolge der Prüfungen abhängen.
        const brokenRules: BrokenFieldRule[] = [];
        const reportBrokenRule = (
            target: HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement,
            result: FieldValidationResult
        ): void => {
            const constraintId = result.violatedConstraintId ?? "";
            if (brokenRules.some((broken) => broken.constraintId === constraintId)) {
                return;
            }

            brokenRules.push({
                constraintId,
                severity: result.violatedSeverity ?? "error",
                target,
                result
            });
        };

        if (valueTypeKindForField(fieldId) === "set") {
            const hasSelection = Array.isArray(value) && value.length > 0;
            if (effective.required && !hasSelection) {
                const feedback = resolveValidationFeedback(effective.requiredConstraint, "required");

                const first = binding.inputs.find((i) => i instanceof HTMLInputElement) ?? binding.inputs[0];

                reportBrokenRule(first, {
                    isValid: false,
                    errorClass: "required_missing",
                    violatedConstraintId: effective.requiredConstraint?.constraintId ?? "required",
                    violatedErrorCode: effective.requiredConstraint?.code,
                    violatedSeverity: effective.requiredConstraint?.severity ?? "error",
                    violatedScope: normalizeContractScope(effective.requiredConstraint?.scope),
                    message: feedback.message,
                    remediation: feedback.remediation
                });
            }
        }

        // oneOf prüft bei skalaren Werten die Mitgliedschaft in values.
        // Bei Mengenwerten beschreibt jeder Eintrag von values eine vollständige erlaubte Menge.
        // Skalare Einträge werden dafür als Menge mit einem Element behandelt.
        if (effective.oneOfValues && value !== undefined) {
            const asSet = (v: RuntimeValue): RuntimeValue => (Array.isArray(v) ? v : [v]) as RuntimeValue;
            const ok = Array.isArray(value)
                ? effective.oneOfValues.some((entry) => valuesEqual(asSet(value), asSet(entry)))
                : effective.oneOfValues.includes(value);

            if (!ok) {
                const feedback = resolveValidationFeedback(effective.oneOfConstraint, "invalidValue");

                reportBrokenRule(binding.inputs[0], {
                    isValid: false,
                    errorClass: "constraint_violation",
                    violatedConstraintId: effective.oneOfConstraint?.constraintId ?? "oneOf",
                    violatedErrorCode: effective.oneOfConstraint?.code,
                    violatedSeverity: effective.oneOfConstraint?.severity ?? "error",
                    violatedScope: normalizeContractScope(effective.oneOfConstraint?.scope),
                    message: feedback.message,
                    remediation: feedback.remediation
                });
            }
        }

        const violatedComparison = findViolatedComparison(fieldId);
        if (violatedComparison) {
            const feedback = resolveValidationFeedback(violatedComparison, "invalidValue");

            reportBrokenRule(binding.inputs[0], {
                isValid: false,
                errorClass: "constraint_violation",
                violatedConstraintId: violatedComparison.constraintId,
                violatedErrorCode: violatedComparison.code,
                violatedSeverity: violatedComparison.severity ?? "error",
                violatedScope: normalizeContractScope(violatedComparison.scope),
                message: feedback.message,
                remediation: feedback.remediation
            });
        }

        const violatedAllOrNone = findViolatedAllOrNone(fieldId);
        if (violatedAllOrNone) {
            const feedback = resolveValidationFeedback(violatedAllOrNone, "invalidValue");

            reportBrokenRule(binding.inputs[0], {
                isValid: false,
                errorClass: "constraint_violation",
                violatedConstraintId: violatedAllOrNone.constraintId,
                violatedErrorCode: violatedAllOrNone.code,
                violatedSeverity: violatedAllOrNone.severity ?? "error",
                violatedScope: normalizeContractScope(violatedAllOrNone.scope),
                message: feedback.message,
                remediation: feedback.remediation
            });
        }

        // `HTMLTextAreaElement` unterstützt keine musterbasierte Gültigkeit. Muster werden von Hand geprüft.
        if (effective.pattern && effective.pattern.trim().length > 0 && binding.inputs[0] instanceof HTMLTextAreaElement && typeof value === "string") {
            if (value.length > 0 && !isPatternSatisfied(value, effective.pattern, () => throwRuntimeError(RuntimeErrorCodeContractPatternInvalid))) {
                const feedback = resolveValidationFeedback(effective.patternConstraint, "invalidFormat");

                reportBrokenRule(binding.inputs[0], {
                    isValid: false,
                    errorClass: "format_invalid",
                    violatedConstraintId: effective.patternConstraint?.constraintId ?? "pattern",
                    violatedErrorCode: effective.patternConstraint?.code,
                    violatedSeverity: effective.patternConstraint?.severity ?? "error",
                    violatedScope: normalizeContractScope(effective.patternConstraint?.scope),
                    message: feedback.message,
                    remediation: feedback.remediation
                });
            }
        }

        // Programmierte Wertänderungen lösen die native Längenprüfung nicht zuverlässig aus.
        // Deshalb werden Mindest- und Höchstlänge zusätzlich mit dem Vertragsprüfer ausgewertet.
        // Die HTML-Attribute bleiben für die native Eingabebehandlung erhalten.
        for (const lengthConstraint of [effective.minLengthConstraint, effective.maxLengthConstraint]) {
            if (!lengthConstraint) {
                continue;
            }

            if (!isContractRuleBroken(
                lengthConstraint,
                value,
                valueTypeKindForField(fieldId),
                () => throwRuntimeError(RuntimeErrorCodeContractPatternInvalid),
                () => throwRuntimeError(RuntimeErrorCodeContractBoundInvalid))) {
                continue;
            }

            const feedback = resolveValidationFeedback(lengthConstraint, "invalidValue");

            reportBrokenRule(binding.inputs[0], {
                isValid: false,
                errorClass: "constraint_violation",
                violatedConstraintId: lengthConstraint.constraintId,
                violatedErrorCode: lengthConstraint.code,
                violatedSeverity: lengthConstraint.severity ?? "error",
                violatedScope: normalizeContractScope(lengthConstraint.scope),
                message: feedback.message,
                remediation: feedback.remediation
            });
        }

        // Auch verletzte Regeln mit warning oder info werden für die Telemetrie erfasst.
        // Sie sperren die Eingabe nicht.
        for (const constraint of collectBrokenRulesThePageDoesNotEnforce(fieldId, value)) {
            const feedback = resolveValidationFeedback(constraint, "invalidValue");

            reportBrokenRule(binding.inputs[0], {
                isValid: false,
                errorClass: errorClassForConstraintKind(constraint.kind),
                violatedConstraintId: constraint.constraintId,
                violatedErrorCode: constraint.code,
                violatedSeverity: constraint.severity,
                violatedScope: normalizeContractScope(constraint.scope),
                message: feedback.message,
                remediation: feedback.remediation
            });
        }

        const first = binding.inputs[0];
        const nativelyValid = first.checkValidity();
        const validity = first.validity;
        const errorClass = getErrorClass(validity);

        // Native Validierungsfehler werden nach Möglichkeit einer Vertragsregel zugeordnet.
        // Ohne passende Vertragsregel wird unten ein eigener Browserfehler erzeugt.
        if (!nativelyValid) {
            if (validity.valueMissing && effective.requiredConstraint) {
                const feedback = resolveValidationFeedback(effective.requiredConstraint, "required");

                reportBrokenRule(first, {
                    isValid: false,
                    errorClass,
                    violatedConstraintId: effective.requiredConstraint.constraintId,
                    violatedErrorCode: effective.requiredConstraint.code,
                    violatedSeverity: effective.requiredConstraint.severity,
                    violatedScope: normalizeContractScope(effective.requiredConstraint.scope),
                    message: feedback.message,
                    remediation: feedback.remediation
                });
            }

            if (validity.patternMismatch && effective.patternConstraint) {
                const feedback = resolveValidationFeedback(effective.patternConstraint, "invalidFormat");

                reportBrokenRule(first, {
                    isValid: false,
                    errorClass,
                    violatedConstraintId: effective.patternConstraint.constraintId,
                    violatedErrorCode: effective.patternConstraint.code,
                    violatedSeverity: effective.patternConstraint.severity,
                    violatedScope: normalizeContractScope(effective.patternConstraint.scope),
                    message: feedback.message,
                    remediation: feedback.remediation
                });
            }

            if (validity.tooShort && effective.minLengthConstraint) {
                const feedback = resolveValidationFeedback(effective.minLengthConstraint, "invalidValue");

                reportBrokenRule(first, {
                    isValid: false,
                    errorClass,
                    violatedConstraintId: effective.minLengthConstraint.constraintId,
                    violatedErrorCode: effective.minLengthConstraint.code,
                    violatedSeverity: effective.minLengthConstraint.severity,
                    violatedScope: normalizeContractScope(effective.minLengthConstraint.scope),
                    message: feedback.message,
                    remediation: feedback.remediation
                });
            }

            if (validity.tooLong && effective.maxLengthConstraint) {
                const feedback = resolveValidationFeedback(effective.maxLengthConstraint, "invalidValue");

                reportBrokenRule(first, {
                    isValid: false,
                    errorClass,
                    violatedConstraintId: effective.maxLengthConstraint.constraintId,
                    violatedErrorCode: effective.maxLengthConstraint.code,
                    violatedSeverity: effective.maxLengthConstraint.severity,
                    violatedScope: normalizeContractScope(effective.maxLengthConstraint.scope),
                    message: feedback.message,
                    remediation: feedback.remediation
                });
            }

            if (validity.rangeUnderflow && effective.minConstraint) {
                const feedback = resolveValidationFeedback(effective.minConstraint, "invalidValue");

                reportBrokenRule(first, {
                    isValid: false,
                    errorClass,
                    violatedConstraintId: effective.minConstraint.constraintId,
                    violatedErrorCode: effective.minConstraint.code,
                    violatedSeverity: effective.minConstraint.severity,
                    violatedScope: normalizeContractScope(effective.minConstraint.scope),
                    message: feedback.message,
                    remediation: feedback.remediation
                });
            }

            if (validity.rangeOverflow && effective.maxConstraint) {
                const feedback = resolveValidationFeedback(effective.maxConstraint, "invalidValue");

                reportBrokenRule(first, {
                    isValid: false,
                    errorClass,
                    violatedConstraintId: effective.maxConstraint.constraintId,
                    violatedErrorCode: effective.maxConstraint.code,
                    violatedSeverity: effective.maxConstraint.severity,
                    violatedScope: normalizeContractScope(effective.maxConstraint.scope),
                    message: feedback.message,
                    remediation: feedback.remediation
                });
            }
        }

        // Einen offenen Fehler und getrennt davon einen sperrenden Fehler auswählen.
        // Native Fehler ohne Vertragsregel dienen erst danach als Ersatz.
        const openContractRule = selectOpenRuleAtField(brokenRules);
        const enforcedRule = selectOpenRuleAtField(
            brokenRules.filter((broken) => normalizeSeverity(broken.severity) === "error"));

        if (enforcedRule) {
            enforcedRule.target.setCustomValidity(enforcedRule.result.message ?? "");

            return { enforced: finish(enforcedRule.result), openError: openContractRule?.result ?? null };
        }

        if (nativelyValid) {
            return {
                enforced: finish({ isValid: true }),
                openError: openContractRule?.result ?? null
            };
        }

        // Für einen nativen Fehler ohne Vertragsregel eine lokalisierte Ersatzmeldung verwenden.
        // Seine Kennung wird aus der verletzten Browserprüfung abgeleitet.
        const violatedConstraintId = validity.valueMissing
            ? "required"
            : validity.patternMismatch
              ? "pattern"
              : validity.tooShort
                ? "minLength"
                : validity.tooLong
                  ? "maxLength"
                  : validity.rangeUnderflow
                    ? "min"
                    : validity.rangeOverflow
                      ? "max"
                      : validity.stepMismatch
                        ? "step"
                        : validity.typeMismatch
                          ? `type.${(first as HTMLInputElement).type}`
                          : validity.badInput
                            ? "badInput"
                            : "unknown";

        const fallbackMessage = getValidationFallbackMessage(getValidationFallbackKind(validity));
        first.setCustomValidity(fallbackMessage);

        const nativeRejection: FieldValidationResult = {
            isValid: false,
            errorClass,
            violatedConstraintId,
            violatedSeverity: "error",
            violatedScope: "field",
            message: fallbackMessage
        };

        return {
            enforced: finish(nativeRejection),
            openError: openContractRule?.result ?? nativeRejection
        };
    };

    const validateField = (fieldId: string, options?: { showErrors?: boolean }): FieldValidationResult =>
        validateFieldViews(fieldId, options).enforced;

    const findOpenErrorAtField = (fieldId: string): FieldValidationResult | null =>
        validateFieldViews(fieldId).openError;

    const buildValidationIssue = (fieldId: string, binding: FieldDomBinding, result: FieldValidationResult): ValidationIssue => {
        const rawLabel = (binding.labelElement?.textContent ?? binding.baseLabelText ?? binding.fieldId).trim();
        const label = rawLabel.length > 0 ? stripRequiredMarker(rawLabel) : fieldId;

        const message =
            (result.message ?? "").trim().length > 0
                ? (result.message ?? "").trim()
                : getValidationFallbackMessage("invalidValue");

        const remediation = (result.remediation ?? "").trim();

        return {
            fieldId,
            label,
            message,
            remediation: remediation.length > 0 ? remediation : null,
            focusTarget: binding.inputs[0]
        };
    };

    const collectValidationIssues = (showErrors: boolean): ValidationIssue[] => {
        if (!runtime) {
            return [];
        }

        const issues: ValidationIssue[] = [];
        for (const fieldId of Object.keys(runtime.bindingsByFieldId)) {
            const binding = runtime.bindingsByFieldId[fieldId];
            const result = validateField(fieldId, { showErrors });
            if (!result.isValid) {
                issues.push(buildValidationIssue(fieldId, binding, result));
            }
        }

        return issues;
    };

    const resolveFieldGroupId = (fieldId: string): string => {
        if (!runtime) {
            return "n/a";
        }

        const binding = runtime.bindingsByFieldId[fieldId];
        if (binding) {
            const fieldGroupId = binding.fieldGroupId.trim();
            if (fieldGroupId.length > 0) {
                return fieldGroupId;
            }
        }

        const fallback = (runtime.contract.entryFieldGroupId ?? "").trim();
        return fallback.length > 0 ? fallback : "n/a";
    };

    const isFieldRelevantForTelemetry = (binding: FieldDomBinding): boolean => {
        // Durch Regeln verborgene Felder sind für die Telemetrie nicht relevant.
        // Felder auf einer anderen Gruppenseite bleiben relevant, sofern sie nicht deaktiviert sind.
        if (isFieldHiddenByRules(binding)) {
            return false;
        }

        if (binding.inputs.every((input) => input.disabled)) {
            return false;
        }

        return true;
    };

    const getOrderedFieldIds = (): string[] => {
        if (!runtime) {
            return [];
        }

        const orderedIds: string[] = [];
        for (const element of Array.from(runtime.form.querySelectorAll<HTMLElement>("[data-field-id]"))) {
            const fieldId = (element.dataset.fieldId ?? "").trim();
            if (fieldId.length === 0 || (!runtime.bindingsByFieldId[fieldId] && !runtime.readOnlyDisplaysByFieldId[fieldId])) {
                continue;
            }

            orderedIds.push(fieldId);
        }

        if (orderedIds.length > 0) {
            return Array.from(new Set(orderedIds));
        }

        return Object.keys(runtime.bindingsByFieldId).sort(compareOrdinal);
    };

    const buildActiveValidationErrorsByKey = (): Record<string, ActiveTelemetryError> => {
        if (!runtime) {
            return {};
        }

        const orderedFieldIds = getOrderedFieldIds();
        const errorsByKey: Record<string, ActiveTelemetryError> = {};

        for (const fieldId of orderedFieldIds) {
            const binding = runtime.bindingsByFieldId[fieldId];
            if (!binding) {
                continue;
            }

            if (!isFieldRelevantForTelemetry(binding)) {
                continue;
            }

            const result = findOpenErrorAtField(fieldId);
            if (result === null) {
                continue;
            }

            const errorCodeCandidate = (result.violatedErrorCode ?? result.violatedConstraintId ?? result.errorClass ?? "unclear").trim();
            const errorCode = errorCodeCandidate.length > 0 ? errorCodeCandidate : "unclear";
            const severity = normalizeErrorSeverity(result.violatedSeverity);
            const scope = normalizeContractScope(result.violatedScope);
            const fieldGroupId = resolveFieldGroupId(fieldId);

            const baseError: Omit<ActiveTelemetryError, "key" | "sourceFieldIds"> = {
                errorCode,
                severity,
                scope
            };

            if (scope === "field") {
                baseError.fieldId = fieldId;
            } else {
                baseError.fieldGroupId = fieldGroupId;
            }

            if (scope === "crossGroup" || scope === "form") {
                baseError.relatedFieldIds = orderedFieldIds.slice().sort(compareOrdinal);
            }

            const key = buildOpenErrorKey(baseError);
            const existing = errorsByKey[key];
            if (existing) {
                existing.sourceFieldIds.push(fieldId);
                continue;
            }

            errorsByKey[key] = {
                ...baseError,
                key,
                sourceFieldIds: [fieldId]
            };
        }

        return errorsByKey;
    };

    const getFieldErrorCodes = (
        activeErrorsByKey: Record<string, ActiveTelemetryError>,
        fieldId: string
    ): string[] => {
        const codes = new Set<string>();
        for (const error of Object.values(activeErrorsByKey)) {
            if (error.severity !== "error") {
                continue;
            }

            if (error.scope === "field" && error.fieldId === fieldId) {
                codes.add(error.errorCode);
            }
        }

        return Array.from(codes).sort(compareOrdinal);
    };

    const getOpenGroupErrorCodes = (
        activeErrorsByKey: Record<string, ActiveTelemetryError>,
        fieldGroupId: string
    ): string[] => {
        const codes = new Set<string>();
        for (const error of Object.values(activeErrorsByKey)) {
            if (error.severity !== "error") {
                continue;
            }

            if (blocksFieldGroupCompletionForError(error, fieldGroupId)) {
                codes.add(error.errorCode);
            }
        }

        return Array.from(codes).sort(compareOrdinal);
    };

    const computeTelemetrySnapshot = (): TelemetrySnapshot | null => {
        if (!runtime) {
            return null;
        }

        const orderedFieldIds = getOrderedFieldIds();
        const activeErrorsByKey = buildActiveValidationErrorsByKey();

        const fieldsById: Record<string, FieldTelemetryStatus> = {};
        for (const fieldId of orderedFieldIds) {
            // Sichtbare Anzeigefelder werden als relevant, nicht verpflichtend und vollständig erfasst.
            // Sie gehen damit in die Positions- und Fortschrittszahlen ein.
            const display = runtime.readOnlyDisplaysByFieldId[fieldId];
            if (display) {
                const displayRelevant = display.wrapper.style.display !== "none";
                fieldsById[fieldId] = {
                    fieldId,
                    fieldGroupId: display.fieldGroupId.length > 0 ? display.fieldGroupId : resolveFieldGroupId(fieldId),
                    relevant: displayRelevant,
                    required: false,
                    completed: displayRelevant,
                    blockingErrorCodes: []
                };
                continue;
            }

            const binding = runtime.bindingsByFieldId[fieldId];
            if (!binding) {
                continue;
            }

            const relevant = isFieldRelevantForTelemetry(binding);
            const required = relevant ? computeEffectiveConstraintsForField(fieldId).required : false;
            const value = readVariableValueFromField(fieldId);
            const hasValue = hasRuntimeValue(value);
            const isValid = relevant ? validateField(fieldId, { showErrors: false }).isValid : true;

            fieldsById[fieldId] = {
                fieldId,
                fieldGroupId: resolveFieldGroupId(fieldId),
                relevant,
                required,
                completed: relevant && isValid && (required ? hasValue : true),
                blockingErrorCodes: getFieldErrorCodes(activeErrorsByKey, fieldId)
            };
        }

        const groupIds = Array.from(
            new Set(
                Object.values(fieldsById)
                    .map((field) => field.fieldGroupId)
                    .filter((groupId) => groupId.trim().length > 0)
            )
        ).sort(compareOrdinal);

        const groupsById: Record<string, FieldGroupTelemetryStatus> = {};
        for (const fieldGroupId of groupIds) {
            const fieldsInGroup = Object.values(fieldsById).filter((field) => field.fieldGroupId === fieldGroupId);
            const relevant = fieldsInGroup.some((field) => field.relevant);
            const baseCompleted = fieldsInGroup.filter((field) => field.relevant).every((field) => field.completed);
            const openGroupErrorCodes = getOpenGroupErrorCodes(activeErrorsByKey, fieldGroupId);

            groupsById[fieldGroupId] = {
                fieldGroupId,
                relevant,
                completed: relevant && baseCompleted && openGroupErrorCodes.length === 0,
                openGroupErrorCodes
            };
        }

        const relevantOrderedFieldIds = orderedFieldIds.filter((fieldId) => fieldsById[fieldId]?.relevant);

        let currentFieldId = "n/a";
        const activeElement = document.activeElement;
        if (activeElement instanceof HTMLInputElement || activeElement instanceof HTMLSelectElement || activeElement instanceof HTMLTextAreaElement) {
            const activeFieldId = getFieldIdFromInput(activeElement);
            if (activeFieldId && fieldsById[activeFieldId]?.relevant) {
                currentFieldId = activeFieldId;
            }
        }

        if (currentFieldId === "n/a" && relevantOrderedFieldIds.length > 0) {
            currentFieldId = relevantOrderedFieldIds[0];
        }

        if (currentFieldId === "n/a" && orderedFieldIds.length > 0) {
            currentFieldId = orderedFieldIds[0];
        }

        const currentFieldGroupId =
            fieldsById[currentFieldId]?.fieldGroupId ??
            (relevantOrderedFieldIds[0] ? fieldsById[relevantOrderedFieldIds[0]]?.fieldGroupId : undefined) ??
            "n/a";

        const navigationIndex = relevantOrderedFieldIds.includes(currentFieldId)
            ? relevantOrderedFieldIds.indexOf(currentFieldId) + 1
            : 0;

        const progressAllTotal = Object.values(fieldsById).filter((field) => field.relevant).length;
        const progressAllCompleted = Object.values(fieldsById).filter((field) => field.relevant && field.completed).length;
        const progressRequiredTotal = Object.values(fieldsById).filter((field) => field.relevant && field.required).length;
        const progressRequiredCompleted = Object.values(fieldsById).filter((field) => field.relevant && field.required && field.completed).length;
        const progressGroupsTotal = Object.values(groupsById).filter((group) => group.relevant).length;
        const progressGroupsCompleted = Object.values(groupsById).filter((group) => group.relevant && group.completed).length;

        return {
            navigation: {
                currentFieldId,
                currentFieldGroupId,
                navigationIndex,
                navigationTotal: relevantOrderedFieldIds.length
            },
            progress: {
                allFields: {
                    completed: progressAllCompleted,
                    total: progressAllTotal
                },
                requiredFieldsOnly: {
                    completed: progressRequiredCompleted,
                    total: progressRequiredTotal
                },
                fieldGroupBased: {
                    completed: progressGroupsCompleted,
                    total: progressGroupsTotal
                }
            },
            fieldsById,
            groupsById,
            activeErrorsByKey
        };
    };

    const fieldStatusChanged = (
        previousStatus: FieldTelemetryStatus | null,
        nextStatus: FieldTelemetryStatus
    ): { relevanceChanged: boolean; completionChanged: boolean } => {
        if (!previousStatus) {
            return { relevanceChanged: true, completionChanged: true };
        }

        const relevanceChanged = previousStatus.relevant !== nextStatus.relevant;
        const completionChanged = previousStatus.completed !== nextStatus.completed;

        return { relevanceChanged, completionChanged };
    };

    const groupStatusChanged = (
        previousStatus: FieldGroupTelemetryStatus | null,
        nextStatus: FieldGroupTelemetryStatus
    ): { relevanceChanged: boolean; completionChanged: boolean } => {
        if (!previousStatus) {
            return { relevanceChanged: true, completionChanged: true };
        }

        const relevanceChanged = previousStatus.relevant !== nextStatus.relevant;
        const completionChanged = previousStatus.completed !== nextStatus.completed;

        return { relevanceChanged, completionChanged };
    };

    const navigationChanged = (previousSnapshot: TelemetrySnapshot | null, nextSnapshot: TelemetrySnapshot): boolean => {
        if (!previousSnapshot) {
            return true;
        }

        return previousSnapshot.navigation.currentFieldId !== nextSnapshot.navigation.currentFieldId
            || previousSnapshot.navigation.currentFieldGroupId !== nextSnapshot.navigation.currentFieldGroupId
            || previousSnapshot.navigation.navigationIndex !== nextSnapshot.navigation.navigationIndex
            || previousSnapshot.navigation.navigationTotal !== nextSnapshot.navigation.navigationTotal;
    };

    const progressChanged = (previousSnapshot: TelemetrySnapshot | null, nextSnapshot: TelemetrySnapshot): boolean => {
        if (!previousSnapshot) {
            return true;
        }

        return previousSnapshot.progress.allFields.completed !== nextSnapshot.progress.allFields.completed
            || previousSnapshot.progress.allFields.total !== nextSnapshot.progress.allFields.total
            || previousSnapshot.progress.requiredFieldsOnly.completed !== nextSnapshot.progress.requiredFieldsOnly.completed
            || previousSnapshot.progress.requiredFieldsOnly.total !== nextSnapshot.progress.requiredFieldsOnly.total
            || previousSnapshot.progress.fieldGroupBased.completed !== nextSnapshot.progress.fieldGroupBased.completed
            || previousSnapshot.progress.fieldGroupBased.total !== nextSnapshot.progress.fieldGroupBased.total;
    };

    const determineZeroDenominatorMode = (
        progress: TelemetrySnapshot["progress"]
    ): "allFields" | "requiredFieldsOnly" | "fieldGroupBased" | undefined => {
        if (progress.allFields.total === 0) {
            return "allFields";
        }

        if (progress.requiredFieldsOnly.total === 0) {
            return "requiredFieldsOnly";
        }

        if (progress.fieldGroupBased.total === 0) {
            return "fieldGroupBased";
        }

        return undefined;
    };

    const emitValidationErrorDiff = (
        previousSnapshot: TelemetrySnapshot | null,
        nextSnapshot: TelemetrySnapshot,
        trigger: ErrorTrigger,
        attemptedFieldId?: string
    ): void => {
        if (!currentSession) {
            return;
        }

        const previousErrors = previousSnapshot?.activeErrorsByKey ?? {};
        const nextErrors = nextSnapshot.activeErrorsByKey;

        for (const key of Object.keys(nextErrors).sort(compareOrdinal)) {
            const error = nextErrors[key];
            // Fehler, die aus dem gerade bearbeiteten Feld stammen, erneut als user_input melden.
            // Das gilt auch dann, wenn derselbe Fehler bereits im vorherigen Snapshot vorhanden war.
            const isTheAttempt = attemptedFieldId !== undefined && error.sourceFieldIds.includes(attemptedFieldId);

            // Bei einem Sendeversuch werden bestehende Fehler erneut gemeldet.
            // Ein unveränderter Fehlerzustand darf diesen erneuten Versuch nicht unterdrücken.
            if (previousErrors[key] && !isTheAttempt && trigger !== "submit_attempt") {
                continue;
            }

            window.formsTelemetry.trackValidationErrorOpened(
                currentSession,
                error.errorCode,
                error.severity,
                isTheAttempt ? "user_input" : trigger,
                error.scope,
                error.fieldId,
                error.fieldGroupId,
                error.relatedFieldIds
            );
        }

        for (const key of Object.keys(previousErrors).sort(compareOrdinal)) {
            if (nextErrors[key]) {
                continue;
            }

            const error = previousErrors[key];
            window.formsTelemetry.trackValidationErrorCleared(
                currentSession,
                error.errorCode,
                error.severity,
                error.scope,
                error.fieldId,
                error.fieldGroupId,
                error.relatedFieldIds
            );
        }
    };

    const emitFieldAndGroupDiff = (
        previousSnapshot: TelemetrySnapshot,
        nextSnapshot: TelemetrySnapshot
    ): void => {
        if (!currentSession) {
            return;
        }

        const fieldChanges: FieldStatusChangeData[] = [];
        const previousFieldsById = previousSnapshot.fieldsById;
        for (const fieldId of Object.keys(nextSnapshot.fieldsById).sort(compareOrdinal)) {
            const nextStatus = nextSnapshot.fieldsById[fieldId];
            const previousStatus = previousFieldsById[fieldId] ?? null;
            const changed = fieldStatusChanged(previousStatus, nextStatus);
            if (changed.relevanceChanged || changed.completionChanged) {
                fieldChanges.push({
                    fieldId: nextStatus.fieldId,
                    fieldGroupId: nextStatus.fieldGroupId,
                    relevant: nextStatus.relevant,
                    required: nextStatus.required,
                    completed: nextStatus.completed,
                    blockingErrorCodes: nextStatus.blockingErrorCodes,
                    relevanceChanged: changed.relevanceChanged,
                    completionChanged: changed.completionChanged
                });
            }
        }

        const fieldGroupChanges: FieldGroupStatusChangeData[] = [];
        const previousGroupsById = previousSnapshot.groupsById;
        for (const groupId of Object.keys(nextSnapshot.groupsById).sort(compareOrdinal)) {
            const nextStatus = nextSnapshot.groupsById[groupId];
            const previousStatus = previousGroupsById[groupId] ?? null;
            const changed = groupStatusChanged(previousStatus, nextStatus);
            if (changed.relevanceChanged || changed.completionChanged) {
                fieldGroupChanges.push({
                    fieldGroupId: nextStatus.fieldGroupId,
                    relevant: nextStatus.relevant,
                    completed: nextStatus.completed,
                    openGroupErrorCodes: nextStatus.openGroupErrorCodes,
                    relevanceChanged: changed.relevanceChanged,
                    completionChanged: changed.completionChanged
                });
            }
        }

        window.formsTelemetry.trackFieldAndGroupStatusChanges(currentSession, {
            fields: fieldChanges,
            fieldGroups: fieldGroupChanges
        });
    };

    const emitNavigationAndStatus = (
        previousSnapshot: TelemetrySnapshot | null,
        nextSnapshot: TelemetrySnapshot
    ): void => {
        if (!currentSession) {
            return;
        }

        const hasNavigationChanged = navigationChanged(previousSnapshot, nextSnapshot);
        const hasProgressChanged = progressChanged(previousSnapshot, nextSnapshot);

        if (hasNavigationChanged) {
            window.formsTelemetry.trackNavigationChanged(currentSession, {
                current_field_id: nextSnapshot.navigation.currentFieldId,
                current_field_group_id: nextSnapshot.navigation.currentFieldGroupId,
                navigation_index: nextSnapshot.navigation.navigationIndex,
                navigation_total: nextSnapshot.navigation.navigationTotal
            });
        }

        if (previousSnapshot === null) {
            window.formsTelemetry.primeFieldAndGroupCompletionBaseline(currentSession, {
                fields: Object.keys(nextSnapshot.fieldsById).sort(compareOrdinal).map((fieldId) => ({
                    fieldId,
                    completed: nextSnapshot.fieldsById[fieldId].completed
                })),
                fieldGroups: Object.keys(nextSnapshot.groupsById).sort(compareOrdinal).map((fieldGroupId) => ({
                    fieldGroupId,
                    completed: nextSnapshot.groupsById[fieldGroupId].completed
                }))
            });
        }

        if (hasNavigationChanged || hasProgressChanged) {
            window.formsTelemetry.trackStatusSnapshot(currentSession, {
                navigation: {
                    current_field_id: nextSnapshot.navigation.currentFieldId,
                    current_field_group_id: nextSnapshot.navigation.currentFieldGroupId,
                    navigation_index: nextSnapshot.navigation.navigationIndex,
                    navigation_total: nextSnapshot.navigation.navigationTotal
                },
                progress: {
                    allFields: nextSnapshot.progress.allFields,
                    requiredFieldsOnly: nextSnapshot.progress.requiredFieldsOnly,
                    fieldGroupBased: nextSnapshot.progress.fieldGroupBased
                },
                zero_denominator_mode: determineZeroDenominatorMode(nextSnapshot.progress)
            });
        }
    };

    // Der Aufrufer gibt den Auslöser für die Telemetriemeldung an.
    // Aus der Zustandsdifferenz allein ist nicht erkennbar, ob Laden, Regeln oder Absenden sie auslösten.
    const syncTelemetrySnapshot = (trigger: ErrorTrigger, attemptedFieldId?: string): void => {
        const nextSnapshot = computeTelemetrySnapshot();
        if (!nextSnapshot) {
            lastTelemetrySnapshot = null;
            return;
        }

        const previousSnapshot = lastTelemetrySnapshot;

        if (currentSession) {
            emitValidationErrorDiff(previousSnapshot, nextSnapshot, trigger, attemptedFieldId);
            if (previousSnapshot) {
                emitFieldAndGroupDiff(previousSnapshot, nextSnapshot);
            }
            emitNavigationAndStatus(previousSnapshot, nextSnapshot);
        }

        lastTelemetrySnapshot = nextSnapshot;
    };

    const validateForm = (): boolean => {
        if (!runtime) {
            return true;
        }

        shouldShowValidationFeedback = true;

        applyConstraintsToAllFields();

        const issues: ValidationIssue[] = [];
        let isValid = true;
        for (const fieldId of Object.keys(runtime.bindingsByFieldId)) {
            const binding = runtime.bindingsByFieldId[fieldId];
            const result = validateField(fieldId, { showErrors: true });
            if (!result.isValid) {
                isValid = false;
                issues.push(buildValidationIssue(fieldId, binding, result));
            }
        }

        renderValidationSummary(issues, !isValid);
        syncTelemetrySnapshot("submit_attempt");
        return isValid;
    };

    const refreshDomFromState = (): void => {
        renderVisibilityAndInteractivity();
        applyConstraintsToAllFields();
    };

    const normalizeConfig = (config: FormNavigationConfig): FormNavigationConfig => ({
        formSelector: config.formSelector.trim(),
        applicationId: config.applicationId.trim(),
        formId: config.formId.trim(),
        requestedLanguage: config.requestedLanguage.trim(),
        participantId: config.participantId?.trim(),
        condition: config.condition?.trim(),
        operationalProbe: config.operationalProbe === true
    });

    /** Zustand für die Initialisierung nach dem Laden gespeicherter Sitzungsdaten. */
    type InitialSessionMode = "editing" | "ending" | "completed";

    const persistedIdentityMatchesConfig = (
        session: PersistedApplicationSession,
        config: FormNavigationConfig
    ): boolean => {
        const expected = identityFromConfig(config);
        return session.identity.applicationId === expected.applicationId
            && session.identity.participantId === expected.participantId
            && session.identity.condition === expected.condition
            && session.identity.taskId === expected.taskId
            && session.identity.recordProvenance === expected.recordProvenance;
    };

    const ensureTelemetrySession = async (
        config: FormNavigationConfig,
        resetApplicationState: boolean
    ): Promise<InitialSessionMode> => {
        const applicationId = config.applicationId;
        const loadedBeforeTelemetry = resetApplicationState ? null : loadPersistedSession(config);
        if (loadedBeforeTelemetry?.deliveryFailureCode) {
            persistedSession = loadedBeforeTelemetry;
            throw new Error(loadedBeforeTelemetry.deliveryFailureCode);
        }

        currentSession = await window.formsTelemetry.startSession({
            applicationId,
            formId: config.formId,
            requestedLanguage: config.requestedLanguage,
            participantId: config.participantId,
            condition: config.condition,
            operationalProbe: config.operationalProbe
        }, resetApplicationState ? () => {
            const key = getStorageKey(identityFromConfig(config));
            localStorage.removeItem(key);
            if (localStorage.getItem(key) !== null) {
                throw new Error("forms.session.storage_unavailable");
            }
        } : undefined);

        const loadedSession = loadedBeforeTelemetry;
        const currentIdentity = currentSession.identity;
        const identityMatches = loadedSession
            && loadedSession.identity.applicationId === currentIdentity.applicationId
            && loadedSession.identity.participantId === currentIdentity.participantId
            && loadedSession.identity.condition === currentIdentity.condition
            && loadedSession.identity.taskId === currentIdentity.taskId
            && loadedSession.identity.recordProvenance === currentIdentity.recordProvenance;

        persistedSession = identityMatches
            ? loadedSession
            : {
                version: 5,
                identity: { ...currentIdentity },
                forms: {},
                completedForms: {}
            };
        if (!savePersistedSession(persistedSession)) {
            throw new Error("forms.session.storage_unavailable");
        }

        if (persistedSession.identity.recordProvenance === "demonstration"
            && (persistedSession.completed || persistedSession.terminalTransition)) {
            // Bei einer Demonstration wird ein gespeicherter Abschluss beim erneuten Aufruf verworfen.
            // So kann das Beispiel erneut bearbeitet werden.
            // Für andere Herkunftsarten bleibt der gespeicherte Abschluss bestehen.
            // Der verworfene Endzustand bestimmt die anschließende Neustartmeldung.
            const discardedOutcome = persistedSession.completed
                ? persistedSession.completed.outcome
                : persistedSession.terminalTransition!.outcome;
            persistedSession = {
                version: 5,
                identity: { ...currentIdentity },
                forms: {},
                completedForms: {}
            };
            if (!savePersistedSession(persistedSession)) {
                throw new Error("forms.session.storage_unavailable");
            }
            demonstrationTerminalStateDiscarded = discardedOutcome;
        }

        if (persistedSession.completed) {
            if (currentSession.taskPhase === "accepted") {
                window.formsTelemetry.commitTask(currentSession);
            } else if (currentSession.taskPhase !== "committed"
                && currentSession.taskPhase !== "idle") {
                throw new Error("telemetry.delivery.checkpoint_invalid");
            }

            if (!window.formsTelemetry.endSession(
                currentSession,
                persistedSession.completed.outcome === "submitted" ? "submitted" : "cancelled")) {
                throw new Error("telemetry.delivery.checkpoint_cleanup_failed");
            }
            currentSession = null;
            return "completed";
        }

        if (persistedSession.terminalTransition) {
            if ((currentSession.taskPhase !== "ending" && currentSession.taskPhase !== "accepted")
                || JSON.stringify(currentSession.terminalIntent)
                    !== JSON.stringify(persistedSession.terminalTransition.taskEndOptions)) {
                throw new Error("telemetry.delivery.checkpoint_invalid");
            }
            return "ending";
        }

        if (currentSession.taskPhase === "ending"
            || currentSession.taskPhase === "accepted"
            || currentSession.taskPhase === "committed") {
            throw new Error("telemetry.delivery.checkpoint_invalid");
        }

        window.formsTelemetry.startTask(currentSession);
        return "editing";
    };

    const getManifestFormInfo = (formId: string): AppNavigationManifestFormInfo | null => {
        const normalized = formId.trim().toLowerCase();
        if (normalized.length === 0) {
            return null;
        }

        for (const form of navigationManifest?.forms ?? []) {
            if (!form || typeof form.formId !== "string") {
                continue;
            }

            if (form.formId.trim().toLowerCase() === normalized) {
                return form;
            }
        }

        return null;
    };

    const getManifestFormDisplayName = (formId: string): string => {
        const info = getManifestFormInfo(formId);
        const displayName = (info?.displayName ?? "").trim();
        return displayName.length > 0 ? displayName : formId;
    };

    const PARTICIPANT_TOKEN_PATTERN = /^[a-z0-9]{6,16}$/;

    const buildFormsUrl = (formId: string | null): string => {
        if (!runtime) {
            return "/";
        }

        const params = new URLSearchParams();
        params.set("app", runtime.config.applicationId);

        const normalizedFormId = (formId ?? "").trim();
        if (normalizedFormId.length > 0) {
            params.set("form", normalizedFormId);
        }

        params.set("lang", runtime.config.requestedLanguage);

        const query = "?" + params.toString();

        const participantId = runtime.config.participantId?.trim() ?? "";
        if (participantId.length === 0 || !PARTICIPANT_TOKEN_PATTERN.test(participantId)) {
            return "/" + query;
        }

        return "/umfrage/" + participantId + query;
    };

    const canonicalizeCrossFormNavigationLink = (link: HTMLAnchorElement): string | null => {
        const targetFormId = (link.dataset.nextForm ?? link.dataset.previousForm ?? "").trim();
        if (targetFormId.length === 0) {
            return null;
        }

        link.href = buildFormsUrl(targetFormId);
        return targetFormId;
    };

    const canonicalizeCrossFormNavigationLinks = (): void => {
        for (const link of document.querySelectorAll<HTMLAnchorElement>(
            ".form-navigation a[data-next-form], .form-navigation a[data-previous-form]")) {
            canonicalizeCrossFormNavigationLink(link);
        }
    };

    const markCurrentFormCompleted = (): void => {
        if (!runtime || !persistedSession) {
            return;
        }

        persistedSession.completedForms ??= {};
        persistedSession.completedForms[runtime.config.formId.trim()] = true;
        savePersistedSession(persistedSession);
    };

    const showMissingPrerequisites = (missingFormIds: string[]): void => {
        if (!runtime) {
            return;
        }

        const box = getValidationSummaryBox();
        if (box) {
            box.replaceChildren();

            const title = document.createElement("strong");
            title.textContent = uiText("prerequisitesTitle");
            box.appendChild(title);

            const list = document.createElement("ul");
            for (const formId of missingFormIds) {
                const li = document.createElement("li");

                const anchor = document.createElement("a");
                anchor.href = buildFormsUrl(formId);
                anchor.textContent = getManifestFormDisplayName(formId);

                li.appendChild(anchor);
                list.appendChild(li);
            }
            box.appendChild(list);

            const selection = document.createElement("p");
            const selectionLink = document.createElement("a");
            selectionLink.href = buildFormsUrl(null);
            selectionLink.textContent = uiText("selectionLink");
            selection.appendChild(selectionLink);
            box.appendChild(selection);

            box.style.display = "";
            box.focus();
        }

        for (const binding of Object.values(runtime.bindingsByFieldId)) {
            binding.wrapper.style.display = "none";
        }

        const nav = document.querySelector<HTMLElement>(".form-navigation");
        if (nav) {
            nav.style.display = "none";
        }

        if (currentSession) {
            try {
                window.formsTelemetry.trackNavigationError(currentSession, ConstraintScopeCrossGroup);
            } catch (error) {
                if (typeof console !== "undefined" && typeof console.warn === "function") {
                    console.warn("formNavigation: navigation_error dropped at the wrong-step exit", error);
                }
            }
        }

        syncTelemetrySnapshot("load");
    };

    const enforcePrerequisitesForCurrentForm = (): boolean => {
        if (!runtime || !persistedSession) {
            return true;
        }

        // Formularvoraussetzungen nur bei eingeladenen Studienläufen prüfen. Direkte Beispielaufrufe bleiben möglich.
        if (!studyRunIsParticipantInvited(runtime.config)) {
            return true;
        }

        const manifestForm = getManifestFormInfo(runtime.config.formId);
        const prerequisiteForms = manifestForm?.prerequisiteForms ?? [];
        if (prerequisiteForms.length === 0) {
            return true;
        }

        const completedFlags = persistedSession.completedForms ?? {};
        const completed = new Set<string>();
        for (const [formId, isCompleted] of Object.entries(completedFlags)) {
            if (isCompleted === true) {
                const normalized = formId.trim().toLowerCase();
                if (normalized.length > 0) {
                    completed.add(normalized);
                }
            }
        }
        const missing: string[] = [];

        const current = runtime.config.formId.trim().toLowerCase();
        for (const rawId of prerequisiteForms) {
            const id = (rawId ?? "").trim();
            if (id.length === 0) {
                continue;
            }

            if (id.trim().toLowerCase() === current) {
                continue;
            }

            if (!completed.has(id.trim().toLowerCase())) {
                missing.push(id);
            }
        }

        if (missing.length === 0) {
            return true;
        }

        showMissingPrerequisites(missing);
        return false;
    };

    const showSharedVariableImportNotice = (): void => {
        if (!runtime || lastSharedVariableImports.length === 0) {
            setRuntimeStatus(null);
            return;
        }

        const labels: string[] = [];
        for (const variableId of lastSharedVariableImports) {
            const fieldIds = runtime.index.variableToFieldIds[variableId] ?? [];
            let added = false;

            for (const fieldId of fieldIds) {
                const field = runtime.index.fieldsById[fieldId];
                const label = resolveLocalizedText(field?.label, runtime.contract, runtime.requestedLanguage);
                if (label) {
                    labels.push(stripRequiredMarker(label));
                    added = true;
                }
            }

            if (!added) {
                labels.push(variableId);
            }
        }

        const unique = Array.from(new Set(labels.map((l) => l.trim()).filter((l) => l.length > 0)));
        setRuntimeStatus(`${uiText("sharedImportPrefix")} ${unique.join(", ")}`);
        lastSharedVariableImports = [];
    };

    const buildInitialVariables = (contract: FormContract, index: ContractIndex, config: FormNavigationConfig): RuntimeVariables => {
        const variables: RuntimeVariables = {};
        lastSharedVariableImports = [];

        for (const variable of contract.variables ?? []) {
            const variableId = (variable.variableId ?? "").trim();
            if (variableId.length === 0) {
                continue;
            }

            const initial = normalizeRuntimeValue(variable.initialValue);
            if (initial !== undefined) {
                variables[variableId] = initial;
            }
        }

        const manifestForm = getManifestFormInfo(config.formId);
        const prerequisiteForms = manifestForm?.prerequisiteForms ?? [];
        const sharedVariables = manifestForm?.sharedVariables ?? [];

        if (sharedVariables.length > 0 && prerequisiteForms.length > 0 && persistedSession) {
            for (const rawVariableId of sharedVariables) {
                const variableId = (rawVariableId ?? "").trim();
                if (variableId.length === 0 || !index.variablesById[variableId]) {
                    continue;
                }

                for (const rawPrereqFormId of prerequisiteForms) {
                    const prereqFormId = (rawPrereqFormId ?? "").trim();
                    if (prereqFormId.length === 0) {
                        continue;
                    }

                    const prereqValues = persistedSession.forms[prereqFormId];
                    const candidate = prereqValues?.[variableId];
                    if (candidate === undefined) {
                        continue;
                    }

                    const normalized = normalizeRuntimeValue(candidate);
                    if (normalized !== undefined) {
                        variables[variableId] = normalized;
                        lastSharedVariableImports.push(variableId);
                        break;
                    }
                }
            }
        }

        const persistedFormValues = persistedSession?.forms[config.formId] ?? {};
        for (const [variableId, value] of Object.entries(persistedFormValues)) {
            if (!index.variablesById[variableId]) {
                continue;
            }

            const normalized = normalizeRuntimeValue(value);
            if (normalized !== undefined) {
                variables[variableId] = normalized;
            }
        }

        return variables;
    };

    const renderCommittedSurface = (
        form: HTMLFormElement,
        outcome: "submitted" | "cancelled",
        identity: EvaluationTaskIdentity
    ): void => {
        const container = form.parentElement;
        if (!container) {
            return;
        }

        const surface = document.createElement("div");
        surface.className = outcome === "submitted" ? "success-message" : "cancel-message";
        surface.setAttribute("role", "status");
        surface.setAttribute("aria-live", "polite");
        surface.setAttribute("aria-atomic", "true");
        surface.setAttribute("tabindex", "-1");

        const title = document.createElement("h2");
        title.textContent = uiText(outcome === "submitted" ? "successTitle" : "cancelTitle");
        surface.appendChild(title);

        const result = document.createElement("p");
        result.textContent = uiText(outcome === "submitted" ? "successSubmittedText" : "cancelledText");
        surface.appendChild(result);

        if (outcome === "submitted") {
            const nextSteps = document.createElement("p");
            nextSteps.textContent = uiText("successNextSteps");
            surface.appendChild(nextSteps);
        }

        if (identity.recordProvenance === "demonstration") {
            const restart = document.createElement("button");
            restart.type = "button";
            restart.className = "btn btn-primary";
            restart.setAttribute("data-mda-terminal-restart", "");
            restart.textContent = uiText("restartLabel");
            restart.addEventListener("click", () => {
                window.localStorage.removeItem(getStorageKey(identity));
                window.location.reload();
            });
            surface.appendChild(restart);
        }

        const selection = document.createElement("a");
        selection.href = "/";
        selection.className = "btn btn-primary";
        selection.textContent = uiText("selectionLink");
        surface.appendChild(selection);

        container.replaceChildren(surface);
        surface.focus();
    };

    const toComparisonActual = (value: RuntimeValue | undefined): string => {
        if (value === undefined || value === null) {
            return "";
        }

        if (Array.isArray(value)) {
            return value.map((item) => String(item)).join(",");
        }

        return String(value);
    };

    const isReadOnlyDisplayRelevantByRules = (display: ReadOnlyDisplayBinding): boolean => {
        if (!runtime) {
            return false;
        }

        const fieldVisible = runtime.visibleByFieldId[display.fieldId] ?? true;
        return fieldVisible && !getHiddenFieldGroupIds().has(display.fieldGroupId);
    };

    const isFieldRelevant = (fieldId: string): boolean => {
        if (!runtime) {
            return false;
        }

        const display = runtime.readOnlyDisplaysByFieldId[fieldId];
        if (display) {
            return isReadOnlyDisplayRelevantByRules(display);
        }

        const binding = runtime.bindingsByFieldId[fieldId];
        return binding ? isFieldRelevantForTelemetry(binding) : false;
    };

    const collectAnswerOptions = (fieldId: string): { optionCodes: string[]; optionLabels: string[] } => {
        const empty = { optionCodes: [] as string[], optionLabels: [] as string[] };
        if (!runtime) {
            return empty;
        }

        const binding = runtime.bindingsByFieldId[fieldId];
        if (binding) {
            const optionCodes: string[] = [];
            const optionLabels: string[] = [];
            const first = binding.inputs[0];
            if (first instanceof HTMLSelectElement) {
                for (const option of Array.from(first.options)) {
                    const code = option.value.trim();
                    if (code.length === 0) {
                        continue;
                    }

                    optionCodes.push(code);
                    const label = option.textContent?.trim() ?? "";
                    optionLabels.push(label.length > 0 ? label : code);
                }
            } else {
                for (const input of binding.inputs) {
                    if (!(input instanceof HTMLInputElement) || input.type !== "checkbox") {
                        continue;
                    }

                    const code = input.value.trim();
                    if (code.length === 0) {
                        continue;
                    }

                    optionCodes.push(code);
                    const label = (input.labels?.[0]?.textContent ?? "").trim();
                    optionLabels.push(label.length > 0 ? label : code);
                }
            }

            if (optionCodes.length > 0) {
                return { optionCodes, optionLabels };
            }
        }

        const answer = runtime.index.fieldsById[fieldId];
        const answerVariable = answer ? runtime.index.variablesById[answer.variableId] : undefined;
        const optionCodes = (answerVariable?.valueType.values ?? [])
            .map((option) => option.code)
            .filter((code) => typeof code === "string" && code.length > 0);
        return { optionCodes, optionLabels: optionCodes.slice() };
    };

    const collectTaskCards = (): TaskCardAssessmentCard[] => {
        if (!runtime) {
            return [];
        }

        const cards: TaskCardAssessmentCard[] = [];
        for (const fieldGroup of runtime.contract.fieldGroups ?? []) {
            const memberFieldIds = (fieldGroup.members ?? [])
                .filter((member): member is { kind: "field"; fieldId: string } => member.kind === "field")
                .map((member) => member.fieldId);
            if (memberFieldIds.length !== 2) {
                continue;
            }

            const instruction = runtime.index.fieldsById[memberFieldIds[0]];
            const answer = runtime.index.fieldsById[memberFieldIds[1]];
            if (!instruction || !answer
                || instruction.editability !== "readOnly"
                || answer.editability !== "userEditable") {
                continue;
            }

            const instructionVariable = runtime.index.variablesById[instruction.variableId];
            const target = typeof instructionVariable?.initialValue === "string"
                ? instructionVariable.initialValue
                : null;
            const options = collectAnswerOptions(answer.fieldId);

            cards.push({
                instructionRelevant: isFieldRelevant(instruction.fieldId),
                answerRelevant: isFieldRelevant(answer.fieldId),
                target,
                actual: toComparisonActual(readVariableValueFromField(answer.fieldId)),
                optionCodes: options.optionCodes,
                optionLabels: options.optionLabels
            });
        }

        return cards;
    };

    const createTaskEndOptions = (outcome: "submitted" | "cancelled"): TaskEndOptions => {
        const processSubmitted = outcome === "submitted";
        const terminationClassification: ParticipantTaskTerminationClassification = outcome === "cancelled"
            ? "participant_cancelled"
            : "completed";
        const snapshot = computeTelemetrySnapshot();
        const required = snapshot?.progress.requiredFieldsOnly ?? { total: 0, completed: 0 };
        const openBlockingValidationCount = snapshot
            ? Object.keys(snapshot.activeErrorsByKey).length
            : 0;
        const mismatchCode = assessTaskCardMismatch({
            cards: collectTaskCards(),
            processSubmitted,
            requiredFieldsTotal: required.total,
            requiredFieldsCompleted: required.completed,
            openBlockingValidationCount,
            terminationClassification
        });

        return {
            processSubmitted,
            requiredFieldsTotal: required.total,
            requiredFieldsCompleted: required.completed,
            openBlockingValidationCount,
            terminationClassification,
            mismatchCode,
            correctnessBasis: "log_and_system_check"
        };
    };

    const showTerminalPending = (form: HTMLFormElement): void => {
        form.setAttribute("aria-busy", "true");
        form.setAttribute("inert", "");
        const container = form.parentElement;
        if (!container) {
            return;
        }
        container.querySelector("[data-mda-terminal-error]")?.remove();
        let status = container.querySelector<HTMLElement>("[data-mda-terminal-status]");
        if (!status) {
            status = document.createElement("div");
            status.setAttribute("data-mda-terminal-status", "true");
            status.setAttribute("role", "status");
            status.setAttribute("aria-live", "polite");
            status.setAttribute("aria-atomic", "true");
            status.setAttribute("tabindex", "-1");
            status.className = "status";
            container.insertBefore(status, form);
        }
        status.textContent = uiText("telemetryTerminalPendingText");
        status.focus();
    };

    const clearTerminalPending = (form: HTMLFormElement): void => {
        form.removeAttribute("aria-busy");
        form.removeAttribute("inert");
        form.removeAttribute("aria-disabled");
        form.removeAttribute("data-terminal-snapshot");
        form.classList.remove("terminal-snapshot");
        form.parentElement?.querySelector("[data-mda-terminal-status]")?.remove();
        form.parentElement?.querySelector("[data-mda-terminal-error]")?.remove();
    };

    const handleBeforeUnload = (): void => {
        if (!runtime || !currentSession) {
            return;
        }

        if (internalNavigationInProgress) {
            return;
        }

        persistCurrentFormValues();
        window.formsTelemetry.endSession(currentSession, "abandoned");
        lastTelemetrySnapshot = null;
    };

    const handleFocusIn = (event: Event): void => {
        if (!runtime || !currentSession || terminalTransitionPending) {
            return;
        }

        const target = event.target;
        if (!(target instanceof HTMLInputElement || target instanceof HTMLSelectElement || target instanceof HTMLTextAreaElement)) {
            return;
        }

        const fieldId = getFieldIdFromInput(target);
        if (!fieldId) {
            return;
        }

        const binding = runtime.bindingsByFieldId[fieldId];
        const fieldGroupId = binding?.fieldGroupId?.trim() ?? undefined;

        window.formsTelemetry.trackFieldFocus(currentSession, fieldId, fieldGroupId);

        if (fieldGroupId && fieldGroupId !== lastEnteredFieldGroupId) {
            lastEnteredFieldGroupId = fieldGroupId;
            if (handleRules("onEnterFieldGroup")) {
                refreshDomFromState();
                persistCurrentFormValues();
            }
        }

        syncTelemetrySnapshot("rule_visibility");
    };

    const handleChange = (event: Event): void => {
        if (!runtime || terminalTransitionPending) {
            return;
        }

        const target = event.target;
        if (!(target instanceof HTMLInputElement || target instanceof HTMLSelectElement || target instanceof HTMLTextAreaElement)) {
            return;
        }

        const fieldId = getFieldIdFromInput(target);
        if (!fieldId) {
            return;
        }

        const resolved = tryGetFieldAndVariable(fieldId);
        if (resolved) {
            const value = readVariableValueFromField(fieldId);
            setVariableValue(resolved.variable.variableId, value);
        }

        persistCurrentFormValues();

        if (handleRules("onValueChanged")) {
            refreshDomFromState();
            persistCurrentFormValues();
        } else {
            refreshDomFromState();
        }

        const result = validateField(fieldId, { showErrors: shouldShowValidationFeedback });

        if (shouldShowValidationFeedback) {
            renderValidationSummary(collectValidationIssues(true), false);
        }

        if (event.isTrusted && currentSession) {
            window.formsTelemetry.trackFieldAttempt(currentSession, fieldId, "keyboard", true);
        }

        // Fehler aus dem bearbeiteten Feld als Eingabereaktion melden.
        // Andere durch den Regellauf entstandene Fehler behalten den Auslöser rule_visibility.
        syncTelemetrySnapshot("rule_visibility", fieldId);
    };

    const handleSubmit = (event: Event): void => {
        event.preventDefault();
        submit();
    };

    const initializeRuntime = (config: FormNavigationConfig, editing: boolean): void => {
        internalNavigationInProgress = false;
        lastEnteredFieldGroupId = null;
        runtime = null;
        groupPager?.dispose();
        groupPager = null;

        const form = document.querySelector<HTMLFormElement>(config.formSelector);
        if (!form) {
            console.warn(`[formNavigation] Form not found: ${config.formSelector}`);
            return;
        }

        shouldShowValidationFeedback = false;
        clearValidationSummary();
        setRuntimeStatus(null);
        navigationManifest = readEmbeddedNavigationManifest();

        const contract = readEmbeddedContract();
        if (!contract) {
            const box = createOrGetRuntimeErrorBox(form);
            box.textContent = RuntimeErrorCodeContractParseFailed;
            box.style.display = "";
            box.focus?.();
            return;
        }

        const localizationError = validateContractLocalizationCoverage(contract, config.requestedLanguage);
        if (localizationError) {
            const box = createOrGetRuntimeErrorBox(form);
            box.textContent = localizationError;
            box.style.display = "";
            box.focus?.();
            return;
        }

        const index = buildContractIndex(contract);
        const bindingsByFieldId = buildFieldBindingsById(form);
        const readOnlyDisplaysByFieldId = buildReadOnlyDisplayBindingsById(form);
        const variables = buildInitialVariables(contract, index, config);

        runtime = {
            config,
            contract,
            index,
            form,
            bindingsByFieldId,
            readOnlyDisplaysByFieldId,
            variables,
            visibleByFieldId: {},
            visibleByFieldGroupId: {},
            enabledByFieldId: {},
            readOnlyByFieldId: {},
            requestedLanguage: config.requestedLanguage
        };

        // Die Studienidentität schon vor dem Kopieren oder Vorlesen formularübergreifender Links im Pfad erhalten.
        canonicalizeCrossFormNavigationLinks();

        groupPager = createFormGroupPager({
            form,
            getText: (key) => uiText(key),
            isGroupRuleHidden: (fieldGroupId) => getHiddenFieldGroupIds().has(fieldGroupId),
            renderRuleVisibility: () => renderVisibilityAndInteractivity()
        });

        syncDomFromVariables();

        if (editing) {
            handleRules("onInit");
            refreshDomFromState();
            persistCurrentFormValues();
            showSharedVariableImportNotice();
            syncTelemetrySnapshot("load");
        }
    };

    let documentInitializationStarted = false;

    const init = async (config: FormNavigationConfig): Promise<void> => {
        const navigation = performance.getEntriesByType?.("navigation")[0] as PerformanceNavigationTiming | undefined;
        const resetApplicationState = !documentInitializationStarted && navigation?.type === "reload";
        documentInitializationStarted = true;
        removeTrackedListeners();
        removeTelemetryFailureSubscription();

        const normalized = normalizeConfig(config);
        requestedUiLanguage = normalized.requestedLanguage || formNavigationUiLanguageFallback;
        internalNavigationInProgress = false;
        lastTelemetrySnapshot = null;
        terminalTransitionPending = false;
        demonstrationTerminalStateDiscarded = null;
        pendingTelemetryFailure = null;
        initializationState = "pending";

        unsubscribeFromTelemetryFailures = window.formsTelemetry.subscribeToDeliveryFailures((failedSession, error) => {
            if (!currentSession || failedSession !== currentSession) {
                return;
            }

            pendingTelemetryFailure = error;
            terminalTransitionPending = true;
            internalNavigationInProgress = false;
            if (runtime) {
                captureCurrentDomValues();
            }
            if (persistedSession && isPersistedDeliveryFailureCode(error.message)) {
                persistedSession.deliveryFailureCode = error.message;
            }
            if (runtime) {
                const snapshotStored = persistCurrentFormValues();
                showTerminalTelemetryError(runtime.form, snapshotStored);
            } else if (persistedSession) {
                savePersistedSession(persistedSession);
            }
        });

        let operationalEntryLeft = false;
        const guardedForm = document.querySelector<HTMLFormElement>(normalized.formSelector);
        if (guardedForm) {
            addTrackedListener(guardedForm, "submit", handleSubmit);
            if (normalized.operationalProbe) {
                addTrackedListener(window, "pagehide", () => {
                    operationalEntryLeft = true;
                    initializationState = "failed";
                    terminalTransitionPending = true;
                    showTelemetryError(guardedForm, "telemetryInitializationErrorText");
                });
            }
        }

        let sessionMode: InitialSessionMode;
        try {
            if (normalized.operationalProbe) {
                guardedForm?.setAttribute("inert", "");
                guardedForm?.setAttribute("aria-busy", "true");
                await window.formsTelemetry.initializeOperationalProbe(normalized);
            }
            sessionMode = await ensureTelemetrySession(normalized, resetApplicationState);
            if (operationalEntryLeft) {
                throw new Error("telemetry.probe.context_unavailable");
            }
        } catch (error) {
            initializationState = "failed";
            currentSession = null;
            const terminalRestore = resetApplicationState || normalized.operationalProbe ? null : loadPersistedSession(normalized);
            const failureMessage = error !== null
                && typeof error === "object"
                && "message" in error
                && typeof error.message === "string"
                ? error.message
                : "";
            const failedCheckpointRestore = failureMessage === "telemetry.delivery.permanent_rejection"
                || failureMessage === "telemetry.delivery.storage_unavailable"
                || failureMessage === "telemetry.delivery.checkpoint_invalid"
                || failureMessage === "telemetry.delivery.checkpoint_missing";
            if (terminalRestore
                && (terminalRestore.terminalTransition || terminalRestore.completed || failedCheckpointRestore)
                && persistedIdentityMatchesConfig(terminalRestore, normalized)) {
                persistedSession = terminalRestore;
                terminalTransitionPending = true;
                initializeRuntime(normalized, false);
                if (runtime) {
                    showTerminalTelemetryError(runtime.form);
                }
                console.warn("[formNavigation] Evaluation telemetry terminal restore failed.", error);
                return;
            }
            if (guardedForm) {
                const textKey = failureMessage === "telemetry.delivery.identity_writer_unavailable"
                    ? "telemetryConcurrentWriterErrorText"
                    : "telemetryInitializationErrorText";
                showTelemetryError(guardedForm, textKey);
            }
            console.warn("[formNavigation] Evaluation telemetry initialization failed.", error);
            return;
        }

        initializationState = "ready";
        if (normalized.operationalProbe) {
            guardedForm?.removeAttribute("inert");
            guardedForm?.removeAttribute("aria-busy");
        }
        if (sessionMode === "completed") {
            if (guardedForm && persistedSession?.completed) {
                renderCommittedSurface(guardedForm, persistedSession.completed.outcome, persistedSession.identity);
            }
            return;
        }

        initializeRuntime(normalized, sessionMode === "editing");

        if (!runtime) {
            return;
        }

        if (pendingTelemetryFailure || currentSession?.taskPhase === "failed") {
            captureCurrentDomValues();
            const snapshotStored = persistCurrentFormValues();
            showTerminalTelemetryError(runtime.form, snapshotStored);
            return;
        }

        if (sessionMode === "ending") {
            terminalTransitionPending = true;
            showTerminalPending(runtime.form);
            addTrackedListener(window, "beforeunload", handleBeforeUnload);
            if (currentSession && persistedSession?.terminalTransition) {
                void completeTerminalTransition(
                    currentSession,
                    runtime.form,
                    persistedSession.terminalTransition);
            }
            return;
        }

        if (!enforcePrerequisitesForCurrentForm()) {
            return;
        }

        if (resetApplicationState) {
            setRuntimeStatus(uiText("studyRestartedText"));
        } else if (demonstrationTerminalStateDiscarded) {
            setRuntimeStatus(uiText(demonstrationTerminalStateDiscarded === "submitted"
                ? "demonstrationRestartAfterSubmitText"
                : "demonstrationRestartAfterCancelText"));
        }

        addTrackedListener(runtime.form, "focusin", handleFocusIn);
        addTrackedListener(runtime.form, "change", handleChange);
        addTrackedListener(window, "beforeunload", handleBeforeUnload);
    };

    const validateAndNavigate = (linkElement: HTMLAnchorElement): boolean => {
        if (initializationState !== "ready" || terminalTransitionPending) {
            return false;
        }

        const navAction = (linkElement.dataset.navAction ?? "").trim().toLowerCase();
        const isPrevious = navAction === "previous" || (linkElement.dataset.previousForm ?? "").trim().length > 0;

        if (runtime && !isPrevious) {
            if (!handleRules("onSubmitAttempt")) {
                refreshDomFromState();
                syncTelemetrySnapshot("submit_attempt");
                return false;
            }

            refreshDomFromState();

            if (!validateForm()) {
                return false;
            }

            markCurrentFormCompleted();
        }

        const nextFormId = canonicalizeCrossFormNavigationLink(linkElement);
        if (runtime) {
            persistCurrentFormValues();
        }

        if (currentSession && nextFormId) {
            currentSession = window.formsTelemetry.switchForm(currentSession, nextFormId);
        }

        internalNavigationInProgress = true;
        return true;
    };

    const finalizeSuccessfulTransition = (
        outcome: SessionOutcome,
        form: HTMLFormElement,
        identity: EvaluationTaskIdentity
    ): void => {
        internalNavigationInProgress = true;

        removeTrackedListeners();
        removeTelemetryFailureSubscription();
        clearTerminalPending(form);
        renderCommittedSurface(form, outcome === "submitted" ? "submitted" : "cancelled", identity);

        currentSession = null;
        runtime = null;
        groupPager?.dispose();
        groupPager = null;
        lastTelemetrySnapshot = null;
        terminalTransitionPending = false;
    };

    const showTerminalTelemetryFailure = (error: unknown): void => {
        terminalTransitionPending = true;
        internalNavigationInProgress = false;
        if (runtime) {
            captureCurrentDomValues();
            const snapshotStored = persistCurrentFormValues();
            showTerminalTelemetryError(runtime.form, snapshotStored);
        }
        console.warn("[formNavigation] Evaluation telemetry delivery did not reach acknowledgement.", error);
    };

    const persistTerminalTransition = (
        outcome: "submitted" | "cancelled"
    ): PersistedTerminalTransition | null => {
        if (!runtime || !persistedSession) {
            return null;
        }
        const transition: PersistedTerminalTransition = {
            outcome,
            taskEndOptions: createTaskEndOptions(outcome)
        };
        const next: PersistedApplicationSession = {
            ...persistedSession,
            forms: { ...persistedSession.forms },
            completedForms: { ...(persistedSession.completedForms ?? {}) },
            terminalTransition: transition
        };
        delete next.completed;
        if (!savePersistedSession(next)) {
            return null;
        }
        persistedSession = next;
        return transition;
    };

    const persistCompletionTombstone = (
        outcome: "submitted" | "cancelled"
    ): boolean => {
        if (!runtime || !persistedSession) {
            return false;
        }
        const completedForms = { ...(persistedSession.completedForms ?? {}) };
        if (outcome === "submitted") {
            completedForms[runtime.config.formId.trim()] = true;
        }
        const next: PersistedApplicationSession = {
            ...persistedSession,
            completedForms,
            completed: { outcome }
        };
        delete next.terminalTransition;
        if (!savePersistedSession(next)) {
            return false;
        }
        persistedSession = next;
        return true;
    };

    const completeTerminalTransition = async (
        session: EvaluationSessionHandle,
        form: HTMLFormElement,
        transition: PersistedTerminalTransition
    ): Promise<void> => {
        try {
            await window.formsTelemetry.endTask(session, transition.taskEndOptions);
        } catch (error) {
            showTerminalTelemetryFailure(error);
            return;
        }

        if (!persistCompletionTombstone(transition.outcome)) {
            showTerminalTelemetryFailure(new Error("forms.session.storage_unavailable"));
            return;
        }
        try {
            window.formsTelemetry.commitTask(session);
        } catch (error) {
            showTerminalTelemetryFailure(error);
            return;
        }
        if (!window.formsTelemetry.endSession(session, transition.outcome)) {
            showTerminalTelemetryFailure(new Error("telemetry.delivery.checkpoint_cleanup_failed"));
            return;
        }
        finalizeSuccessfulTransition(transition.outcome, form, session.identity);
    };

    const submit = (): boolean => {
        if (initializationState !== "ready" || terminalTransitionPending) {
            return false;
        }

        if (runtime) {
            if (!handleRules("onSubmitAttempt")) {
                refreshDomFromState();
                syncTelemetrySnapshot("submit_attempt");
                return false;
            }

            refreshDomFromState();

            if (!validateForm()) {
                return false;
            }

            persistCurrentFormValues();
        }

        if (!currentSession || !runtime) {
            return false;
        }

        const transition = persistTerminalTransition("submitted");
        if (!transition) {
            terminalTransitionPending = true;
            showTerminalTelemetryError(runtime.form, false);
            return false;
        }
        terminalTransitionPending = true;
        showTerminalPending(runtime.form);
        void completeTerminalTransition(currentSession, runtime.form, transition);
        return true;
    };

    const cancel = (): void => {
        if (initializationState !== "ready" || terminalTransitionPending) {
            return;
        }

        if (!confirm(uiText("cancelConfirm"))) {
            return;
        }

        if (!currentSession || !runtime) {
            return;
        }

        persistCurrentFormValues();

        const transition = persistTerminalTransition("cancelled");
        if (!transition) {
            terminalTransitionPending = true;
            showTerminalTelemetryError(runtime.form, false);
            return;
        }
        terminalTransitionPending = true;
        showTerminalPending(runtime.form);
        void completeTerminalTransition(currentSession, runtime.form, transition);
    };

    const getSession = (): EvaluationSessionHandle | null => currentSession;

    window.formNavigation = {
        init,
        validateAndNavigate,
        submit,
        cancel,
        getSession
    };
})();
