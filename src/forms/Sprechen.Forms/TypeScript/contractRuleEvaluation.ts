/**
 * Wertnormalisierung und Prüfung der unterstützten Vertragsregeln unabhängig vom DOM.
 * Die ins Markup übertragenen Regeln steuern die Browservalidierung. Auch weitere
 * unterstützte Vertragsregeln werden hier für die Fehlererfassung ausgewertet.
 */

type RuntimeValue = string | number | boolean | string[];

const compareOrdinal = (left: string, right: string): number => {
    if (left === right) {
        return 0;
    }

    return left < right ? -1 : 1;
};

const normalizeRuntimeValue = (value: unknown): RuntimeValue | undefined => {
    if (value === null || value === undefined) {
        return undefined;
    }

    if (typeof value === "string" || typeof value === "number" || typeof value === "boolean") {
        return value;
    }

    if (Array.isArray(value) && value.every((v) => typeof v === "string")) {
        return value as string[];
    }

    return undefined;
};

const valuesEqual = (left: RuntimeValue, right: RuntimeValue): boolean => {
    if (Array.isArray(left) && Array.isArray(right)) {
        if (left.length !== right.length) {
            return false;
        }

        const leftSorted = [...left].sort(compareOrdinal);
        const rightSorted = [...right].sort(compareOrdinal);
        for (let i = 0; i < leftSorted.length; i++) {
            if (leftSorted[i] !== rightSorted[i]) {
                return false;
            }
        }

        return true;
    }

    return left === right;
};

const hasRuntimeValue = (value: RuntimeValue | undefined): boolean => {
    if (value === undefined) {
        return false;
    }

    if (Array.isArray(value)) {
        return value.length > 0;
    }

    if (typeof value === "string") {
        return value.trim().length > 0;
    }

    return true;
};

// Ganzzahlen werden auf den gemeinsamen 32-Bit-Wertebereich begrenzt,
// damit Formular und Sprachkanal dieselben Werte akzeptieren.
const SmallestWholeNumber = -2147483648;
const LargestWholeNumber = 2147483647;

const asWholeNumber = (candidate: number): number | null =>
    Number.isInteger(candidate) && candidate >= SmallestWholeNumber && candidate <= LargestWholeNumber
        ? candidate
        : null;

const readWholeNumber = (captured: string): number | null => {
    const trimmed = captured.trim();

    return /^[+-]?\d+$/.test(trimmed) ? asWholeNumber(Number(trimmed)) : null;
};

// Vertragsgrenzen dürfen als JSON-Zahl oder Dezimalzeichenkette vorliegen.
// Beide Formen werden wie ein eingegebener Ganzzahlwert gelesen.
const readAuthoredWholeNumber = (authored: unknown): number | null => {
    if (typeof authored === "number") {
        return asWholeNumber(authored);
    }

    return typeof authored === "string" ? readWholeNumber(authored) : null;
};

const statesBound = (rule: ConstraintDefinition, boundName: string): boolean =>
    Object.prototype.hasOwnProperty.call(rule, boundName);

const authoredBound = (rule: ConstraintDefinition, boundName: string): unknown =>
    (rule as unknown as Record<string, unknown>)[boundName];

/** Gelesener Grenzwert mit numerischer Darstellung und Markup-Text. */
interface StatedBound {
    readonly ordinal: number;
    readonly markupText: string;
}

// Eine vorhandene, aber unlesbare Grenze ist ein Vertragsfehler.
// Sie darf nicht wie eine fehlende Grenze behandelt werden.
const readStatedWholeNumberBound = (
    rule: ConstraintDefinition,
    boundName: string,
    reportUnreadableBound: () => never
): StatedBound | null => {
    if (!statesBound(rule, boundName)) {
        return null;
    }

    const bound = readAuthoredWholeNumber(authoredBound(rule, boundName));
    return bound === null ? reportUnreadableBound() : { ordinal: bound, markupText: bound.toString() };
};

