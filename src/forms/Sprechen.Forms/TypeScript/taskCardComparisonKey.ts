/**
 * Bildet den parameterfreien Vergleichsschlüssel einer Freitextvorgabe.
 * Die Schritte sind fest und identisch zur Sprachlaufzeit.
 */
// var macht die Funktion für andere klassische Skripte im globalen Geltungsbereich verfügbar.
// eslint-disable-next-line no-var
var taskCardComparisonKey = (value: string): string => {
    const nfc = value.trim().normalize("NFC");
    let withoutMarks = "";
    for (const character of nfc) {
        const code = character.codePointAt(0);
        if (code === undefined) {
            continue;
        }

        const category = unicodeCategory(code);
        if (category === "Cc" || category === "Cf" || category === "Mn") {
            continue;
        }

        withoutMarks += character;
    }

    const folded = withoutMarks.toLowerCase().replace(/ß/g, "ss");
    const withoutEdgePunctuation = stripEdgePunctuation(folded);
    const dashesAsSpace = replaceDashesWithSpace(withoutEdgePunctuation);
    return deleteWhitespace(dashesAsSpace);
};

const unicodeCategory = (code: number): string => {
    const character = String.fromCodePoint(code);
    if (/\p{Cc}/u.test(character)) {
        return "Cc";
    }

    if (/\p{Cf}/u.test(character)) {
        return "Cf";
    }

    if (/\p{Mn}/u.test(character)) {
        return "Mn";
    }

    if (/\p{Pd}/u.test(character)) {
        return "Pd";
    }

    if (/\p{P}/u.test(character)) {
        return "P";
    }

    return "";
};

const stripEdgePunctuation = (value: string): string => {
    let start = 0;
    let end = value.length;
    while (start < end && /\p{P}/u.test(value[start])) {
        start += 1;
    }

    while (end > start && /\p{P}/u.test(value[end - 1])) {
        end -= 1;
    }

    return value.slice(start, end);
};

const replaceDashesWithSpace = (value: string): string => {
    let result = "";
    for (const character of value) {
        result += character === "\u2212" || /\p{Pd}/u.test(character) ? " " : character;
    }

    return result;
};

const deleteWhitespace = (value: string): string => value.replace(/[\s\u00A0]/g, "");
