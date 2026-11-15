"use strict";
(() => {
    "use strict";
    const StorageKey = "sprechenIo.voice.sessionSetup.userLanguage";
    const normalizeNullableString = (value) => {
        if (typeof value !== "string") {
            return null;
        }
        const normalized = value.trim();
        return normalized.length === 0 ? null : normalized;
    };
    const normalizeNullableIsoCode = (value) => {
        const normalized = normalizeNullableString(value);
        if (normalized === null) {
            return null;
        }
        const dashIndex = normalized.indexOf("-");
        const baseLanguage = (dashIndex > 0 ? normalized.slice(0, dashIndex) : normalized).toLowerCase();
        return /^[a-z]{2}$/.test(baseLanguage)
            ? baseLanguage
            : null;
    };
    const normalizeNullablePolitenessLevel = (value) => {
        return value === "formal" || value === "informal"
            ? value
            : null;
    };
    const normalizePreferredLanguageTags = (value) => {
        if (!Array.isArray(value)) {
            return [];
        }
        const tags = [];
        const seen = new Set();
        for (const candidate of value) {
            const normalized = normalizeNullableString(candidate);
            if (normalized === null) {
                continue;
            }
            const dedupeKey = normalized.toLowerCase();
            if (seen.has(dedupeKey)) {
                continue;
            }
            seen.add(dedupeKey);
            tags.push(normalized);
        }
        return tags;
    };
    const normalizeSettings = (value) => {
        if (typeof value !== "object" || value === null) {
            return {
                selectedIsoCode: null,
                selectedPolitenessLevel: null,
                entryOfferHeardIsoCode: null
            };
        }
        const raw = value;
        return {
            selectedIsoCode: normalizeNullableIsoCode(raw.selectedIsoCode),
            selectedPolitenessLevel: normalizeNullablePolitenessLevel(raw.selectedPolitenessLevel),
            entryOfferHeardIsoCode: normalizeNullableIsoCode(raw.entryOfferHeardIsoCode)
        };
    };
    const resolveDocumentLanguage = (selectedIsoCode) => {
        return selectedIsoCode
            ?? normalizeNullableIsoCode(document.documentElement.lang)
            ?? "de";
    };
    const applyDocumentLanguage = (selectedIsoCode) => {
        document.documentElement.lang = resolveDocumentLanguage(selectedIsoCode);
    };
    const tryRead = () => {
        try {
            const raw = window.localStorage.getItem(StorageKey);
            if (!raw) {
                return normalizeSettings(null);
            }
            return normalizeSettings(JSON.parse(raw));
        }
        catch {
            return normalizeSettings(null);
        }
    };
    const tryWrite = (value) => {
        try {
            // Der Eintrag wird erst gelöscht, wenn auch der Einführungsmarker leer ist.
            // So bleibt die bereits ausgegebene Einführung unabhängig von einer Sprachwahl vermerkt.
            if (value.selectedIsoCode === null
                && value.selectedPolitenessLevel === null
                && value.entryOfferHeardIsoCode === null) {
                window.localStorage.removeItem(StorageKey);
                return;
            }
            window.localStorage.setItem(StorageKey, JSON.stringify(value));
        }
        catch {
        }
    };
    /**
     * Ergänzt den Einstiegs-Marker, ohne die übrigen gespeicherten Einstellungen zu ersetzen.
     * Lesen und Schreiben liegen im selben try: Schlägt das Lesen oder Parsen fehl,
     * wird nicht mit einem leeren Ersatzdatensatz weitergeschrieben. Die Zusammenführung
     * bleibt im Browser, damit .NET keinen zuvor gelesenen Ersatzwert zurückschreibt.
     */
    const trySetEntryOfferHeard = (isoCode) => {
        const normalized = normalizeNullableIsoCode(isoCode);
        if (normalized === null) {
            return;
        }
        try {
            const raw = window.localStorage.getItem(StorageKey);
            const current = raw ? normalizeSettings(JSON.parse(raw)) : normalizeSettings(null);
            window.localStorage.setItem(StorageKey, JSON.stringify({ ...current, entryOfferHeardIsoCode: normalized }));
        }
        catch {
        }
    };
    const getPreferredLanguageTags = () => {
        try {
            const preferredTags = normalizePreferredLanguageTags(navigator.languages);
            if (preferredTags.length > 0) {
                return preferredTags;
            }
            const fallbackTag = normalizeNullableString(navigator.language);
            return fallbackTag === null
                ? []
                : [fallbackTag];
        }
        catch {
            return [];
        }
    };
    applyDocumentLanguage(tryRead().selectedIsoCode);
    window.userLanguageSettings = {
        get: () => tryRead(),
        set: (value) => {
            const settings = normalizeSettings(value);
            tryWrite(settings);
            applyDocumentLanguage(settings.selectedIsoCode);
        },
        setEntryOfferHeard: (isoCode) => trySetEntryOfferHeard(isoCode),
        getPreferredLanguageTags: () => getPreferredLanguageTags()
    };
})();
