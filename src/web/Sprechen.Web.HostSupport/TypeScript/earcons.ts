/** Kennungen der verfügbaren akustischen Statussignale. */
type EarconId =
    | "FlowOutputStart"
    | "FlowListening"
    | "FlowAck"
    | "FlowProcessing"
    | "FlowBargeIn"
    | "FlowDone"
    | "ValidationSuccess"
    | "ValidationAsrFail"
    | "ValidationAmbiguous"
    | "StructureStepTransition"
    | "StructureBoundary"
    | "ModePrivacyChange"
    | "ModeConfirmationRequired"
    | "VoiceInputMicOn"
    | "VoiceInputMicOff"
    | "WakeWordNotRecognized";

/** Global verfügbare Schnittstelle zum Abspielen und Steuern der Earcons. */
type EarconApi = {
    play(id: EarconId): Promise<void>;
    preview(id: EarconId): Promise<void>;
    stopAll(): void;
    stopLooping(): void;
    warmup(): Promise<void>;
    getVolume(): number;
    setVolume(volume: unknown): void;
};

/** Globale Earcon-Schnittstelle und vom Host bereitgestellte Audioressourcen. */
interface Window {
    earcons: EarconApi;
    sprechenIoWeb?: {
        pcmRecorderWorkletUrl?: string;
        pcmPlaybackWorkletUrls?: string[];
        earconUrls?: Record<string, string>;
    };
}

