/** Eine zweigliedrige Aufgabenkarte mit Ziel, Istwert und Sichtbarkeit. */
interface TaskCardAssessmentCard {
    instructionRelevant: boolean;
    answerRelevant: boolean;
    target: string | null;
    actual: string;
    optionCodes: string[];
    optionLabels?: string[];
}

/** Pflichtstand und Karten für den aufgabenweiten Abweichungscode. */
interface TaskCardAssessmentInput {
    cards: TaskCardAssessmentCard[];
    processSubmitted: boolean;
    requiredFieldsTotal: number;
    requiredFieldsCompleted: number;
    openBlockingValidationCount: number;
    terminationClassification: string;
}

const tokenizeComparisonValue = (value: string, knownTokens: string[]): string[] => {
    const trimmed = value.trim();
    if (trimmed.length === 0) {
        return [];
    }

    if (knownTokens.includes(trimmed)
        || knownTokens.some((token) => token.toLowerCase() === trimmed.toLowerCase())) {
        return [trimmed];
    }

    return trimmed.split(/[,|]/).map((part) => part.trim()).filter((part) => part.length > 0);
};

const mapTokenToOptionCode = (token: string, codes: string[], labels: string[]): string | null => {
    const codeIndex = codes.indexOf(token);
    if (codeIndex >= 0) {
        return codes[codeIndex];
    }

    const folded = token.toLowerCase();
    const labelIndex = labels.findIndex((label) => label === token || label.toLowerCase() === folded);
    if (labelIndex >= 0) {
        return codes[labelIndex] ?? null;
    }

    return null;
};

const compareCodedCard = (card: TaskCardAssessmentCard): "wrong_field_class" | "task_card_mismatch" | "none" => {
    const codes = card.optionCodes;
    const labels = card.optionLabels ?? [];
    const knownTokens = [...codes, ...labels];
    const actualTokens = tokenizeComparisonValue(card.actual, knownTokens);
    const targetTokens = tokenizeComparisonValue(card.target ?? "", knownTokens);
    const actualCodes: string[] = [];
    for (const token of actualTokens) {
        const mapped = mapTokenToOptionCode(token, codes, labels);
        if (mapped === null) {
            return "wrong_field_class";
        }

        actualCodes.push(mapped);
    }

    const targetCodes: string[] = [];
    for (const token of targetTokens) {
        const mapped = mapTokenToOptionCode(token, codes, labels);
        if (mapped !== null) {
            targetCodes.push(mapped);
        }
    }

    if (targetCodes.length === 0) {
        return taskCardComparisonKey(card.actual) === taskCardComparisonKey(card.target ?? "")
            ? "none"
            : "task_card_mismatch";
    }

    const actualSet = [...new Set(actualCodes)].sort();
    const targetSet = [...new Set(targetCodes)].sort();
    if (actualSet.length !== targetSet.length
        || actualSet.some((code, index) => code !== targetSet[index])) {
        return "task_card_mismatch";
    }

    return "none";
};

/**
 * Bestimmt den aufgabenweiten Abweichungscode. Soll- und Istwerte verlassen
 * die Funktion nicht. Kodierte Felder vergleicht die Funktion als Mengen von
 * Codes. Anzeigetext der Vorgabe wird auf Codes abgebildet. Eine Divergenz der
 * Regel-Sichtbarkeit ist ein Abhängigkeitsbruch, eine andere Pagerseite nicht.
 */
// var macht die Funktion für andere klassische Skripte im globalen Geltungsbereich verfügbar.
// eslint-disable-next-line no-var
var assessTaskCardMismatch = (input: TaskCardAssessmentInput): string => {
    if (input.terminationClassification === "technical_failure"
        || input.terminationClassification === "study_directed_abort") {
        return "not_assessed";
    }

    let wrongFieldClass = false;
    let wrongDependencyState = false;
    let taskCardMismatch = false;
    let undecidable = false;
    for (const card of input.cards) {
        if (card.instructionRelevant !== card.answerRelevant) {
            wrongDependencyState = true;
            continue;
        }

        if (!card.answerRelevant) {
            continue;
        }

        if (card.target === null) {
            undecidable = true;
            continue;
        }

        if (card.optionCodes.length > 0) {
            const coded = compareCodedCard(card);
            if (coded === "wrong_field_class") {
                wrongFieldClass = true;
            } else if (coded === "task_card_mismatch") {
                taskCardMismatch = true;
            }

            continue;
        }

        if (taskCardComparisonKey(card.actual) !== taskCardComparisonKey(card.target)) {
            taskCardMismatch = true;
        }
    }

    if (input.requiredFieldsTotal > 0 && input.requiredFieldsCompleted < input.requiredFieldsTotal) {
        return "missing_required";
    }

    if (wrongFieldClass) {
        return "wrong_field_class";
    }

    if (wrongDependencyState) {
        return "wrong_dependency_state";
    }

    if (input.openBlockingValidationCount > 0) {
        return "validation_error_open";
    }

    if (!input.processSubmitted) {
        return "not_submitted";
    }

    if (undecidable) {
        return "not_assessed";
    }

    return taskCardMismatch ? "task_card_mismatch" : "none";
};
