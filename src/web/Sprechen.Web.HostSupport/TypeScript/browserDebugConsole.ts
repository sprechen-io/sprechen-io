/** Kennungen und Betriebsmodus der aktuellen Diagnosesitzung. */
type DiagnosticSessionState = {
    enabled: boolean;
    sessionId: string | null;
    clientCorrelationId: string | null;
    operatingMode: string;
};

/** Verwaltet Konsolenausgabe, Diagnosezulassung und Zuordnung zur Diagnosesitzung. */
type BrowserDebugConsoleApi = {
    getEnabled(): boolean;
    getDiagnosticsAdmitted(): boolean;
    setEnabled(value: unknown): void;
    setDiagnosticSession(value: unknown): void;
    getDiagnosticSession(): DiagnosticSessionState;
    write(scope: unknown, message: unknown): void;
};

/** Stellt die Diagnosefunktionen für klassische Skripte auf window bereit. */
interface Window {
    browserDebugConsole: BrowserDebugConsoleApi;
}

((): void => {
    "use strict";

    const EnabledStorageKey = "sprechenIo.browserDebug.enabled";

    const inactiveSession = (): DiagnosticSessionState => ({
        enabled: false,
        sessionId: null,
        clientCorrelationId: null,
        operatingMode: "standard",
    });

    let diagnosticSession: DiagnosticSessionState = inactiveSession();

    const normalizeBoolean = (value: unknown): boolean => value === true;

    const normalizeText = (value: unknown): string | null => {
        if (typeof value !== "string") {
            return null;
        }

        const normalized = value.trim();
        return normalized.length === 0 ? null : normalized;
    };

    const tryReadEnabled = (): boolean => {
        try {
            const raw = window.localStorage.getItem(EnabledStorageKey);
            if (!raw) {
                return false;
            }

            return normalizeBoolean(JSON.parse(raw) as unknown);
        } catch {
            return false;
        }
    };

    // Im Diagnosemodus sind strukturierte Berichte zugelassen. Sonst sperrt nur der
    // gespeicherte Textwert "false" diese Berichte. Ein fehlender oder unlesbarer Wert erlaubt sie.
    const tryReadDiagnosticsAdmitted = (): boolean => {
        if (diagnosticSession.enabled) {
            return true;
        }

        try {
            return window.localStorage.getItem(EnabledStorageKey) !== "false";
        } catch {
            return true;
        }
    };

    const readDiagnosticSession = (value: unknown): DiagnosticSessionState => {
        if (!value || typeof value !== "object") {
            return inactiveSession();
        }

        const record = value as Record<string, unknown>;
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

    const tryWriteEnabled = (enabled: boolean): void => {
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
        } catch {
        }
    };

    window.browserDebugConsole = {
        getEnabled: (): boolean => tryReadEnabled(),
        getDiagnosticsAdmitted: (): boolean => tryReadDiagnosticsAdmitted(),
        setEnabled: (value: unknown): void => {
            tryWriteEnabled(normalizeBoolean(value));
        },
        setDiagnosticSession: (value: unknown): void => {
            diagnosticSession = readDiagnosticSession(value);
        },
        getDiagnosticSession: (): DiagnosticSessionState => diagnosticSession,
        write: (scope: unknown, message: unknown): void => {
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
