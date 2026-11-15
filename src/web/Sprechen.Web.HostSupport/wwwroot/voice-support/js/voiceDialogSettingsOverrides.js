"use strict";
(() => {
    "use strict";
    const DeviceAllowBargeInStorageKey = "sprechenIo.voiceDialogSettings.allowBargeIn.device";
    const ApplicationAllowBargeInStorageKey = "sprechenIo.voiceDialogSettings.allowBargeIn.application";
    const DeviceAutoAdvanceAfterCommitStorageKey = "sprechenIo.voiceDialogSettings.autoAdvanceAfterCommit.device";
    const ApplicationAutoAdvanceAfterCommitStorageKey = "sprechenIo.voiceDialogSettings.autoAdvanceAfterCommit.application";
    const normalizeNullableBoolean = (value) => {
        return typeof value === "boolean" ? value : null;
    };
    const normalizeApplicationId = (value) => {
        if (typeof value !== "string") {
            return null;
        }
        const normalized = value.trim().toLowerCase();
        return normalized.length === 0 ? null : normalized;
    };
    const normalizeApplicationOverrides = (value) => {
        if (typeof value !== "object" || value === null) {
            return {};
        }
        const overrides = {};
        for (const [rawApplicationId, rawValue] of Object.entries(value)) {
            const applicationId = normalizeApplicationId(rawApplicationId);
            const normalizedValue = normalizeNullableBoolean(rawValue);
            if (applicationId === null || normalizedValue === null) {
                continue;
            }
            overrides[applicationId] = normalizedValue;
        }
        return overrides;
    };
    const tryReadDeviceOverride = (storageKey) => {
        try {
            const raw = window.localStorage.getItem(storageKey);
            if (!raw) {
                return null;
            }
            return normalizeNullableBoolean(JSON.parse(raw));
        }
        catch {
            return null;
        }
    };
    const tryWriteDeviceOverride = (storageKey, value) => {
        try {
            if (value === null) {
                window.localStorage.removeItem(storageKey);
                return;
            }
            window.localStorage.setItem(storageKey, JSON.stringify(value));
        }
        catch {
        }
    };
    const tryReadApplicationOverrides = (storageKey) => {
        try {
            const raw = window.localStorage.getItem(storageKey);
            if (!raw) {
                return {};
            }
            return normalizeApplicationOverrides(JSON.parse(raw));
        }
        catch {
            return {};
        }
    };
    const tryWriteApplicationOverrides = (storageKey, value) => {
        try {
            if (Object.keys(value).length === 0) {
                window.localStorage.removeItem(storageKey);
                return;
            }
            window.localStorage.setItem(storageKey, JSON.stringify(value));
        }
        catch {
        }
    };
    const readApplicationOverride = (storageKey, applicationId) => {
        const normalizedApplicationId = normalizeApplicationId(applicationId);
        if (normalizedApplicationId === null) {
            return null;
        }
        const overrides = tryReadApplicationOverrides(storageKey);
        return Object.prototype.hasOwnProperty.call(overrides, normalizedApplicationId)
            ? overrides[normalizedApplicationId]
            : null;
    };
    const writeApplicationOverride = (storageKey, applicationId, value) => {
        const normalizedApplicationId = normalizeApplicationId(applicationId);
        if (normalizedApplicationId === null) {
            return;
        }
        const overrides = tryReadApplicationOverrides(storageKey);
        const normalizedValue = normalizeNullableBoolean(value);
        if (normalizedValue === null) {
            delete overrides[normalizedApplicationId];
        }
        else {
            overrides[normalizedApplicationId] = normalizedValue;
        }
        tryWriteApplicationOverrides(storageKey, overrides);
    };
    window.voiceDialogSettingsOverrides = {
        getDeviceAllowBargeInOverride: () => tryReadDeviceOverride(DeviceAllowBargeInStorageKey),
        setDeviceAllowBargeInOverride: (value) => {
            tryWriteDeviceOverride(DeviceAllowBargeInStorageKey, normalizeNullableBoolean(value));
        },
        getApplicationAllowBargeInOverrides: () => {
            return tryReadApplicationOverrides(ApplicationAllowBargeInStorageKey);
        },
        getApplicationAllowBargeInOverride: (applicationId) => {
            return readApplicationOverride(ApplicationAllowBargeInStorageKey, applicationId);
        },
        setApplicationAllowBargeInOverride: (applicationId, value) => {
            writeApplicationOverride(ApplicationAllowBargeInStorageKey, applicationId, value);
        },
        getDeviceAutoAdvanceAfterCommitOverride: () => tryReadDeviceOverride(DeviceAutoAdvanceAfterCommitStorageKey),
        setDeviceAutoAdvanceAfterCommitOverride: (value) => {
            tryWriteDeviceOverride(DeviceAutoAdvanceAfterCommitStorageKey, normalizeNullableBoolean(value));
        },
        getApplicationAutoAdvanceAfterCommitOverrides: () => {
            return tryReadApplicationOverrides(ApplicationAutoAdvanceAfterCommitStorageKey);
        },
        getApplicationAutoAdvanceAfterCommitOverride: (applicationId) => {
            return readApplicationOverride(ApplicationAutoAdvanceAfterCommitStorageKey, applicationId);
        },
        setApplicationAutoAdvanceAfterCommitOverride: (applicationId, value) => {
            writeApplicationOverride(ApplicationAutoAdvanceAfterCommitStorageKey, applicationId, value);
        }
    };
})();
