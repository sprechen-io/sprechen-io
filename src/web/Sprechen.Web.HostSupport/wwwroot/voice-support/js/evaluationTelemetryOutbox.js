"use strict";
(() => {
    "use strict";
    const leaseReleases = new Map();
    const readRetryAfterMs = (response) => {
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
        keys(storageKeyPrefix) {
            if (!storageKeyPrefix) {
                throw new Error("The evaluation checkpoint storage prefix is required.");
            }
            const keys = [];
            for (let index = 0; index < window.localStorage.length; index += 1) {
                const key = window.localStorage.key(index);
                if (key?.startsWith(storageKeyPrefix)) {
                    keys.push(key);
                }
            }
            return keys.sort();
        },
        read(key) {
            return window.localStorage.getItem(key);
        },
        write(key, checkpoint) {
            window.localStorage.setItem(key, checkpoint);
        },
        delete(key) {
            window.localStorage.removeItem(key);
        },
        async tryAcquireLease(key) {
            if (!navigator.locks) {
                throw new Error("The Web Locks API is required for evaluation checkpoint ownership.");
            }
            const leaseId = crypto.randomUUID();
            return await new Promise((acquired, rejected) => {
                navigator.locks.request(`sprechen-evaluation-lease:${key}`, { mode: "exclusive", ifAvailable: true }, async (lock) => {
                    if (!lock) {
                        acquired(null);
                        return;
                    }
                    await new Promise(release => {
                        leaseReleases.set(leaseId, release);
                        acquired(leaseId);
                    });
                }).catch(rejected);
            });
        },
        releaseLease(leaseId) {
            const release = leaseReleases.get(leaseId);
            if (!release) {
                throw new Error("Evaluation checkpoint lease is not owned by this tab.");
            }
            leaseReleases.delete(leaseId);
            release();
        },
        async deliver(url, body, timeoutMs) {
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
            }
            catch (reason) {
                if (typeof console !== "undefined" && typeof console.warn === "function") {
                    console.warn("evaluationTelemetryOutbox: delivery not acknowledged", reason);
                }
                throw reason;
            }
            finally {
                window.clearTimeout(timer);
            }
        },
    };
})();
