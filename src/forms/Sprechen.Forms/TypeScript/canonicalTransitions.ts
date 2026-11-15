/**
 * Projiziert Ereignisse des Formulars auf kanonische Zustandswechsel.
 * Für Formular und Sprachkanal gilt dieselbe Übergangstabelle des Evaluationsmodells.
 * Ein Interaktionsschritt zählt einen kanonischen Zustandswechsel. Für den Vergleich
 * beider Bedingungen müssen diese Wechsel gleich projiziert werden.
 */

interface CanonicalTransition {
    canonical_class: CanonicalClass;
    scope: TransitionScope;
    object_id: string;
    resulting_state: ResultingState;
}

const projectCanonicalTransitions = (
    eventName: string,
    data: Record<string, unknown> | undefined
): CanonicalTransition[] => {
    // Ohne bekanntes Feld oder bekannte Feldgruppe enthält die Nutzlast einen Platzhalter.
    // Daraus darf kein Übergang für ein scheinbar gemeinsames Objekt entstehen.
    const text = (name: string): string => {
        const value = data ? data[name] : undefined;
        const trimmed = typeof value === "string" ? value.trim() : "";
        return trimmed === telemetryUnidentifiedObject ? "" : trimmed;
    };
    const flag = (name: string): boolean => (data ? data[name] === true : false);

    const one = (
        canonicalClass: CanonicalClass,
        scope: TransitionScope,
        objectId: string,
        resultingState: ResultingState
    ): CanonicalTransition[] =>
        objectId.length > 0
            ? [{ canonical_class: canonicalClass, scope, object_id: objectId, resulting_state: resultingState }]
            : [];

    const manual = (): CanonicalTransition[] => {
        const supplied = data?.canonical_transitions;
        if (!Array.isArray(supplied) || supplied.length !== 1) {
            throw new Error("A manual step must carry exactly one complete canonical transition.");
        }

        const candidate = supplied[0];
        if (!candidate || typeof candidate !== "object" || Array.isArray(candidate)) {
            throw new Error("A manual step transition must be an object.");
        }

        const transition = candidate as Record<string, unknown>;
        const canonicalClass = transition.canonical_class;
        const scope = transition.scope;
        const objectId = transition.object_id;
        const resultingState = transition.resulting_state;
        if (typeof canonicalClass !== "string"
            || !telemetryCanonicalClasses.has(canonicalClass as CanonicalClass)
            || typeof scope !== "string"
            || !telemetryTransitionScopes.has(scope as TransitionScope)
            || typeof objectId !== "string"
            || objectId.trim().length === 0
            || objectId.trim().length > 128
            || typeof resultingState !== "string"
            || !telemetryResultingStates.has(resultingState as ResultingState)) {
            throw new Error("A manual step transition must use the modeled canonical vocabulary.");
        }

        const normalized: CanonicalTransition = {
            canonical_class: canonicalClass as CanonicalClass,
            scope: scope as TransitionScope,
            object_id: objectId.trim(),
            resulting_state: resultingState as ResultingState
        };

        const validCombination =
            (normalized.canonical_class === "field_target_changed"
                && normalized.scope === "field"
                && normalized.resulting_state === "targeted")
            || (normalized.canonical_class === "field_group_target_changed"
                && normalized.scope === "field_group"
                && normalized.resulting_state === "targeted")
            || (normalized.canonical_class === "confirmed_field_value_changed"
                && normalized.scope === "field"
                && normalized.resulting_state === "confirmed_changed")
            || (normalized.canonical_class === "field_completion_changed"
                && normalized.scope === "field"
                && (normalized.resulting_state === "complete" || normalized.resulting_state === "incomplete"))
            || (normalized.canonical_class === "field_group_completion_changed"
                && normalized.scope === "field_group"
                && (normalized.resulting_state === "complete" || normalized.resulting_state === "incomplete"));

        if (!validCombination) {
            throw new Error("A manual step transition uses an unsupported class, scope, and state combination.");
        }

        return [normalized];
    };

    switch (eventName) {
        case "focus_change":
            return one("field_target_changed", "field", text("field_id"), "targeted");
        case "navigation_changed": {
            // Die einzige Projektion, die zwei Übergänge liefern darf: ein Navigationsschritt kann
            // das Feldziel, das Feldgruppenziel oder beides zugleich wechseln.
            const transitions: CanonicalTransition[] = [];
            transitions.push(...one("field_target_changed", "field", text("current_field_id"), "targeted"));
            transitions.push(...one("field_group_target_changed", "field_group", text("current_field_group_id"), "targeted"));
            return transitions;
        }
        case "value_change":
            return one("confirmed_field_value_changed", "field", text("field_id"), "confirmed_changed");
        case "field_completion_changed":
            return one("field_completion_changed", "field", text("field_id"), flag("completed") ? "complete" : "incomplete");
        case "field_group_completion_changed":
            return one(
                "field_group_completion_changed",
                "field_group",
                text("field_group_id"),
                flag("completed") ? "complete" : "incomplete");
        case "manual_step":
            return manual();
        default:
            // Ereignisse ohne kanonischen Zustandswechsel tragen keine Übergänge.
            // Fehler bei der Projektion dürfen nicht durch eine leere Liste verdeckt werden.
            return [];
    }
};
