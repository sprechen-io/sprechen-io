/**
 * Bildet die Identität offener Fehler im Formular nach derselben Regeltabelle
 * wie FormOpenErrorResolver im Sprachkanal. Regel, Schweregrad und Geltungsbereich
 * bestimmen zusammen mit dem Feld oder der Feldgruppe den Schlüssel.
 * Zusätzliche betroffene Felder und angezeigte Meldungstexte gehören nicht zum Schlüssel.
 */

interface OpenErrorIdentity {
    scope: ContractScope;
    errorCode: string;
    severity: Severity;
    fieldId?: string;
    fieldGroupId?: string;
}

const buildOpenErrorKey = (error: OpenErrorIdentity): string => {
    const ownedByOneField = error.scope === "field";

    return [
        error.scope,
        error.errorCode,
        error.severity,
        ownedByOneField ? error.fieldId ?? "" : "",
        ownedByOneField ? "" : error.fieldGroupId ?? ""
    ].join("|");
};

/**
 * Wählt bei mehreren verletzten Regeln eines Feldes den offenen Fehler aus.
 * Zuerst entscheidet der Schweregrad, bei Gleichstand die constraintId.
 * Die Auswahl ist damit unabhängig von der Reihenfolge der Validierungsprüfungen.
 */

interface BrokenRuleAtOneField {
    constraintId: string;
    severity: string;
}

const openRuleSeverityRank = (severity: string): number => {
    switch (severity) {
        case "error":
            return 3;
        case "warning":
            return 2;
        case "info":
            return 1;
        default:
            return 0;
    }
};

const selectOpenRuleAtField = <TBrokenRule extends BrokenRuleAtOneField>(
    brokenRules: readonly TBrokenRule[]
): TBrokenRule | null => {
    let open: TBrokenRule | null = null;
    let openRank = -1;

    for (const brokenRule of brokenRules) {
        const rank = openRuleSeverityRank(brokenRule.severity);
        if (open === null
            || rank > openRank
            || (rank === openRank && brokenRule.constraintId < open.constraintId)) {
            open = brokenRule;
            openRank = rank;
        }
    }

    return open;
};