const readStatedCalendarDateBound = (
    rule: ConstraintDefinition,
    boundName: string,
    reportUnreadableBound: () => never
): StatedBound | null => {
    if (!statesBound(rule, boundName)) {
        return null;
    }

    const authored = authoredBound(rule, boundName);
    const markupText = typeof authored === "string" ? authored.trim() : "";
    const bound = markupText.length > 0 ? readCalendarDate(markupText) : null;
    return bound === null ? reportUnreadableBound() : { ordinal: bound, markupText };
};

/** Zusammengehörige untere und obere Grenze einer Regel. */
interface StatedBounds {
    readonly lower: StatedBound | null;
    readonly upper: StatedBound | null;
}

/** Grenznamen und Lesefunktion für ein- oder zweiseitig begrenzte Regeln. */
interface BoundedRuleReading {
    readonly lowerBoundName: string | null;
    readonly upperBoundName: string | null;
    readonly readStatedBound: (
        rule: ConstraintDefinition,
        boundName: string,
        reportUnreadableBound: () => never
    ) => StatedBound | null;
}

const BoundedRuleReadings: Record<string, BoundedRuleReading> = {
    min: { lowerBoundName: "min", upperBoundName: null, readStatedBound: readStatedWholeNumberBound },
    max: { lowerBoundName: null, upperBoundName: "max", readStatedBound: readStatedWholeNumberBound },
    range: { lowerBoundName: "min", upperBoundName: "max", readStatedBound: readStatedWholeNumberBound },
    dateMin: { lowerBoundName: "minDate", upperBoundName: null, readStatedBound: readStatedCalendarDateBound },
    dateMax: { lowerBoundName: null, upperBoundName: "maxDate", readStatedBound: readStatedCalendarDateBound },
    dateRange: { lowerBoundName: "minDate", upperBoundName: "maxDate", readStatedBound: readStatedCalendarDateBound }
};

// Nur eigene Einträge der Tabelle sind Regelarten. Geerbte Objekteigenschaften zählen nicht.
const boundedRuleReading = (kind: string): BoundedRuleReading | null =>
    Object.prototype.hasOwnProperty.call(BoundedRuleReadings, kind) ? BoundedRuleReadings[kind] : null;

// Grenzen des Werttyps werden auf vorhandene Vertragsregelarten abgebildet.
// Die Tabelle enthält nur unterstützte Grenzen für Zeichenketten und Ganzzahlen.
/** Zuordnung einer Werttypgrenze zu einer Vertragsregelart. */
interface ValueTypeStatedBound {
    readonly boundName: string;
    readonly ruleKind: string;
}

const ValueTypeStatedBounds: Record<string, readonly ValueTypeStatedBound[]> = {
    string: [
        { boundName: "minLength", ruleKind: "minLength" },
        { boundName: "maxLength", ruleKind: "maxLength" }
    ],
    integer: [
        { boundName: "min", ruleKind: "min" },
        { boundName: "max", ruleKind: "max" }
    ]
};

// Werttypgrenzen werden in beiden Kanälen als Feldfehler mit demselben Code erfasst.
const ValueTypeStatedRuleCode = "form.valueType.invalid";
const ValueTypeStatedRuleSeverity = "error";
const ValueTypeStatedRuleScope = "field";

// Die ursprüngliche Grenze wird unverändert übernommen. Ihre Gültigkeit prüft der gemeinsame Leser.
const valueTypeStatedRules = (
    variableId: string,
    valueType: VariableValueTypeDefinition | null | undefined
): ConstraintDefinition[] => {
    const statedBounds = valueType ? ValueTypeStatedBounds[(valueType.kind ?? "").trim()] : undefined;
    if (statedBounds === undefined) {
        return [];
    }

    const stated = valueType as unknown as ConstraintDefinition;
    return statedBounds
        .filter((bound) => statesBound(stated, bound.boundName))
        .map((bound) => ({
            constraintId: `valueType.${bound.boundName}`,
            target: { kind: "variable", variableId },
            severity: ValueTypeStatedRuleSeverity,
            code: ValueTypeStatedRuleCode,
            message: {},
            kind: bound.ruleKind,
            scope: ValueTypeStatedRuleScope,
            [bound.boundName]: authoredBound(stated, bound.boundName)
        }) as unknown as ConstraintDefinition);
};

