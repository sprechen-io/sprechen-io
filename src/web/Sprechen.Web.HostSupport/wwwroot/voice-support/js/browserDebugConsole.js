"use strict";
(() => {
    "use strict";
    const EnabledStorageKey = "sprechenIo.browserDebug.enabled";
    const inactiveSession = () => ({
        enabled: false,
        sessionId: null,
        clientCorrelationId: null,
        operatingMode: "standard",
    });
    let diagnosticSession = inactiveSession();
    const normalizeBoolean = (value) => value === true;
    const normalizeText = (value) => {
        if (typeof value !== "string") {
            return null;
        }
        const normalized = value.trim();
        return normalized.length === 0 ? null : normalized;
    };
    const tryReadEnabled = () => {
        try {
            const raw = window.localStorage.getItem(EnabledStorageKey);
            if (!raw) {
                return false;
            }
            return normalizeBoolean(JSON.parse(raw));
        }
        catch {
            return false;
        }
    };
    // Im Diagnosemodus sind strukturierte Berichte zugelassen. Sonst sperrt nur der
    // gespeicherte Textwert "false" diese Berichte. Ein fehlender oder unlesbarer Wert erlaubt sie.
    const tryReadDiagnosticsAdmitted = () => {
        if (diagnosticSession.enabled) {
            return true;
        }
        try {
            return window.localStorage.getItem(EnabledStorageKey) !== "false";
        }
        catch {
            return true;
        }
    };
    const readDiagnosticSession = (value) => {
        if (!value || typeof value !== "object") {
            return inactiveSession();
        }
        const record = value;
        const enabled = record.enabled === true;
        if (!enabled) {
            return inactiveSession();
        }
        return {
            enabled: true,
            sessionId: normalizeText(record.sessionId),
            clientCorrelationId: normalizeText(record.clientCorrelationId),
            operatingMode: normalizeText(record.operatingMode) ?? "diagnostic",
        };
    };
    const tryWriteEnabled = (enabled) => {
        try {
            if (!enabled) {
                window.localStorage.removeItem(EnabledStorageKey);
                // eslint-disable-next-line no-console
                console.info("[sprechen-debug] browser debug logging disabled");
                return;
            }
            window.localStorage.setItem(EnabledStorageKey, JSON.stringify(true));
            // eslint-disable-next-line no-console
            console.info("[sprechen-debug] browser debug logging enabled");
        }
        catch {
        }
    };
    window.browserDebugConsole = {
        getEnabled: () => tryReadEnabled(),
        getDiagnosticsAdmitted: () => tryReadDiagnosticsAdmitted(),
        setEnabled: (value) => {
            tryWriteEnabled(normalizeBoolean(value));
        },
        setDiagnosticSession: (value) => {
            diagnosticSession = readDiagnosticSession(value);
        },
        getDiagnosticSession: () => diagnosticSession,
        write: (scope, message) => {
            if (!diagnosticSession.enabled && !tryReadEnabled()) {
                return;
            }
            const normalizedScope = normalizeText(scope) ?? "voice";
            const normalizedMessage = normalizeText(message);
            if (normalizedMessage === null) {
                return;
            }
            const timestamp = new Date().toISOString();
            // eslint-disable-next-line no-console
            console.info(`[sprechen-debug][${timestamp}][${normalizedScope}] ${normalizedMessage}`);
        }
    };
})();
