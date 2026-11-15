/**
 * Projiziert den Endzustand einer Formularaufgabe nach der gemeinsamen Tabelle
 * des Evaluationsmodells. Ein Aufgabenziel kann ohne Absenden des Gesamtprozesses
 * erreicht werden. Ob der Zustand die Aufgabenkarte erfüllt, entscheidet eine
 * getrennte Bewertung. Das Ergebnis dieser Funktion enthält nur den Endzustand.
 */

interface TaskEndProjectionInput {
    processSubmitted: boolean;
    requiredFieldsTotal: number;
    requiredFieldsCompleted: number;
    openBlockingValidationCount: number;
    terminationClassification: TaskTerminationClassification;
}

/** Projizierter Endzustand ohne fachliche Erfolgsbewertung. */
interface TaskEndProjectionResult {
    endState: TaskEndState;
}

// var macht die Funktion für andere klassische Skripte im globalen Geltungsbereich verfügbar.
// eslint-disable-next-line no-var
var projectTaskEnd = (input: TaskEndProjectionInput): TaskEndProjectionResult => {
    const supportedClassifications: TaskTerminationClassification[] = [
        "completed", "participant_cancelled", "session_abandoned", "study_directed_abort",
        "technical_failure"
    ];
    if (!supportedClassifications.includes(input.terminationClassification)) {
        throw new Error("telemetry.task_end.termination_classification_invalid");
    }

    // Teilnehmerabbruch und Sitzungsaufgabe werden nach Absenden und erreichtem Zielzustand eingeordnet.
    if (input.terminationClassification === "technical_failure") {
        return { endState: "technical_failure" };
    }

    if (input.terminationClassification === "study_directed_abort") {
        return { endState: "aborted" };
    }

    if (input.processSubmitted) {
        return { endState: "process_submitted" };
    }

    // Ohne erforderliche Felder wird kein erreichter Aufgabenzustand abgeleitet.
    const targetStateReached = input.requiredFieldsTotal > 0
        && input.requiredFieldsCompleted >= input.requiredFieldsTotal
        && input.openBlockingValidationCount === 0;

    return { endState: targetStateReached ? "task_target_reached" : "not_submitted" };
};