const statesRuleKind = (rule: ConstraintDefinition, ruleKind: string): boolean =>
    (rule.kind ?? "").trim() === ruleKind;

// Die Browserattribute werden aus den Feldregeln abgeleitet, nicht aus vorhandenem Markup zurückgelesen.
/** Aus Feldregeln abgeleitete Attribute und zugehörige Validierungsregeln. */
interface SurfaceCarriedRules {
    readonly required: boolean;
    readonly requiredConstraint: ConstraintDefinition | null;
    readonly pattern: string | null;
    readonly patternConstraint: ConstraintDefinition | null;
    readonly minLength: number | null;
    readonly minLengthConstraint: ConstraintDefinition | null;
    readonly maxLength: number | null;
    readonly maxLengthConstraint: ConstraintDefinition | null;
    readonly min: string | null;
    readonly minConstraint: ConstraintDefinition | null;
    readonly max: string | null;
    readonly maxConstraint: ConstraintDefinition | null;
}

// Unbekannte Schweregrade werden abgewiesen, statt sie als blockierende Fehler einzuordnen.
const carriedRuleSeverity = (
    rule: ConstraintDefinition,
    reportUnmodelledSeverity: () => never
): string => {
    const severity = (rule.severity ?? "").trim().toLowerCase();
    return severity === "error" || severity === "warning" || severity === "info"
        ? severity
        : reportUnmodelledSeverity();
};

// Bei erforderlichen Feldern und Mustern entscheidet die gemeinsame Fehlerpriorität,
// welche Regel das einzelne Browserattribut liefert.
const carriedRuleOfSide = (candidates: readonly ConstraintDefinition[]): ConstraintDefinition | null => {
    const open = selectOpenRuleAtField(candidates.map((rule) => ({
        constraintId: rule.constraintId,
        severity: "error",
        rule
    })));

    return open === null ? null : open.rule;
};

// Von mehreren Längengrenzen gilt im Browser die engste. Bei gleicher Grenze
// entscheidet die Fehlerpriorität, damit Wert und Regelidentität zusammenbleiben.
const carriedLengthBound = (
    rulesOfThisKind: readonly ConstraintDefinition[],
    boundName: "minLength" | "maxLength",
    isNarrower: (candidate: number, carried: number) => boolean
): { bound: number; rule: ConstraintDefinition } | null => {
    const stating = rulesOfThisKind.filter((rule) => typeof rule[boundName] === "number");

    let narrowest: number | null = null;
    for (const rule of stating) {
        const bound = rule[boundName] as number;
        if (narrowest === null || isNarrower(bound, narrowest)) {
            narrowest = bound;
        }
    }

    if (narrowest === null) {
        return null;
    }

    const rule = carriedRuleOfSide(stating.filter((candidate) => candidate[boundName] === narrowest));
    return rule === null ? null : { bound: narrowest, rule };
};

