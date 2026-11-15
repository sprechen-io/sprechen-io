/** Verwaltet lokale Schwellenwerte für Sprechpausen und Sprachlautstärke im Browser. */
type SpeechCaptureSettingsApi = {
    getSilenceDebounceMs(): number;
    setSilenceDebounceMs(value: unknown): void;
    clearSilenceDebounceMs(): void;
    getSpeechAmplitudeThreshold(): number;
    setSpeechAmplitudeThreshold(value: unknown): void;
    clearSpeechAmplitudeThreshold(): void;
};

/** Stellt die lokalen Erfassungseinstellungen für klassische Skripte auf window bereit. */
interface Window {
    speechCaptureSettings: SpeechCaptureSettingsApi;
}

((): void => {
    "use strict";

    const SilenceDebounceStorageKey = "sprechenIo.speechCapture.silenceDebounceMs";
    const SpeechAmplitudeThresholdStorageKey = "sprechenIo.speechCapture.speechAmplitudeThreshold";

    const SilenceDebounceMsMin = 500;
    const SilenceDebounceMsMax = 6000;
    const SpeechAmplitudeThresholdMin = 50;
    const SpeechAmplitudeThresholdMax = 2000;

    const normalizeSilenceDebounceMs = (value: unknown): number => {
        if (typeof value !== "number" || !Number.isFinite(value)) {
            return 0;
        }

        const rounded = Math.round(value);
        if (rounded < SilenceDebounceMsMin || rounded > SilenceDebounceMsMax) {
            return 0;
        }

        return rounded;
    };

    const tryReadSilenceDebounceMs = (): number => {
        try {
            const raw = window.localStorage.getItem(SilenceDebounceStorageKey);
            if (!raw) {
                return 0;
            }

            const parsed = JSON.parse(raw) as unknown;
            return normalizeSilenceDebounceMs(parsed);
        } catch {
            return 0;
        }
    };

    const tryWriteSilenceDebounceMs = (value: number | null): void => {
        try {
            if (value === null) {
                window.localStorage.removeItem(SilenceDebounceStorageKey);
                return;
            }

            window.localStorage.setItem(SilenceDebounceStorageKey, JSON.stringify(value));
        } catch {
        }
    };

    const normalizeSpeechAmplitudeThreshold = (value: unknown): number => {
        if (typeof value !== "number" || !Number.isFinite(value)) {
            return 0;
        }

        const rounded = Math.round(value);
        if (rounded < SpeechAmplitudeThresholdMin || rounded > SpeechAmplitudeThresholdMax) {
            return 0;
        }

        return rounded;
    };

    const tryReadSpeechAmplitudeThreshold = (): number => {
        try {
            const raw = window.localStorage.getItem(SpeechAmplitudeThresholdStorageKey);
            if (!raw) {
                return 0;
            }

            const parsed = JSON.parse(raw) as unknown;
            return normalizeSpeechAmplitudeThreshold(parsed);
        } catch {
            return 0;
        }
    };

    const tryWriteSpeechAmplitudeThreshold = (value: number | null): void => {
        try {
            if (value === null) {
                window.localStorage.removeItem(SpeechAmplitudeThresholdStorageKey);
                return;
            }

            window.localStorage.setItem(SpeechAmplitudeThresholdStorageKey, JSON.stringify(value));
        } catch {
        }
    };

    window.speechCaptureSettings = {
        getSilenceDebounceMs: (): number => tryReadSilenceDebounceMs(),
        setSilenceDebounceMs: (value: unknown): void => {
            const normalized = normalizeSilenceDebounceMs(value);
            tryWriteSilenceDebounceMs(normalized);
        },
        clearSilenceDebounceMs: (): void => {
            tryWriteSilenceDebounceMs(null);
        },
        getSpeechAmplitudeThreshold: (): number => tryReadSpeechAmplitudeThreshold(),
        setSpeechAmplitudeThreshold: (value: unknown): void => {
            const normalized = normalizeSpeechAmplitudeThreshold(value);
            tryWriteSpeechAmplitudeThreshold(normalized);
        },
        clearSpeechAmplitudeThreshold: (): void => {
            tryWriteSpeechAmplitudeThreshold(null);
        }
    };
})();

