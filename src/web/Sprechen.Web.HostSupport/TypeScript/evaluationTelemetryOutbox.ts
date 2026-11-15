/** Speichert Evaluations-Checkpoints im localStorage und verwaltet exklusive Web-Locks-Sperren. */
type EvaluationTelemetryOutboxApi = {
    keys(storageKeyPrefix: string): string[];
    read(key: string): string | null;
    write(key: string, checkpoint: string): void;
    delete(key: string): void;
    tryAcquireLease(key: string): Promise<string | null>;
    releaseLease(leaseId: string): void;
    deliver(url: string, body: string, timeoutMs: number): Promise<EvaluationTelemetryBrowserDeliveryResult>;
};

/** HTTP-Status und optionale Wiederholwartezeit für den verwalteten Versand. */
type EvaluationTelemetryBrowserDeliveryResult = {
    status: number;
    retryAfterMs: number | null;
};

/** Stellt den Checkpointzugriff für klassische Skripte auf window bereit. */
interface Window {
    evaluationTelemetryOutbox: EvaluationTelemetryOutboxApi;
}

((): void => {
    "use strict";

    const leaseReleases = new Map<string, () => void>();

    const readRetryAfterMs = (response: Response): number | null => {
        const header = response.headers.get("Retry-After");
        if (!header) {
            return null;
        }

        const seconds = Number(header);
        if (Number.isFinite(seconds)) {
            return Math.max(0, seconds * 1000);
        }

        const date = Date.parse(header);
        if (Number.isNaN(date)) {
            return null;
        }

        return Math.max(0, date - Date.now());
    };

    window.evaluationTelemetryOutbox = {
        keys(storageKeyPrefix: string): string[] {
            if (!storageKeyPrefix) {
                throw new Error("The evaluation checkpoint storage prefix is required.");
            }

            const keys: string[] = [];
            for (let index = 0; index < window.localStorage.length; index += 1) {
                const key = window.localStorage.key(index);
                if (key?.startsWith(storageKeyPrefix)) {
                    keys.push(key);
                }
            }

            return keys.sort();
        },

        read(key: string): string | null {
            return window.localStorage.getItem(key);
        },

        write(key: string, checkpoint: string): void {
            window.localStorage.setItem(key, checkpoint);
        },

        delete(key: string): void {
            window.localStorage.removeItem(key);
        },

        async tryAcquireLease(key: string): Promise<string | null> {
            if (!navigator.locks) {
                throw new Error("The Web Locks API is required for evaluation checkpoint ownership.");
            }

            const leaseId = crypto.randomUUID();
            return await new Promise<string | null>((acquired, rejected) => {
                navigator.locks.request(
                    `sprechen-evaluation-lease:${key}`,
                    { mode: "exclusive", ifAvailable: true },
                    async (lock): Promise<void> => {
                        if (!lock) {
                            acquired(null);
                            return;
                        }

                        await new Promise<void>(release => {
                            leaseReleases.set(leaseId, release);
                            acquired(leaseId);
                        });
                    }).catch(rejected);
            });
        },

        releaseLease(leaseId: string): void {
            const release = leaseReleases.get(leaseId);
            if (!release) {
                throw new Error("Evaluation checkpoint lease is not owned by this tab.");
            }

            leaseReleases.delete(leaseId);
            release();
        },

        async deliver(url: string, body: string, timeoutMs: number): Promise<EvaluationTelemetryBrowserDeliveryResult> {
            if (typeof url !== "string" || url.length === 0) {
                throw new Error("The evaluation delivery URL is required.");
            }

            if (typeof body !== "string" || body.length === 0) {
                throw new Error("The evaluation delivery body is required.");
            }

            if (!Number.isFinite(timeoutMs) || timeoutMs < 1) {
                throw new Error("The evaluation delivery timeout is required.");
            }

            const controller = new AbortController();
            const timer = window.setTimeout(() => controller.abort(), timeoutMs);
            try {
                const response = await fetch(url, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body,
                    cache: "no-store",
                    redirect: "error",
                    signal: controller.signal
                });
                return {
                    status: response.status,
                    retryAfterMs: readRetryAfterMs(response)
                };
            } catch (reason) {
                if (typeof console !== "undefined" && typeof console.warn === "function") {
                    console.warn("evaluationTelemetryOutbox: delivery not acknowledged", reason);
                }

                throw reason;
            } finally {
                window.clearTimeout(timer);
            }
        },
    };
})();