// Auch für Zahl- und Datumsgrenzen gilt die engste Grenze. Bei Gleichstand
// entscheidet die Fehlerpriorität statt der Reihenfolge im Vertrag.
const carriedMagnitudeBound = (
    blocking: readonly ConstraintDefinition[],
    side: "lower" | "upper",
    reportUnreadableBound: () => never
): { bound: StatedBound; rule: ConstraintDefinition } | null => {
    const stating: { bound: StatedBound; rule: ConstraintDefinition }[] = [];
    for (const rule of blocking) {
        const reading = boundedRuleReading((rule.kind ?? "").trim());
        if (reading === null) {
            continue;
        }

        const stated = readStatedBounds(reading, rule, reportUnreadableBound);
        const bound = side === "lower" ? stated.lower : stated.upper;
        if (bound !== null) {
            stating.push({ bound, rule });
        }
    }

    let narrowest: number | null = null;
    for (const entry of stating) {
        if (narrowest === null
            || (side === "lower" ? entry.bound.ordinal > narrowest : entry.bound.ordinal < narrowest)) {
            narrowest = entry.bound.ordinal;
        }
    }

    if (narrowest === null) {
        return null;
    }

    const candidates = stating.filter((entry) => entry.bound.ordinal === narrowest);
    const rule = carriedRuleOfSide(candidates.map((entry) => entry.rule));
    const carried = candidates.find((entry) => entry.rule === rule);
    return carried === undefined ? null : carried;
};

const surfaceCarriedRules = (
    statedRules: readonly ConstraintDefinition[],
    reportUnmodelledSeverity: () => never,
    reportUnreadableBound: () => never
): SurfaceCarriedRules => {
    // Nur Regeln mit Schweregrad error werden als blockierende Browserattribute gesetzt.
    // Hinweise und Warnungen dürfen auf diesem Weg keine Eingabe sperren.
    const blocking = statedRules.filter((rule) => carriedRuleSeverity(rule, reportUnmodelledSeverity) === "error");

    const requiredConstraint = carriedRuleOfSide(
        blocking.filter((rule) => statesRuleKind(rule, "required")));

    const patternConstraint = carriedRuleOfSide(blocking.filter((rule) =>
        statesRuleKind(rule, "pattern")
        && typeof rule.pattern === "string"
        && rule.pattern.trim().length > 0));

    const minLength = carriedLengthBound(
        blocking.filter((rule) => statesRuleKind(rule, "minLength")),
        "minLength",
        (candidate, carried) => candidate > carried);

    const maxLength = carriedLengthBound(
        blocking.filter((rule) => statesRuleKind(rule, "maxLength")),
        "maxLength",
        (candidate, carried) => candidate < carried);

    const min = carriedMagnitudeBound(blocking, "lower", reportUnreadableBound);
    const max = carriedMagnitudeBound(blocking, "upper", reportUnreadableBound);

    return {
        required: requiredConstraint !== null,
        requiredConstraint,
        pattern: patternConstraint === null ? null : (patternConstraint.pattern as string).trim(),
        patternConstraint,
        minLength: minLength?.bound ?? null,
        minLengthConstraint: minLength?.rule ?? null,
        maxLength: maxLength?.bound ?? null,
        maxLengthConstraint: maxLength?.rule ?? null,
        min: min?.bound.markupText ?? null,
        minConstraint: min?.rule ?? null,
        max: max?.bound.markupText ?? null,
        maxConstraint: max?.rule ?? null
    };
};

const readStatedBounds = (
    reading: BoundedRuleReading,
    rule: ConstraintDefinition,
    reportUnreadableBound: () => never
): StatedBounds => ({
    lower: reading.lowerBoundName === null
        ? null
        : reading.readStatedBound(rule, reading.lowerBoundName, reportUnreadableBound),
    upper: reading.upperBoundName === null
        ? null
        : reading.readStatedBound(rule, reading.upperBoundName, reportUnreadableBound)
});

// Datumswerte verwenden yyyy-MM-dd. Der Rückvergleich verhindert, dass ein
// ungültiger Kalendertag durch die Datumskonvertierung in einen Folgemonat verschoben wird.
const readCalendarDate = (captured: string): number | null => {
    const trimmed = captured.trim();
    if (!/^\d{4}-\d{2}-\d{2}$/.test(trimmed)) {
        return null;
    }

    const parsed = Date.parse(`${trimmed}T00:00:00Z`);
    if (!Number.isFinite(parsed)) {
        return null;
    }

    return new Date(parsed).toISOString().slice(0, 10) === trimmed ? parsed : null;
};

