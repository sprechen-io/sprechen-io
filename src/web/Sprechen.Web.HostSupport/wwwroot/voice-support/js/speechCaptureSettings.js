"use strict";
(() => {
    "use strict";
    const SilenceDebounceStorageKey = "sprechenIo.speechCapture.silenceDebounceMs";
    const SpeechAmplitudeThresholdStorageKey = "sprechenIo.speechCapture.speechAmplitudeThreshold";
    const SilenceDebounceMsMin = 500;
    const SilenceDebounceMsMax = 6000;
    const SpeechAmplitudeThresholdMin = 50;
    const SpeechAmplitudeThresholdMax = 2000;
    const normalizeSilenceDebounceMs = (value) => {
        if (typeof value !== "number" || !Number.isFinite(value)) {
            return 0;
        }
        const rounded = Math.round(value);
        if (rounded < SilenceDebounceMsMin || rounded > SilenceDebounceMsMax) {
            return 0;
        }
        return rounded;
    };
    const tryReadSilenceDebounceMs = () => {
        try {
            const raw = window.localStorage.getItem(SilenceDebounceStorageKey);
            if (!raw) {
                return 0;
            }
            const parsed = JSON.parse(raw);
            return normalizeSilenceDebounceMs(parsed);
        }
        catch {
            return 0;
        }
    };
    const tryWriteSilenceDebounceMs = (value) => {
        try {
            if (value === null) {
                window.localStorage.removeItem(SilenceDebounceStorageKey);
                return;
            }
            window.localStorage.setItem(SilenceDebounceStorageKey, JSON.stringify(value));
        }
        catch {
        }
    };
    const normalizeSpeechAmplitudeThreshold = (value) => {
        if (typeof value !== "number" || !Number.isFinite(value)) {
            return 0;
        }
        const rounded = Math.round(value);
        if (rounded < SpeechAmplitudeThresholdMin || rounded > SpeechAmplitudeThresholdMax) {
            return 0;
        }
        return rounded;
    };
    const tryReadSpeechAmplitudeThreshold = () => {
        try {
            const raw = window.localStorage.getItem(SpeechAmplitudeThresholdStorageKey);
            if (!raw) {
                return 0;
            }
            const parsed = JSON.parse(raw);
            return normalizeSpeechAmplitudeThreshold(parsed);
        }
        catch {
            return 0;
        }
    };
    const tryWriteSpeechAmplitudeThreshold = (value) => {
        try {
            if (value === null) {
                window.localStorage.removeItem(SpeechAmplitudeThresholdStorageKey);
                return;
            }
            window.localStorage.setItem(SpeechAmplitudeThresholdStorageKey, JSON.stringify(value));
        }
        catch {
        }
    };
    window.speechCaptureSettings = {
        getSilenceDebounceMs: () => tryReadSilenceDebounceMs(),
        setSilenceDebounceMs: (value) => {
            const normalized = normalizeSilenceDebounceMs(value);
            tryWriteSilenceDebounceMs(normalized);
        },
        clearSilenceDebounceMs: () => {
            tryWriteSilenceDebounceMs(null);
        },
        getSpeechAmplitudeThreshold: () => tryReadSpeechAmplitudeThreshold(),
        setSpeechAmplitudeThreshold: (value) => {
            const normalized = normalizeSpeechAmplitudeThreshold(value);
            tryWriteSpeechAmplitudeThreshold(normalized);
        },
        clearSpeechAmplitudeThreshold: () => {
            tryWriteSpeechAmplitudeThreshold(null);
        }
    };
})();
