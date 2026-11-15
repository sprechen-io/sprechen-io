/**
 * Gespeicherte Sprachwahl der Sitzung, getrennt vom gehörten Einstiegsangebot,
 * damit Schweigen keine Auswahl vortäuscht.
 */
type UserLanguageSettings = {
    selectedIsoCode: string | null;
    selectedPolitenessLevel: string | null;
    // Sprache, in der die Einführung samt den anderen Sprachnamen bereits ausgegeben wurde.
    // Der Marker bleibt von einer aktiv gespeicherten Sprachwahl getrennt.
    entryOfferHeardIsoCode: string | null;
};

/** Verwaltet Sprachwahl, Anredeform, Einführungsmarker und bevorzugte Browsersprachen. */
type UserLanguageSettingsApi = {
    get(): UserLanguageSettings;
    set(value: unknown): void;
    setEntryOfferHeard(isoCode: unknown): void;
    getPreferredLanguageTags(): string[];
};

/** Stellt die Spracheinstellungen für klassische Skripte auf window bereit. */
interface Window {
    userLanguageSettings: UserLanguageSettingsApi;
}

((): void => {
    "use strict";

    const StorageKey = "sprechenIo.voice.sessionSetup.userLanguage";

    const normalizeNullableString = (value: unknown): string | null => {
        if (typeof value !== "string") {
            return null;
        }

        const normalized = value.trim();
        return normalized.length === 0 ? null : normalized;
    };

    const normalizeNullableIsoCode = (value: unknown): string | null => {
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

    const normalizeNullablePolitenessLevel = (value: unknown): string | null => {
        return value === "formal" || value === "informal"
            ? value
            : null;
    };

    const normalizePreferredLanguageTags = (value: unknown): string[] => {
        if (!Array.isArray(value)) {
            return [];
        }

        const tags: string[] = [];
        const seen = new Set<string>();
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

    const normalizeSettings = (value: unknown): UserLanguageSettings => {
        if (typeof value !== "object" || value === null) {
            return {
                selectedIsoCode: null,
                selectedPolitenessLevel: null,
                entryOfferHeardIsoCode: null
            };
        }

        const raw = value as Record<string, unknown>;
        return {
            selectedIsoCode: normalizeNullableIsoCode(raw.selectedIsoCode),
            selectedPolitenessLevel: normalizeNullablePolitenessLevel(raw.selectedPolitenessLevel),
            entryOfferHeardIsoCode: normalizeNullableIsoCode(raw.entryOfferHeardIsoCode)
        };
    };

    const resolveDocumentLanguage = (selectedIsoCode: string | null): string => {
        return selectedIsoCode
            ?? normalizeNullableIsoCode(document.documentElement.lang)
            ?? "de";
    };

    const applyDocumentLanguage = (selectedIsoCode: string | null): void => {
        document.documentElement.lang = resolveDocumentLanguage(selectedIsoCode);
    };

    const tryRead = (): UserLanguageSettings => {
        try {
            const raw = window.localStorage.getItem(StorageKey);
            if (!raw) {
                return normalizeSettings(null);
            }

            return normalizeSettings(JSON.parse(raw) as unknown);
        } catch {
            return normalizeSettings(null);
        }
    };

    const tryWrite = (value: UserLanguageSettings): void => {
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
        } catch {
        }
    };

    /**
     * Ergänzt den Einstiegs-Marker, ohne die übrigen gespeicherten Einstellungen zu ersetzen.
     * Lesen und Schreiben liegen im selben try: Schlägt das Lesen oder Parsen fehl,
     * wird nicht mit einem leeren Ersatzdatensatz weitergeschrieben. Die Zusammenführung
     * bleibt im Browser, damit .NET keinen zuvor gelesenen Ersatzwert zurückschreibt.
     */
    const trySetEntryOfferHeard = (isoCode: unknown): void => {
        const normalized = normalizeNullableIsoCode(isoCode);
        if (normalized === null) {
            return;
        }

        try {
            const raw = window.localStorage.getItem(StorageKey);
            const current = raw ? normalizeSettings(JSON.parse(raw) as unknown) : normalizeSettings(null);
            window.localStorage.setItem(
                StorageKey,
                JSON.stringify({ ...current, entryOfferHeardIsoCode: normalized }));
        } catch {
        }
    };

    const getPreferredLanguageTags = (): string[] => {
        try {
            const preferredTags = normalizePreferredLanguageTags(navigator.languages);
            if (preferredTags.length > 0) {
                return preferredTags;
            }

            const fallbackTag = normalizeNullableString(navigator.language);
            return fallbackTag === null
                ? []
                : [fallbackTag];
        } catch {
            return [];
        }
    };

    applyDocumentLanguage(tryRead().selectedIsoCode);

    window.userLanguageSettings = {
        get: (): UserLanguageSettings => tryRead(),
        set: (value: unknown): void => {
            const settings = normalizeSettings(value);
            tryWrite(settings);
            applyDocumentLanguage(settings.selectedIsoCode);
        },
        setEntryOfferHeard: (isoCode: unknown): void => trySetEntryOfferHeard(isoCode),
        getPreferredLanguageTags: (): string[] => getPreferredLanguageTags()
    };
})();