// Der deklarierte Feldtyp bestimmt die Umwandlung: Ganzzahl oder UTC-Zeitpunkt eines Datums.
// Ein nicht lesbarer, aber vorhandener Wert bleibt von einer leeren Eingabe unterscheidbar.
const toOrdinalOfDeclaredKind = (value: RuntimeValue | undefined, valueTypeKind: string): number | null => {
    if (valueTypeKind === "integer") {
        if (typeof value === "number") {
            return asWholeNumber(value);
        }

        return typeof value === "string" ? readWholeNumber(value) : null;
    }

    if (valueTypeKind === "date") {
        return typeof value === "string" ? readCalendarDate(value) : null;
    }

    return null;
};

// Das Muster wird auf den gesamten Wert angewendet. Ein syntaktisch ungültiges
// Muster wird über reportUnreadablePattern als Vertragsfehler gemeldet.
const isPatternSatisfied = (value: string, pattern: string, reportUnreadablePattern: () => never): boolean => {
    try {
        return new RegExp(`^(?:${pattern})$`).test(value);
    } catch {
        return reportUnreadablePattern();
    }
};

// Prüft unterstützte Vertragsregeln unabhängig davon, ob sie als Browserattribute vorliegen.
// Nicht unterstützte Regelarten werden hier nicht als verletzt gemeldet.
const isContractRuleBroken = (
    constraint: ConstraintDefinition,
    value: RuntimeValue | undefined,
    valueTypeKind: string,
    reportUnreadablePattern: () => never,
    reportUnreadableBound: () => never
): boolean => {
    const kind = (constraint.kind ?? "").trim();

    if (kind === "required") {
        return !hasRuntimeValue(value);
    }

    // Leere Eingaben werden nur durch required abgewiesen, nicht durch Format- oder Größenregeln.
    if (value === undefined || !hasRuntimeValue(value)) {
        return false;
    }

    if (kind === "pattern") {
        const pattern = typeof constraint.pattern === "string" ? constraint.pattern.trim() : "";
        return pattern.length > 0 && typeof value === "string" && !isPatternSatisfied(value, pattern, reportUnreadablePattern);
    }

    if (kind === "minLength") {
        return typeof constraint.minLength === "number" && typeof value === "string" && value.length < constraint.minLength;
    }

    if (kind === "maxLength") {
        return typeof constraint.maxLength === "number" && typeof value === "string" && value.length > constraint.maxLength;
    }

    if (kind === "oneOf" && Array.isArray(constraint.values)) {
        const allowed = constraint.values.map(normalizeRuntimeValue).filter((v): v is RuntimeValue => v !== undefined);
        if (allowed.length === 0) {
            return false;
        }

        const asSet = (v: RuntimeValue): RuntimeValue => (Array.isArray(v) ? v : [v]) as RuntimeValue;
        return Array.isArray(value)
            ? !allowed.some((entry) => valuesEqual(asSet(value), asSet(entry)))
            : !allowed.includes(value);
    }

    const boundedReading = boundedRuleReading(kind);
    if (boundedReading !== null) {
        // Ohne angegebene Grenze liegt kein Größenverstoß vor. Dies wird vor der Wertumwandlung geprüft.
        const { lower, upper } = readStatedBounds(boundedReading, constraint, reportUnreadableBound);
        if (lower === null && upper === null) {
            return false;
        }

        // Bei einer vorhandenen Grenze gilt ein für den Feldtyp unlesbarer Wert als Regelverstoß.
        const ordinal = toOrdinalOfDeclaredKind(value, valueTypeKind);
        if (ordinal === null) {
            return true;
        }

        return (lower !== null && ordinal < lower.ordinal) || (upper !== null && ordinal > upper.ordinal);
    }

    return false;
};

