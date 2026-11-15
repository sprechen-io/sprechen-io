/** Verwaltet lokal gespeicherte Aktivierungswörter unabhängig vom gemeinsamen Katalog. */
type VoiceActivationSettingsApi = {
    getWakeWordActivationFormsOverride(): string[];
    setWakeWordActivationFormsOverride(value: unknown): void;
    clearWakeWordActivationFormsOverride(): void;
};

/** Stellt die lokalen Aktivierungswörter für klassische Skripte auf window bereit. */
interface Window {
    voiceActivationSettings: VoiceActivationSettingsApi;
}

((): void => {
    "use strict";

    const WakeWordActivationFormsStorageKey = "sprechenIo.voiceActivation.wakeWordActivationForms";

    const normalizeWord = (value: string): string => value.trim();

    const normalizeWakeWordActivationForms = (raw: unknown): string[] => {
        if (!Array.isArray(raw)) {
            return [];
        }

        const seen = new Set<string>();
        const result: string[] = [];

        for (const entry of raw) {
            if (typeof entry !== "string") {
                continue;
            }

            const normalized = normalizeWord(entry);
            if (normalized.length === 0) {
                continue;
            }

            const key = normalized.toLowerCase();
            if (seen.has(key)) {
                continue;
            }

            seen.add(key);
            result.push(normalized);
        }

        return result;
    };

    const tryReadWakeWordActivationForms = (): string[] => {
        try {
            const raw = window.localStorage.getItem(WakeWordActivationFormsStorageKey);
            if (!raw) {
                return [];
            }

            const parsed = JSON.parse(raw) as unknown;
            return normalizeWakeWordActivationForms(parsed);
        } catch {
            return [];
        }
    };

    const tryWriteWakeWordActivationForms = (activationForms: string[]): void => {
        try {
            if (activationForms.length === 0) {
                window.localStorage.removeItem(WakeWordActivationFormsStorageKey);
                return;
            }

            window.localStorage.setItem(WakeWordActivationFormsStorageKey, JSON.stringify(activationForms));
        } catch {
        }
    };

    window.voiceActivationSettings = {
        getWakeWordActivationFormsOverride: (): string[] => tryReadWakeWordActivationForms(),
        setWakeWordActivationFormsOverride: (value: unknown): void => {
            const activationForms = normalizeWakeWordActivationForms(value);
            tryWriteWakeWordActivationForms(activationForms);
        },
        clearWakeWordActivationFormsOverride: (): void => {
            tryWriteWakeWordActivationForms([]);
        }
    };
})();
