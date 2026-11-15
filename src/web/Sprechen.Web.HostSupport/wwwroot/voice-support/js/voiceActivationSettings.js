"use strict";
(() => {
    "use strict";
    const WakeWordActivationFormsStorageKey = "sprechenIo.voiceActivation.wakeWordActivationForms";
    const normalizeWord = (value) => value.trim();
    const normalizeWakeWordActivationForms = (raw) => {
        if (!Array.isArray(raw)) {
            return [];
        }
        const seen = new Set();
        const result = [];
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
    const tryReadWakeWordActivationForms = () => {
        try {
            const raw = window.localStorage.getItem(WakeWordActivationFormsStorageKey);
            if (!raw) {
                return [];
            }
            const parsed = JSON.parse(raw);
            return normalizeWakeWordActivationForms(parsed);
        }
        catch {
            return [];
        }
    };
    const tryWriteWakeWordActivationForms = (activationForms) => {
        try {
            if (activationForms.length === 0) {
                window.localStorage.removeItem(WakeWordActivationFormsStorageKey);
                return;
            }
            window.localStorage.setItem(WakeWordActivationFormsStorageKey, JSON.stringify(activationForms));
        }
        catch {
        }
    };
    window.voiceActivationSettings = {
        getWakeWordActivationFormsOverride: () => tryReadWakeWordActivationForms(),
        setWakeWordActivationFormsOverride: (value) => {
            const activationForms = normalizeWakeWordActivationForms(value);
            tryWriteWakeWordActivationForms(activationForms);
        },
        clearWakeWordActivationFormsOverride: () => {
            tryWriteWakeWordActivationForms([]);
        }
    };
})();