((): void => {
    "use strict";

    const DefaultVolume = 1.0;
    const EarconFetchFailureCooldownMs = 30_000;
    // Wiederholtes Verarbeitungssignal spätestens nach dieser Frist stoppen,
    // falls der reguläre Abschluss oder der nächste Eingabeschritt ausbleibt.
    const FlowProcessingMaxDurationMs = 60_000;
    const HostSupportEarconRoutePrefix = "/_content/Sprechen.Web.HostSupport/voice-support/earcon/";

    const clamp = (value: number, min: number, max: number): number => Math.max(min, Math.min(max, value));

    /**
     * Konstruktor von AudioContext, weil Ponyfill und nativer Kontext nicht austauschbar sind.
     */
    type AudioContextCtor = new (options?: AudioContextOptions) => AudioContext;

    // Das Kompatibilitätspaket bevorzugen. Ohne dieses den nativen
    // oder älteren webkitAudioContext-Konstruktor verwenden.
    const resolveAudioContextCtor = (): AudioContextCtor | undefined => {
        const ponyfill = (window as unknown as { standardizedAudioContext?: { AudioContext?: AudioContextCtor } }).standardizedAudioContext;
        if (ponyfill && typeof ponyfill.AudioContext === "function") {
            return ponyfill.AudioContext;
        }

        return window.AudioContext || (window as unknown as { webkitAudioContext?: AudioContextCtor }).webkitAudioContext;
    };

    /** Kennzeichnet einen während der Wiederholungssperre ausgelassenen Ladeversuch. */
    type EarconCooldownSkipError = Error & { earconCooldownSkip: true };

    let audioCtx: AudioContext | null = null;
    let masterGainNode: GainNode | null = null;
    let volume = DefaultVolume;
    const buffersByUrl = new Map<string, Promise<AudioBuffer>>();
    const failedFetchCooldownsByUrl = new Map<string, number>();
    let stopGeneration = 0;
    let queue: { id: EarconId; url: string; generation: number; resolve?: () => void }[] = [];
    let isProcessingQueue = false;
    let activeSource: AudioBufferSourceNode | null = null;
    let processingSource: AudioBufferSourceNode | null = null;
    let processingUrl: string | null = null;
    let processingToken = 0;
    let pendingProcessing: { url: string; token: number; generation: number } | null = null;
    let processingWatchdogTimer: ReturnType<typeof setTimeout> | null = null;
    let warmupStarted = false;

    const clearProcessingWatchdog = (): void => {
        if (processingWatchdogTimer !== null) {
            clearTimeout(processingWatchdogTimer);
            processingWatchdogTimer = null;
        }
    };

    const completeQueueItem = (item: { resolve?: () => void }): void => {
        const resolve = item.resolve;
        if (!resolve) {
            return;
        }

        item.resolve = undefined;

        try {
            resolve();
        } catch {
            // Ein fehlerhafter Abschlusscallback darf die übrigen Warteschlangeneinträge nicht blockieren.
        }
    };

    const ensureMasterGain = (ctx: AudioContext): GainNode => {
        if (masterGainNode && masterGainNode.context === ctx) {
            return masterGainNode;
        }

        masterGainNode = ctx.createGain();
        masterGainNode.gain.value = volume;
        masterGainNode.connect(ctx.destination);

        return masterGainNode;
    };

    const ensureContextAsync = async (): Promise<AudioContext> => {
        if (audioCtx && audioCtx.state !== "closed") {
            if (audioCtx.state === "suspended") {
                await audioCtx.resume();
            }

            ensureMasterGain(audioCtx);
            return audioCtx;
        }

        const ctor = resolveAudioContextCtor();
        if (!ctor) {
            throw new Error("Web Audio API is not supported in this browser (missing AudioContext).");
        }

        audioCtx = new ctor({ latencyHint: "interactive" });
        if (audioCtx.state === "suspended") {
            await audioCtx.resume();
        }

        ensureMasterGain(audioCtx);

        return audioCtx;
    };

    const ensureContextForDecode = (): AudioContext => {
        const ctor = resolveAudioContextCtor();
        if (!ctor) {
            throw new Error("Web Audio API is not supported in this browser (missing AudioContext).");
        }

        if (audioCtx && audioCtx.state !== "closed") {
            ensureMasterGain(audioCtx);
            return audioCtx;
        }

        audioCtx = new ctor({ latencyHint: "interactive" });
        ensureMasterGain(audioCtx);

        return audioCtx;
    };

    const resolveEarconUrl = (id: EarconId): string | null => {
        const raw = window.sprechenIoWeb?.earconUrls?.[id];
        if (typeof raw !== "string" || raw.trim().length === 0) {
            return null;
        }

        try {
            return new URL(raw, document.baseURI).toString();
        } catch {
            return raw;
        }
    };

    const tryResolveCanonicalHostSupportEarconUrl = (url: string): string | null => {
        let absoluteUrl: URL;
        try {
            absoluteUrl = new URL(url, document.baseURI);
        } catch {
            return null;
        }

        if (!absoluteUrl.pathname.startsWith(HostSupportEarconRoutePrefix)) {
            return null;
        }

        const routeLeaf = absoluteUrl.pathname.slice(HostSupportEarconRoutePrefix.length);
        const canonicalLeaf = routeLeaf.replace(/\.[^.]+\.(ogg|mp3)$/i, ".$1");
        if (canonicalLeaf === routeLeaf) {
            return null;
        }

        const canonicalUrl = new URL(absoluteUrl.toString());
        canonicalUrl.pathname = `${HostSupportEarconRoutePrefix}${canonicalLeaf}`;

        const resolved = canonicalUrl.toString();
        return resolved === absoluteUrl.toString()
            ? null
            : resolved;
    };

    const getEarconFetchCandidateUrls = (url: string): string[] => {
        const candidateUrls = [url];
        const canonicalAliasUrl = tryResolveCanonicalHostSupportEarconUrl(url);
        if (canonicalAliasUrl && !candidateUrls.includes(canonicalAliasUrl)) {
            candidateUrls.push(canonicalAliasUrl);
        }

        return candidateUrls;
    };

    const createCooldownSkipError = (url: string): EarconCooldownSkipError => {
        const error = new Error(`Skipping earcon fetch during cooldown: ${url}`) as EarconCooldownSkipError;
        error.earconCooldownSkip = true;
        return error;
    };

    const isCooldownSkipError = (err: unknown): err is EarconCooldownSkipError => {
        return typeof err === "object"
            && err !== null
            && "earconCooldownSkip" in err
            && (err as { earconCooldownSkip?: unknown }).earconCooldownSkip === true;
    };

    const tryEnterCooldown = (url: string): boolean => {
        const retryAfterEpochMs = failedFetchCooldownsByUrl.get(url);
        if (typeof retryAfterEpochMs !== "number") {
            return false;
        }

        if (retryAfterEpochMs <= Date.now()) {
            failedFetchCooldownsByUrl.delete(url);
            return false;
        }

        return true;
    };

    const rememberFetchFailure = (url: string): void => {
        failedFetchCooldownsByUrl.set(url, Date.now() + EarconFetchFailureCooldownMs);
    };

    const loadBufferForCandidateUrlAsync = async (ctx: AudioContext, candidateUrl: string): Promise<AudioBuffer> => {
        const existing = buffersByUrl.get(candidateUrl);
        if (existing) {
            return existing;
        }

        if (tryEnterCooldown(candidateUrl)) {
            throw createCooldownSkipError(candidateUrl);
        }

        const promise = (async (): Promise<AudioBuffer> => {
            const resp = await fetch(candidateUrl, { cache: "force-cache" });
            if (!resp.ok) {
                throw new Error(`Failed to fetch earcon (${resp.status}): ${candidateUrl}`);
            }

            const bytes = await resp.arrayBuffer();
            return await ctx.decodeAudioData(bytes);
        })();

        buffersByUrl.set(candidateUrl, promise);
        try {
            return await promise;
        } catch (err) {
            if (buffersByUrl.get(candidateUrl) === promise) {
                buffersByUrl.delete(candidateUrl);
            }

            rememberFetchFailure(candidateUrl);
            throw err;
        }
    };

    const loadBufferAsync = async (ctx: AudioContext, url: string): Promise<AudioBuffer> => {
        const existing = buffersByUrl.get(url);
        if (existing) {
            return existing;
        }

        const promise = (async (): Promise<AudioBuffer> => {
            const candidateUrls = getEarconFetchCandidateUrls(url);
            let firstFailure: unknown = null;
            let skippedForCooldown = false;

            for (const candidateUrl of candidateUrls) {
                try {
                    return await loadBufferForCandidateUrlAsync(ctx, candidateUrl);
                } catch (err) {
                    if (isCooldownSkipError(err)) {
                        skippedForCooldown = true;
                        continue;
                    }

                    if (firstFailure === null) {
                        firstFailure = err;
                    }
                }
            }

            if (firstFailure !== null) {
                throw firstFailure;
            }

            if (skippedForCooldown) {
                throw createCooldownSkipError(url);
            }

            throw new Error(`Failed to resolve earcon fetch candidates: ${url}`);
        })();

        buffersByUrl.set(url, promise);
        try {
            return await promise;
        } catch (err) {
            if (buffersByUrl.get(url) === promise) {
                buffersByUrl.delete(url);
            }

            throw err;
        }
    };

    const stopAll = (): void => {
        clearProcessingWatchdog();
        stopGeneration++;

        const queued = queue;
        queue = [];
        for (const item of queued) {
            completeQueueItem(item);
        }

        pendingProcessing = null;
        processingToken++;
        processingUrl = null;

        if (!activeSource) {
        } else {
            const source = activeSource;
            activeSource = null;
            try {
                source.stop();
            } catch {
            }
        }

        if (!processingSource) {
            return;
        }

        const source = processingSource;
        processingSource = null;
        try {
            source.stop();
        } catch {
        }
    };

    const stopFlowProcessing = (): void => {
        clearProcessingWatchdog();
        pendingProcessing = null;

        if (!processingSource) {
            processingUrl = null;
            return;
        }

        const source = processingSource;
        processingSource = null;
        processingUrl = null;
        processingToken++;
        try {
            source.stop();
        } catch {
        }
    };

    // Nur das wiederholte Verarbeitungssignal beenden. Laufende und wartende
    // einmalige Earcons behalten ihre Generation und können zu Ende spielen.
    const stopLooping = (): void => {
        stopFlowProcessing();
    };

    const armProcessingWatchdog = (): void => {
        clearProcessingWatchdog();
        processingWatchdogTimer = setTimeout(() => {
            processingWatchdogTimer = null;
            stopFlowProcessing();
        }, FlowProcessingMaxDurationMs);
    };

    const startFlowProcessingAsync = async (url: string, token: number, generation: number): Promise<void> => {
        if (processingSource) {
            return;
        }

        if (token !== processingToken || generation !== stopGeneration) {
            return;
        }

        try {
            const ctx = await ensureContextAsync();

            if (token !== processingToken || generation !== stopGeneration) {
                return;
            }

            const buffer = await loadBufferAsync(ctx, url);
            if (token !== processingToken || generation !== stopGeneration) {
                return;
            }

            const source = ctx.createBufferSource();
            processingSource = source;
            processingUrl = url;
            source.buffer = buffer;
            source.loop = true;
            source.connect(masterGainNode ?? ctx.destination);
            source.onended = () => {
                if (processingSource === source) {
                    processingSource = null;
                    processingUrl = null;
                    clearProcessingWatchdog();
                }
            };

            try {
                source.start();
                armProcessingWatchdog();
            } catch {
                if (processingSource === source) {
                    processingSource = null;
                    processingUrl = null;
                }
            }
        } catch (err) {
            if (isCooldownSkipError(err)) {
                return;
            }

            // eslint-disable-next-line no-console
            console.warn(`[earcons] Failed to start FlowProcessing from '${url}':`, err);
        }
    };

    const maybeStartFlowProcessing = (): void => {
        if (processingSource || !pendingProcessing) {
            return;
        }

        if (isProcessingQueue || activeSource || queue.length > 0) {
            return;
        }

        const next = pendingProcessing;
        pendingProcessing = null;
        void startFlowProcessingAsync(next.url, next.token, next.generation);
    };

    const processQueueAsync = async (): Promise<void> => {
        if (isProcessingQueue) {
            return;
        }

        isProcessingQueue = true;
        try {
            while (queue.length > 0) {
                const next = queue.shift();
                if (!next) {
                    continue;
                }

                if (next.generation !== stopGeneration) {
                    completeQueueItem(next);
                    continue;
                }

                try {
                    const ctx = await ensureContextAsync();
                    if (next.generation !== stopGeneration) {
                        completeQueueItem(next);
                        continue;
                    }

                    const buffer = await loadBufferAsync(ctx, next.url);
                    if (next.generation !== stopGeneration) {
                        completeQueueItem(next);
                        continue;
                    }

                    await new Promise<void>((resolve) => {
                        const source = ctx.createBufferSource();
                        activeSource = source;
                        source.buffer = buffer;
                        source.connect(masterGainNode ?? ctx.destination);
                        source.onended = () => {
                            if (activeSource === source) {
                                activeSource = null;
                            }

                            resolve();
                        };

                        try {
                            source.start();
                        } catch {
                            if (activeSource === source) {
                                activeSource = null;
                            }

                            resolve();
                        }
                    });

                    completeQueueItem(next);
                } catch (err) {
                    if (isCooldownSkipError(err)) {
                        completeQueueItem(next);
                        continue;
                    }

                    // Fehler beim Abspielen eines Hinweissignals sollen die übrige Anwendung nicht abbrechen.

                    // eslint-disable-next-line no-console
                    console.warn(`[earcons] Failed to play '${next.id}' from '${next.url}':`, err);

                    completeQueueItem(next);
                }
            }
        } finally {
            isProcessingQueue = false;
            maybeStartFlowProcessing();
        }
    };

    const play = (id: EarconId): Promise<void> => {
        if (id === "FlowProcessing") {
            const url = resolveEarconUrl(id);
            if (!url) {
                return Promise.resolve();
            }

            if (processingSource && processingUrl === url) {
                return Promise.resolve();
            }

            // Laufende und wartende Einmalsignale stoppen, bevor das Verarbeitungssignal beginnt.
            stopAll();

            processingToken++;
            pendingProcessing = { url, token: processingToken, generation: stopGeneration };
            maybeStartFlowProcessing();
            return Promise.resolve();
        }

        if (id === "FlowListening" || id === "FlowBargeIn") {
            // Für Zuhörbeginn und Unterbrechung alle laufenden und wartenden Earcons stoppen.
            stopAll();
        } else {
            stopFlowProcessing();
        }

        const url = resolveEarconUrl(id);
        if (!url) {
            return Promise.resolve();
        }

        return new Promise<void>((resolve) => {
            queue.push({ id, url, generation: stopGeneration, resolve });
            void processQueueAsync();
        });
    };

    const preview = (id: EarconId): Promise<void> => {
        stopFlowProcessing();

        const url = resolveEarconUrl(id);
        if (!url) {
            return Promise.resolve();
        }

        return new Promise<void>((resolve) => {
            queue.push({ id, url, generation: stopGeneration, resolve });
            void processQueueAsync();
        });
    };

    const preloadAllAsync = async (): Promise<void> => {
        let ctx: AudioContext;
        try {
            ctx = ensureContextForDecode();
        } catch {
            return;
        }

        const urls = new Set<string>();
        const entries = window.sprechenIoWeb?.earconUrls ?? {};
        for (const key of Object.keys(entries)) {
            const id = key as EarconId;
            const url = resolveEarconUrl(id);
            if (url) {
                urls.add(url);
            }
        }

        await Promise.allSettled(Array.from(urls, (url) => loadBufferAsync(ctx, url)));
    };

    const warmup = async (): Promise<void> => {
        if (warmupStarted) {
            return;
        }

        warmupStarted = true;

        try {
            await ensureContextAsync();
        } catch {
            // Browser können resume bis zu einer Benutzergeste sperren. Decodieren bleibt gegebenenfalls möglich.
        }

        void preloadAllAsync();
    };

    window.earcons = {
        play,
        preview,
        stopAll,
        stopLooping,
        warmup,
        getVolume: () => volume,
        setVolume: (value: unknown): void => {
            const parsed = typeof value === "number"
                ? value
                : typeof value === "string"
                    ? Number.parseFloat(value)
                    : NaN;

            if (!Number.isFinite(parsed)) {
                return;
            }

            volume = clamp(parsed, 0, 1);

            if (audioCtx && audioCtx.state !== "closed" && masterGainNode) {
                try {
                    masterGainNode.gain.setTargetAtTime(volume, audioCtx.currentTime, 0.015);
                } catch {
                    masterGainNode.gain.value = volume;
                }
            }
        }
    };
})();
