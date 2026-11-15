/** Lokale Einstellungen für Barge-in und automatisches Weiterschalten, geräteweit oder je Anwendung. */
type VoiceDialogSettingsOverridesApi = {
    getDeviceAllowBargeInOverride(): boolean | null;
    setDeviceAllowBargeInOverride(value: unknown): void;
    getApplicationAllowBargeInOverrides(): Record<string, boolean>;
    getApplicationAllowBargeInOverride(applicationId: unknown): boolean | null;
    setApplicationAllowBargeInOverride(applicationId: unknown, value: unknown): void;
    getDeviceAutoAdvanceAfterCommitOverride(): boolean | null;
    setDeviceAutoAdvanceAfterCommitOverride(value: unknown): void;
    getApplicationAutoAdvanceAfterCommitOverrides(): Record<string, boolean>;
    getApplicationAutoAdvanceAfterCommitOverride(applicationId: unknown): boolean | null;
    setApplicationAutoAdvanceAfterCommitOverride(applicationId: unknown, value: unknown): void;
};

/** Stellt die lokalen Dialogeinstellungen für klassische Skripte auf window bereit. */
interface Window {
    voiceDialogSettingsOverrides: VoiceDialogSettingsOverridesApi;
}

((): void => {
    "use strict";

    const DeviceAllowBargeInStorageKey = "sprechenIo.voiceDialogSettings.allowBargeIn.device";
    const ApplicationAllowBargeInStorageKey = "sprechenIo.voiceDialogSettings.allowBargeIn.application";
    const DeviceAutoAdvanceAfterCommitStorageKey = "sprechenIo.voiceDialogSettings.autoAdvanceAfterCommit.device";
    const ApplicationAutoAdvanceAfterCommitStorageKey = "sprechenIo.voiceDialogSettings.autoAdvanceAfterCommit.application";

    const normalizeNullableBoolean = (value: unknown): boolean | null => {
        return typeof value === "boolean" ? value : null;
    };

    const normalizeApplicationId = (value: unknown): string | null => {
        if (typeof value !== "string") {
            return null;
        }

        const normalized = value.trim().toLowerCase();
        return normalized.length === 0 ? null : normalized;
    };

    const normalizeApplicationOverrides = (value: unknown): Record<string, boolean> => {
        if (typeof value !== "object" || value === null) {
            return {};
        }

        const overrides: Record<string, boolean> = {};
        for (const [rawApplicationId, rawValue] of Object.entries(value as Record<string, unknown>)) {
            const applicationId = normalizeApplicationId(rawApplicationId);
            const normalizedValue = normalizeNullableBoolean(rawValue);
            if (applicationId === null || normalizedValue === null) {
                continue;
            }

            overrides[applicationId] = normalizedValue;
        }

        return overrides;
    };

    const tryReadDeviceOverride = (storageKey: string): boolean | null => {
        try {
            const raw = window.localStorage.getItem(storageKey);
            if (!raw) {
                return null;
            }

            return normalizeNullableBoolean(JSON.parse(raw) as unknown);
        } catch {
            return null;
        }
    };

    const tryWriteDeviceOverride = (storageKey: string, value: boolean | null): void => {
        try {
            if (value === null) {
                window.localStorage.removeItem(storageKey);
                return;
            }

            window.localStorage.setItem(storageKey, JSON.stringify(value));
        } catch {
        }
    };

    const tryReadApplicationOverrides = (storageKey: string): Record<string, boolean> => {
        try {
            const raw = window.localStorage.getItem(storageKey);
            if (!raw) {
                return {};
            }

            return normalizeApplicationOverrides(JSON.parse(raw) as unknown);
        } catch {
            return {};
        }
    };

    const tryWriteApplicationOverrides = (storageKey: string, value: Record<string, boolean>): void => {
        try {
            if (Object.keys(value).length === 0) {
                window.localStorage.removeItem(storageKey);
                return;
            }

            window.localStorage.setItem(storageKey, JSON.stringify(value));
        } catch {
        }
    };

    const readApplicationOverride = (storageKey: string, applicationId: unknown): boolean | null => {
        const normalizedApplicationId = normalizeApplicationId(applicationId);
        if (normalizedApplicationId === null) {
            return null;
        }

        const overrides = tryReadApplicationOverrides(storageKey);
        return Object.prototype.hasOwnProperty.call(overrides, normalizedApplicationId)
            ? overrides[normalizedApplicationId]
            : null;
    };

    const writeApplicationOverride = (storageKey: string, applicationId: unknown, value: unknown): void => {
        const normalizedApplicationId = normalizeApplicationId(applicationId);
        if (normalizedApplicationId === null) {
            return;
        }

        const overrides = tryReadApplicationOverrides(storageKey);
        const normalizedValue = normalizeNullableBoolean(value);
        if (normalizedValue === null) {
            delete overrides[normalizedApplicationId];
        } else {
            overrides[normalizedApplicationId] = normalizedValue;
        }

        tryWriteApplicationOverrides(storageKey, overrides);
    };

    window.voiceDialogSettingsOverrides = {
        getDeviceAllowBargeInOverride: (): boolean | null => tryReadDeviceOverride(DeviceAllowBargeInStorageKey),
        setDeviceAllowBargeInOverride: (value: unknown): void => {
            tryWriteDeviceOverride(DeviceAllowBargeInStorageKey, normalizeNullableBoolean(value));
        },
        getApplicationAllowBargeInOverrides: (): Record<string, boolean> => {
            return tryReadApplicationOverrides(ApplicationAllowBargeInStorageKey);
        },
        getApplicationAllowBargeInOverride: (applicationId: unknown): boolean | null => {
            return readApplicationOverride(ApplicationAllowBargeInStorageKey, applicationId);
        },
        setApplicationAllowBargeInOverride: (applicationId: unknown, value: unknown): void => {
            writeApplicationOverride(ApplicationAllowBargeInStorageKey, applicationId, value);
        },
        getDeviceAutoAdvanceAfterCommitOverride: (): boolean | null =>
            tryReadDeviceOverride(DeviceAutoAdvanceAfterCommitStorageKey),
        setDeviceAutoAdvanceAfterCommitOverride: (value: unknown): void => {
            tryWriteDeviceOverride(DeviceAutoAdvanceAfterCommitStorageKey, normalizeNullableBoolean(value));
        },
        getApplicationAutoAdvanceAfterCommitOverrides: (): Record<string, boolean> => {
            return tryReadApplicationOverrides(ApplicationAutoAdvanceAfterCommitStorageKey);
        },
        getApplicationAutoAdvanceAfterCommitOverride: (applicationId: unknown): boolean | null => {
            return readApplicationOverride(ApplicationAutoAdvanceAfterCommitStorageKey, applicationId);
        },
        setApplicationAutoAdvanceAfterCommitOverride: (applicationId: unknown, value: unknown): void => {
            writeApplicationOverride(ApplicationAutoAdvanceAfterCommitStorageKey, applicationId, value);
        }
    };
})();
