"use strict";
(() => {
    "use strict";
    // Fester Vorlauf vor dem ersten Audioblock einer Äußerung.
    // Optionale Stille füllt höchstens dieses Intervall und endet unmittelbar vor dem Inhalt.
    const ScheduledFirstContentLeadMs = 100;
    const DefaultVolume = 1.0;
    const DefaultMakeup = 1.0;
    // Latenzkategorie des AudioContext. Diagnoseeinstellung erlaubt playback, interactive
    // oder balanced. Andere Werte verwenden interactive. Die tatsächliche Latenz hängt vom Gerät ab.
    const LatencyHintStorageKey = "sprechenIo.audio.latencyHint";
    const ScheduledDefaultLatencyHint = "interactive";
    // Optionale Stille unmittelbar vor dem ersten Audioblock.
    // Der Wert 0 lässt sie weg. Der feste Startvorlauf bleibt bestehen.
    const ScheduledLeadingSilenceMsStorageKey = "sprechenIo.audio.scheduledLeadingSilenceMs";
    const DefaultScheduledLeadingSilenceMs = ScheduledFirstContentLeadMs;
    const MinScheduledLeadingSilenceMs = 0;
    // Keep-alive kombiniert einen optionalen Analyser-Zweig mit einem wiederholten
    // leisen Signal von etwa -80 dBFS. Es soll die Audioausgabe in Pausen aktiv halten.
    // Der Wert off deaktiviert beides.
    const ScheduledKeepAliveStorageKey = "sprechenIo.audio.scheduledKeepAlive";
    const DefaultScheduledKeepAlive = true;
    const ScheduledKeepAliveAmplitude = 1e-4;
    // Tiefe Frequenz für das Keep-alive-Signal. Die tatsächliche Hörbarkeit ist geräteabhängig.
    const ScheduledKeepAliveToneHz = 47;
    // Einmaliger Aufwachvorlauf ab Aktivierung des Keep-alive-Signals.
    // Resume und Aufheben der Stummschaltung starten die Frist erneut.
    // Nur die verbleibende Zeit wird addiert. 0 deaktiviert diesen zusätzlichen Vorlauf.
    const ScheduledFirstUtteranceWakeMsStorageKey = "sprechenIo.audio.scheduledFirstUtteranceWakeMs";
    const DefaultScheduledFirstUtteranceWakeMs = 2500;
    const MinScheduledFirstUtteranceWakeMs = 0;
    const MaxScheduledFirstUtteranceWakeMs = 8000;
    const ScheduledSilenceScanQuantumFrames = 128;
    // Zusätzliche Wartezeit nach der erwarteten Restdauer. Fehlt danach die Abschlussmeldung,
    // meldet der geplante Pfad einen Fehler und der Streamingpfad den Ausgang watchdog.
    const ScheduledDrainWatchdogMarginMs = 2000;
    // Bleibt null, wenn das Worklet nicht eingerichtet werden kann. Dann übernimmt
    // die Wiedergabe mit einzeln geplanten Audioquellen.
    let streamingSink = null;
    let streamingSinkAttempted = false;
    let microSharpPlayback = false;
    let pendingEnqueues = Promise.resolve();
    // Sitzungsweiter Grund für den gewählten Wiedergabepfad. Separat halten,
    // weil reset die Ereignisse der einzelnen Äußerung löscht.
    let streamingSinkStatus = "pending";
    // Geplanter Vorlauf und Kontextzustand beim erfassten Beginn der Äußerung.
    let firstContentLeadMs = null;
    let firstContentCtxState = null;
    const resolveStreamingSinkFactory = () => globalThis.sprechenIoPlaybackSink?.createWorkletPlaybackSink;
    const createLegacyStreamingSink = async (rate) => {
        const factory = resolveStreamingSinkFactory();
        const moduleUrls = window.sprechenIoWeb?.pcmPlaybackWorkletUrls;
        if (!factory || !moduleUrls || moduleUrls.length === 0 || !audioCtx || !masterGainNode) {
            streamingSinkStatus = !factory || !moduleUrls || moduleUrls.length === 0
                ? "unwired"
                : "no-context";
            return null;
        }
        try {
            const sink = await factory(audioCtx, masterGainNode, rate, moduleUrls);
            streamingSinkStatus = "streaming";
            return sink;
        }
        catch (error) {
            // Bei fehlgeschlagenem Worklet-Aufbau den geplanten Wiedergabepfad verwenden
            // und den Grund im Bericht festhalten.
            streamingSinkStatus = "error";
            recordCtxStateEvent("streaming-sink-unavailable");
            return null;
        }
    };
    const ensureStreamingSinkAsync = async () => {
        if (streamingSink || streamingSinkAttempted) {
            return streamingSink;
        }
        streamingSinkAttempted = true;
        const microSharpFactory = globalThis.sprechenIoMicroSharpPlayback?.createMicroSharpPlaybackSink;
        if (microSharpFactory && audioCtx && masterGainNode && prefersMicroSharpPlayback()) {
            try {
                streamingSink = await microSharpFactory(audioCtx, masterGainNode, pcmSampleRate, ScheduledFirstContentLeadMs + remainingFirstContentWakeMs());
                microSharpPlayback = true;
                streamingSinkStatus = "microsharp";
                return streamingSink;
            }
            catch {
                recordCtxStateEvent("microsharp-sink-unavailable");
            }
        }
        streamingSink = await createLegacyStreamingSink(pcmSampleRate);
        return streamingSink;
    };
    const prefersMicroSharpPlayback = () => {
        try {
            return window.localStorage.getItem("sprechenIo.audio.playbackTransport") === "microsharp";
        }
        catch {
            return false;
        }
    };
    const createPcmPlayerFailure = (errorCode, diagnosticDetail) => {
        const error = new Error(`${errorCode}: ${diagnosticDetail}`);
        Object.defineProperty(error, "pcmPlayerErrorCode", {
            value: errorCode,
            enumerable: false,
            configurable: false,
            writable: false
        });
        return error;
    };
    const invokePcmPlayerOperation = async (operation) => {
        try {
            await operation();
            return { succeeded: true, errorCode: null, diagnosticDetail: null };
        }
        catch (error) {
            const candidate = error;
            const errorCode = candidate && typeof candidate.pcmPlayerErrorCode === "string"
                ? candidate.pcmPlayerErrorCode
                : "PCM_PLAYER_UNEXPECTED_FAILURE";
            const diagnosticDetail = candidate && typeof candidate.message === "string"
                ? candidate.message
                : "Unexpected PCM playback failure.";
            return { succeeded: false, errorCode, diagnosticDetail };
        }
    };
    // Wiedergabediagnose an Konsole und Host senden, sofern sie freigegeben ist.
    // Der Bericht enthält Dauerwerte, Unterläufe und Zustandswechsel je Äußerung.
    const DebugEnabledStorageKey = "sprechenIo.browserDebug.enabled";
    const PlaybackDiagnosticsEndpoint = "/api/playback-diagnostics";
    let ctxStateEvents = [];
    let visibilityEvents = [];
    let utteranceSeq = 0;
    let drainStartMs = 0;
    let visibilityListenerAttached = false;
    const nowMs = () => {
        try {
            return Math.round(performance.now());
        }
        catch {
            return Date.now();
        }
    };
    const isDiagnosticsEnabled = () => {
        try {
            if (window.browserDebugConsole && typeof window.browserDebugConsole.getDiagnosticsAdmitted === "function") {
                return window.browserDebugConsole.getDiagnosticsAdmitted();
            }
            // Ohne zentrale Diagnosefreigabe gilt die lokale Vorgabe: an, sofern nicht ausdrücklich false.
            return window.localStorage.getItem(DebugEnabledStorageKey) !== "false";
        }
        catch {
            return true;
        }
    };
    const diagnosticIdentity = () => {
        try {
            if (!window.browserDebugConsole || typeof window.browserDebugConsole.getDiagnosticSession !== "function") {
                return {};
            }
            const session = window.browserDebugConsole.getDiagnosticSession();
            if (!session || session.enabled !== true) {
                return {};
            }
            const stamped = {};
            if (typeof session.sessionId === "string" && session.sessionId.length > 0) {
                stamped.sessionId = session.sessionId;
            }
            if (typeof session.clientCorrelationId === "string" && session.clientCorrelationId.length > 0) {
                stamped.clientCorrelationId = session.clientCorrelationId;
            }
            if (typeof session.operatingMode === "string" && session.operatingMode.length > 0) {
                stamped.operatingMode = session.operatingMode;
            }
            return stamped;
        }
        catch {
            return {};
        }
    };
    const recordCtxStateEvent = (kind) => {
        const state = audioCtx ? audioCtx.state : "closed";
        ctxStateEvents.push({ t: nowMs(), kind, state });
    };
    const attachVisibilityListener = () => {
        if (visibilityListenerAttached || typeof document === "undefined" || typeof document.addEventListener !== "function") {
            return;
        }
        visibilityListenerAttached = true;
        document.addEventListener("visibilitychange", () => {
            const state = typeof document.visibilityState === "string" ? document.visibilityState : "unknown";
            visibilityEvents.push({ t: nowMs(), kind: "visibilitychange", state });
        });
    };
    const postPlaybackReport = (report) => {
        try {
            const body = JSON.stringify(report);
            const nav = typeof navigator !== "undefined" ? navigator : undefined;
            if (nav && typeof nav.sendBeacon === "function" && typeof Blob !== "undefined") {
                nav.sendBeacon(PlaybackDiagnosticsEndpoint, new Blob([body], { type: "application/json" }));
                return;
            }
            if (typeof fetch === "function") {
                void fetch(PlaybackDiagnosticsEndpoint, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body,
                    keepalive: true
                }).catch(() => { });
            }
        }
        catch {
            // Diagnose darf die Wiedergabe nie stören
        }
    };
    const emitPlaybackReport = (writtenFrames, playedOutFrames, underruns, drainOutcome, outputSilence) => {
        if (!isDiagnosticsEnabled()) {
            return;
        }
        const sampleRate = audioCtx && typeof audioCtx.sampleRate === "number" && audioCtx.sampleRate > 0
            ? audioCtx.sampleRate
            : 0;
        const round2 = (value) => Math.round(value * 100) / 100;
        const writtenSeconds = sampleRate > 0 ? round2(writtenFrames / sampleRate) : 0;
        const playedOutSeconds = sampleRate > 0 ? round2(playedOutFrames / sampleRate) : 0;
        const silentOutputWindows = outputSilence ? outputSilence.silentOutputWindows : 0;
        const maxContiguousSilentMs = outputSilence ? round2(outputSilence.maxContiguousSilentMs) : 0;
        const outputSampleRate = outputSilence ? outputSilence.outputSampleRate : 0;
        const keepAlive = outputSilence && outputSilence.keepAlive ? outputSilence.keepAlive : undefined;
        const playbackPath = outputSilence?.playbackPath;
        const droppedFrames = outputSilence?.droppedFrames;
        const starvedQuanta = outputSilence?.starvedQuanta;
        const underrunOutputFrames = outputSilence?.underrunOutputFrames;
        const onsetPeakDbfs = outputSilence?.onsetPeakDbfs;
        const onsetFrames = outputSilence?.onsetFrames;
        const report = {
            event: "playback_report",
            utteranceSeq: ++utteranceSeq,
            ts: new Date().toISOString(),
            ...diagnosticIdentity(),
            writtenSeconds,
            playedOutSeconds,
            ...(typeof underruns === "number" ? { underruns } : {}),
            silentOutputWindows,
            maxContiguousSilentMs,
            outputSampleRate,
            ...(keepAlive ? { keepAlive } : {}),
            ...(playbackPath ? { playbackPath } : {}),
            streamingSinkStatus,
            firstContentLeadMs,
            firstContentCtxState,
            ...(typeof droppedFrames === "number" ? { droppedFrames } : {}),
            ...(typeof starvedQuanta === "number" ? { starvedQuanta } : {}),
            ...(typeof underrunOutputFrames === "number" ? { underrunOutputFrames } : {}),
            ...(onsetPeakDbfs === null || typeof onsetPeakDbfs === "number" ? { onsetPeakDbfs } : {}),
            ...(typeof onsetFrames === "number" ? { onsetFrames } : {}),
            drainOutcome,
            drainMs: drainStartMs > 0 ? nowMs() - drainStartMs : 0,
            ctxState: audioCtx ? audioCtx.state : "closed",
            ctxStateEvents,
            visibilityEvents
        };
        try {
            console.info(`pcmPlayer: playback report seq=${report.utteranceSeq} written=${writtenSeconds}s playedOut=${playedOutSeconds}s `
                + `underruns=${underruns} silentOutputWindows=${silentOutputWindows} maxContiguousSilentMs=${maxContiguousSilentMs} `
                + `outputSampleRate=${outputSampleRate} outcome=${drainOutcome} ctx=${report.ctxState} `
                + `path=${playbackPath ?? "unknown"} sink=${streamingSinkStatus} `
                + `lead=${firstContentLeadMs ?? "n/a"}ms ctxAtFirst=${firstContentCtxState ?? "n/a"} `
                + `onset=${onsetPeakDbfs ?? "n/a"}dBFS onsetFrames=${onsetFrames ?? "n/a"} `
                + `ctxEvents=${ctxStateEvents.length} visEvents=${visibilityEvents.length}`);
        }
        catch {
        }
        postPlaybackReport(report);
    };
    const clamp = (value, min, max) => Math.max(min, Math.min(max, value));
    const tryReadScheduledLeadingSilenceMs = () => {
        try {
            const raw = window.localStorage.getItem(ScheduledLeadingSilenceMsStorageKey);
            if (raw === null) {
                return DefaultScheduledLeadingSilenceMs;
            }
            const parsed = Number.parseFloat(raw);
            if (!Number.isFinite(parsed) || parsed < 0) {
                return DefaultScheduledLeadingSilenceMs;
            }
            return clamp(Math.round(parsed), MinScheduledLeadingSilenceMs, ScheduledFirstContentLeadMs);
        }
        catch {
            return DefaultScheduledLeadingSilenceMs;
        }
    };
    const tryReadScheduledFirstUtteranceWakeMs = () => {
        try {
            const raw = window.localStorage.getItem(ScheduledFirstUtteranceWakeMsStorageKey);
            if (raw === null) {
                return DefaultScheduledFirstUtteranceWakeMs;
            }
            const parsed = Number.parseFloat(raw);
            if (!Number.isFinite(parsed) || parsed < 0) {
                return DefaultScheduledFirstUtteranceWakeMs;
            }
            return clamp(Math.round(parsed), MinScheduledFirstUtteranceWakeMs, MaxScheduledFirstUtteranceWakeMs);
        }
        catch {
            return DefaultScheduledFirstUtteranceWakeMs;
        }
    };
    const tryReadLatencyHint = () => {
        try {
            const raw = window.localStorage.getItem(LatencyHintStorageKey);
            if (raw === "playback" || raw === "interactive" || raw === "balanced") {
                return raw;
            }
            return ScheduledDefaultLatencyHint;
        }
        catch {
            return ScheduledDefaultLatencyHint;
        }
    };
    const tryReadScheduledKeepAlive = () => {
        try {
            const raw = window.localStorage.getItem(ScheduledKeepAliveStorageKey);
            if (raw === "off") {
                return false;
            }
            return DefaultScheduledKeepAlive;
        }
        catch {
            return DefaultScheduledKeepAlive;
        }
    };
    let audioCtx = null;
    let masterGainNode = null;
    let pcmSampleRate = 24000;
    let scheduledInputRate = null;
    let scheduledGeneration = 0;
    // A later init may prepare the next utterance while the current one still owns its DSP.
    const playbackSampleRate = () => scheduledInputRate ?? pcmSampleRate;
    const bindScheduledInputRate = (rate) => {
        if (scheduledInputRate === null) {
            streamingSink?.bindInputRate(rate);
            scheduledInputRate = rate;
        }
    };
    let volume = DefaultVolume;
    // Verstärkungsfaktor vor der Begrenzung und vor der Benutzerlautstärke.
    // Er wird über setMakeup eingestellt.
    let makeup = DefaultMakeup;
    const scheduledLeadingSilenceMs = tryReadScheduledLeadingSilenceMs();
    let scheduledKeepAlive = tryReadScheduledKeepAlive();
    // Sitzungsweite Keep-alive-Quelle außerhalb der Zähler für einzelne Äußerungen.
    let scheduledKeepAliveSource = null;
    // Nach angefordertem Warmup bei init und enqueue erneut versuchen,
    // ein zuvor fehlgeschlagenes Keep-alive einzurichten.
    let scheduledKeepAlivePrimingRequested = false;
    // Kontextzeit der letzten Keep-alive-Aktivierung als Beginn des Aufwachvorlaufs.
    let scheduledKeepAliveStartedAtCtxTime = null;
    // Ob der einmalige Aufwachvorlauf bereits verwendet wurde.
    // Resume und Aufheben der Stummschaltung setzen diesen Zustand zurück.
    let scheduledFirstContentScheduled = false;
    let scheduledFirstUtteranceWakeMs = tryReadScheduledFirstUtteranceWakeMs();
    let scheduledAnalyserNode = null;
    let scheduledCtxInitPromise = null;
    let playbackOutputInitPromise = null;
    let scheduledDsp = null;
    let scheduledDspRate = 0;
    let scheduledSources = [];
    let scheduledOutstanding = 0;
    // Startzeit des nächsten Audioblocks. Der erste Block erhält Vorlauf.
    // Danach wird die Zeit um die jeweilige Pufferdauer weitergeschoben.
    // scheduledCursorArmed kennzeichnet eine bereits gesetzte Startzeit.
    let scheduledNextStartTime = 0;
    let scheduledCursorArmed = false;
    // Abschlusszustand für drain: letztes onended oder Überschreitung der Wartezeit.
    let scheduledDrainPromise = null;
    let scheduledDrainResolve = null;
    let scheduledDrainReject = null;
    let scheduledDrainWatchdogTimer = null;
    let scheduledDrainRequested = false;
    // Diagnose je Äußerung: geplante Dauer, über onended abgeschlossene Dauer
    // und Zahl der Blöcke, deren geplante Startzeit bereits verstrichen war.
    let scheduledWrittenSeconds = 0;
    let scheduledPlayedOutSeconds = 0;
    let scheduledUnderruns = 0;
    let scheduledSilentOutputWindows = 0;
    let scheduledMaxContiguousSilentMs = 0;
    const noteContextNotRunning = (state, err) => {
        if (err !== undefined) {
            console.warn(`pcmPlayer: AudioContext resume rejected (state "${state}").`, err);
            return;
        }
        console.warn(`pcmPlayer: AudioContext did not reach "running" (state "${state}").`);
    };
    const resolveStandardizedAudioContext = () => window.standardizedAudioContext;
    const resolveAudioContextCtor = () => {
        const ponyfill = resolveStandardizedAudioContext();
        if (ponyfill && typeof ponyfill.AudioContext === "function") {
            return ponyfill.AudioContext;
        }
        return window.AudioContext || window.webkitAudioContext;
    };
    const ensureAudioContextSupported = () => {
        if (!resolveAudioContextCtor()) {
            throw createPcmPlayerFailure("PCM_PLAYER_AUDIO_CONTEXT_UNAVAILABLE", "Web Audio API is not supported in this browser.");
        }
    };
    const resolveOfflineDsp = () => {
        const dsp = globalThis.sprechenIoAudioDsp;
        if (!dsp || typeof dsp.MakeupLimiterProcessor !== "function" || typeof dsp.scanSilentOutput !== "function") {
            throw createPcmPlayerFailure("PCM_PLAYER_AUDIO_DSP_UNAVAILABLE", "globalThis.sprechenIoAudioDsp is not loaded; the host document must load audioDsp.js before pcmPlayer.js.");
        }
        return dsp;
    };
    const createAudioContext = () => {
        const audioContextCtor = resolveAudioContextCtor();
        if (!audioContextCtor) {
            throw createPcmPlayerFailure("PCM_PLAYER_AUDIO_CONTEXT_UNAVAILABLE", "Web Audio API is not supported in this browser.");
        }
        try {
            return new audioContextCtor({ latencyHint: tryReadLatencyHint() });
        }
        catch (firstError) {
            try {
                return new audioContextCtor();
            }
            catch (fallbackError) {
                const detail = fallbackError instanceof Error
                    ? fallbackError.message
                    : firstError instanceof Error
                        ? firstError.message
                        : "constructor failed";
                throw createPcmPlayerFailure("PCM_PLAYER_AUDIO_CONTEXT_UNAVAILABLE", `AudioContext construction failed (${detail}).`);
            }
        }
    };
    // Verbleibenden Aufwachvorlauf ab Keep-alive-Aktivierung berechnen.
    // Ist die Frist verstrichen, wird keine zusätzliche Wartezeit eingeplant.
    const remainingFirstContentWakeMs = () => {
        if (scheduledFirstContentScheduled || scheduledFirstUtteranceWakeMs <= 0 || !audioCtx) {
            return 0;
        }
        const elapsedSeconds = scheduledKeepAliveStartedAtCtxTime !== null
            ? Math.max(0, audioCtx.currentTime - scheduledKeepAliveStartedAtCtxTime)
            : 0;
        return Math.max(0, scheduledFirstUtteranceWakeMs - (elapsedSeconds * 1000));
    };
    const rearmFirstContentWakeForLiveFloor = (ctx) => {
        if (!scheduledKeepAliveSource) {
            return;
        }
        scheduledKeepAliveStartedAtCtxTime = ctx.currentTime;
        scheduledFirstContentScheduled = false;
    };
    const getVolume = () => volume;
    const setVolume = (value) => {
        const parsed = typeof value === "number"
            ? value
            : typeof value === "string"
                ? Number.parseFloat(value)
                : NaN;
        if (!Number.isFinite(parsed)) {
            return;
        }
        const previousVolume = volume;
        volume = clamp(parsed, 0, 1);
        if (previousVolume <= 0
            && volume > 0
            && audioCtx
            && audioCtx.state !== "closed"
            && scheduledKeepAliveSource) {
            // Nach Aufheben der Stummschaltung den Aufwachvorlauf erneut beginnen.
            rearmFirstContentWakeForLiveFloor(audioCtx);
        }
        if (audioCtx && audioCtx.state !== "closed" && masterGainNode) {
            try {
                masterGainNode.gain.setTargetAtTime(volume, audioCtx.currentTime, 0.015);
            }
            catch {
                masterGainNode.gain.value = volume;
            }
        }
    };
    const getMakeup = () => makeup;
    const setMakeup = (value) => {
        const parsed = typeof value === "number"
            ? value
            : typeof value === "string"
                ? Number.parseFloat(value)
                : NaN;
        makeup = Number.isFinite(parsed) && parsed > 0 ? parsed : DefaultMakeup;
        if (scheduledDsp) {
            scheduledDsp.setMakeup(makeup);
        }
    };
    // Vom Browser angegebene Ausgabelatenz in Millisekunden. Wenn outputLatency
    // nicht positiv und endlich ist, baseLatency verwenden. Andernfalls 0.
    // Der Wert ist eine Schätzung.
    const getOutputLatencyMs = () => {
        try {
            if (!audioCtx) {
                return 0;
            }
            const ctx = audioCtx;
            const seconds = typeof ctx.outputLatency === "number" && Number.isFinite(ctx.outputLatency) && ctx.outputLatency > 0
                ? ctx.outputLatency
                : (typeof ctx.baseLatency === "number" && Number.isFinite(ctx.baseLatency) && ctx.baseLatency > 0
                    ? ctx.baseLatency
                    : 0);
            return seconds > 0 ? seconds * 1000 : 0;
        }
        catch {
            return 0;
        }
    };
    // Jeden PCM-Block mit dem gemeinsamen DSP verarbeiten. Ein eingerichtetes Worklet
    // übernimmt Streaming und Resampling. Andernfalls werden einzelne AudioBuffer geplant.
    const Int16ToFloatScale = 1 / 32768;
    // PCM-Bytes als ArrayBuffer bereitstellen. Nur eine vollständige Ansicht
    // auf einen gewöhnlichen ArrayBuffer wird direkt verwendet. Andere Ansichten werden kopiert.
    const toPcmArrayBuffer = (pcm16leBytes) => {
        const u8 = pcm16leBytes instanceof Uint8Array
            ? pcm16leBytes
            : pcm16leBytes instanceof ArrayBuffer
                ? new Uint8Array(pcm16leBytes)
                : null;
        if (!u8 || u8.byteLength === 0) {
            return null;
        }
        return u8.byteOffset === 0 && u8.byteLength === u8.buffer.byteLength && u8.buffer instanceof ArrayBuffer
            ? u8.buffer
            : u8.slice().buffer;
    };
    const decodeInt16ToFloat = (buffer) => {
        const sampleCount = Math.floor(buffer.byteLength / 2);
        const pcm = new Int16Array(buffer, 0, sampleCount);
        const out = new Float32Array(sampleCount);
        for (let i = 0; i < sampleCount; i++) {
            out[i] = Math.fround(Math.max(-1, Math.min(1, pcm[i] * Int16ToFloatScale)));
        }
        return out;
    };
    const scheduledContextReady = () => audioCtx !== null && audioCtx.state === "running" && masterGainNode !== null;
    const closeFailedScheduledContextAsync = async (ctx) => {
        if (audioCtx === ctx) {
            audioCtx = null;
        }
        masterGainNode = null;
        scheduledKeepAliveSource = null;
        scheduledKeepAliveStartedAtCtxTime = null;
        scheduledAnalyserNode = null;
        try {
            await ctx.close();
        }
        catch {
            // Fehler beim Schließen nicht über den ursprünglichen Initialisierungsfehler stellen.
            // Der fehlgeschlagene Kontext wird bei einem erneuten Versuch nicht wiederverwendet.
        }
    };
    const ensureScheduledContextRunningAsync = async (ctx, operation) => {
        if (ctx.state === "running") {
            return;
        }
        try {
            await ctx.resume();
        }
        catch (error) {
            await closeFailedScheduledContextAsync(ctx);
            const detail = error instanceof Error ? error.message : "resume failed";
            throw createPcmPlayerFailure("PCM_PLAYER_AUDIO_CONTEXT_UNAVAILABLE", `AudioContext resume failed during ${operation} (${detail}).`);
        }
        const resumedState = String(ctx.state);
        if (resumedState !== "running") {
            const state = resumedState;
            noteContextNotRunning(state);
            await closeFailedScheduledContextAsync(ctx);
            throw createPcmPlayerFailure("AUDIO_CONTEXT_SUSPENDED", `AudioContext remained "${state}" after resume during ${operation}.`);
        }
        rearmFirstContentWakeForLiveFloor(ctx);
    };
    // AudioContext und Master-Gain aufbauen oder wiederverwenden.
    // Der Warmup-Aufrufer kann dafür eine Benutzergeste nutzen.
    const ensureScheduledContextAsync = () => {
        ensureAudioContextSupported();
        if (scheduledContextReady()) {
            return Promise.resolve();
        }
        if (scheduledCtxInitPromise) {
            return scheduledCtxInitPromise;
        }
        const promise = (async () => {
            const existingCtx = audioCtx;
            if (existingCtx && existingCtx.state !== "closed" && masterGainNode) {
                await ensureScheduledContextRunningAsync(existingCtx, "context reuse");
                return;
            }
            const ctx = createAudioContext();
            audioCtx = ctx;
            masterGainNode = null;
            scheduledKeepAliveSource = null;
            scheduledAnalyserNode = null;
            scheduledFirstContentScheduled = false;
            scheduledKeepAliveStartedAtCtxTime = null;
            attachVisibilityListener();
            if (typeof ctx.addEventListener === "function") {
                ctx.addEventListener("statechange", () => recordCtxStateEvent("statechange"));
            }
            recordCtxStateEvent("init");
            await ensureScheduledContextRunningAsync(ctx, "initialization");
            try {
                const gain = ctx.createGain();
                gain.gain.value = volume;
                gain.connect(ctx.destination);
                masterGainNode = gain;
            }
            catch (error) {
                await closeFailedScheduledContextAsync(ctx);
                const detail = error instanceof Error ? error.message : "graph construction failed";
                throw createPcmPlayerFailure("PCM_PLAYER_GRAPH_INIT_FAILED", `output graph construction failed (${detail}).`);
            }
        })();
        scheduledCtxInitPromise = promise;
        void promise.finally(() => {
            scheduledCtxInitPromise = null;
        }).catch(() => { });
        return promise;
    };
    const ensurePlaybackOutputAsync = () => {
        if (playbackOutputInitPromise) {
            return playbackOutputInitPromise;
        }
        if (scheduledContextReady() && streamingSinkAttempted) {
            return Promise.resolve(streamingSink);
        }
        const initialization = ensureScheduledContextAsync().then(ensureStreamingSinkAsync);
        playbackOutputInitPromise = initialization;
        const clearInitialization = () => {
            playbackOutputInitPromise = null;
        };
        // Clear only once the shared promise settles and earlier enqueues are ready to resume.
        void initialization.then(clearInitialization, clearInitialization);
        return initialization;
    };
    // Keep-alive-Quelle und optionalen Analyser einmal pro Kontext einrichten.
    // Scheitert die benötigte Quelle, den Fehler melden, damit ein späterer Aufruf erneut versuchen kann.
    const ensureScheduledKeepAlive = () => {
        if (!scheduledKeepAlive) {
            return;
        }
        const ctx = audioCtx;
        const gain = masterGainNode;
        if (!ctx || ctx.state === "closed" || !gain) {
            throw createPcmPlayerFailure("PCM_PLAYER_KEEP_ALIVE_FAILED", "The playback output graph is unavailable while arming the non-zero floor.");
        }
        if (!scheduledAnalyserNode && typeof ctx.createAnalyser === "function") {
            try {
                // Analyser am Eingang verbinden, ohne einen zweiten Ausgangspfad anzulegen.
                scheduledAnalyserNode = ctx.createAnalyser();
                gain.connect(scheduledAnalyserNode);
            }
            catch {
                scheduledAnalyserNode = null;
            }
        }
        if (!scheduledKeepAliveSource) {
            let source = null;
            try {
                // Eine Sekunde Sinus mit kleinem Pegel wiederholen. Die physische Wirkung
                // dieses digitalen Keep-alive-Signals hängt von der Ausgaberoute ab.
                const frames = Math.max(1, Math.round(ctx.sampleRate));
                const buffer = ctx.createBuffer(1, frames, ctx.sampleRate);
                const data = buffer.getChannelData(0);
                const angular = (2 * Math.PI * ScheduledKeepAliveToneHz) / ctx.sampleRate;
                for (let i = 0; i < frames; i++) {
                    data[i] = Math.sin(angular * i) * ScheduledKeepAliveAmplitude;
                }
                source = ctx.createBufferSource();
                source.buffer = buffer;
                source.loop = true;
                source.connect(gain);
                source.start();
                scheduledKeepAliveSource = source;
                rearmFirstContentWakeForLiveFloor(ctx);
            }
            catch (error) {
                if (source) {
                    try {
                        source.disconnect();
                    }
                    catch {
                        // Den ursprünglichen Fehler beim Starten der Quelle beibehalten.
                    }
                }
                scheduledKeepAliveSource = null;
                scheduledKeepAliveStartedAtCtxTime = null;
                const detail = error instanceof Error ? error.message : "floor construction failed";
                throw createPcmPlayerFailure("PCM_PLAYER_KEEP_ALIVE_FAILED", `Non-zero output floor could not start (${detail}).`);
            }
        }
    };
    const ensureScheduledDsp = () => {
        const rate = playbackSampleRate();
        if (!scheduledDsp || scheduledDspRate !== rate) {
            const DspProcessor = resolveOfflineDsp().MakeupLimiterProcessor;
            try {
                scheduledDsp = new DspProcessor(rate, { makeup });
                scheduledDspRate = rate;
            }
            catch (error) {
                scheduledDsp = null;
                scheduledDspRate = 0;
                const detail = error instanceof Error ? error.message : "DSP construction failed";
                throw createPcmPlayerFailure("PCM_PLAYER_DSP_PROCESS_FAILED", `DSP construction failed (${detail}).`);
            }
        }
        return scheduledDsp;
    };
    const disarmScheduledDrainWatchdog = () => {
        if (scheduledDrainWatchdogTimer !== null) {
            clearTimeout(scheduledDrainWatchdogTimer);
            scheduledDrainWatchdogTimer = null;
        }
    };
    const clearScheduledDrainState = () => {
        disarmScheduledDrainWatchdog();
        scheduledDrainPromise = null;
        scheduledDrainResolve = null;
        scheduledDrainReject = null;
        scheduledDrainRequested = false;
    };
    // Geplante Quellen stoppen und trennen. Wiederholte Freigabe soll auch
    // nach einem Fehler beim Stoppen die übrigen Quellen erreichen.
    const stopAllScheduledSources = () => {
        for (const source of scheduledSources) {
            try {
                source.onended = null;
            }
            catch {
            }
            try {
                source.stop();
            }
            catch {
                // Ein fehlgeschlagener Stopp darf das weitere Aufräumen nicht verhindern.
            }
            try {
                source.disconnect();
            }
            catch {
            }
        }
        scheduledSources = [];
        scheduledOutstanding = 0;
    };
    // Verarbeiteten Puffer als nächste Quelle planen und die Startzeit weiterschieben.
    // Ist die vorgesehene Zeit schon vergangen, einen Unterlauf zählen.
    const scheduleProcessedBuffer = (processed) => {
        if (processed.length === 0) {
            return;
        }
        const ctx = audioCtx;
        const gain = masterGainNode;
        if (!ctx || ctx.state === "closed" || !gain) {
            throw createPcmPlayerFailure("PCM_PLAYER_GRAPH_INIT_FAILED", "The playback output graph is unavailable.");
        }
        let audioBuffer;
        let source;
        try {
            audioBuffer = ctx.createBuffer(1, processed.length, playbackSampleRate());
            audioBuffer.getChannelData(0).set(processed);
            source = ctx.createBufferSource();
            source.buffer = audioBuffer;
            source.connect(gain);
        }
        catch (error) {
            const detail = error instanceof Error ? error.message : "source creation failed";
            throw createPcmPlayerFailure("PCM_PLAYER_SOURCE_CREATE_FAILED", `Audio buffer/source creation failed (${detail}).`);
        }
        const cursorWasArmed = scheduledCursorArmed;
        let nextStartTime = scheduledNextStartTime;
        let leadingSilenceSource = null;
        let leadingSilenceStartAttempted = false;
        if (!cursorWasArmed) {
            const now = ctx.currentTime;
            let wakeLeadSeconds = 0;
            if (!scheduledFirstContentScheduled && scheduledFirstUtteranceWakeMs > 0) {
                const elapsedSinceFloor = scheduledKeepAliveStartedAtCtxTime !== null
                    ? Math.max(0, now - scheduledKeepAliveStartedAtCtxTime)
                    : 0;
                wakeLeadSeconds = Math.max(0, scheduledFirstUtteranceWakeMs / 1000 - elapsedSinceFloor);
            }
            const contentStart = now + wakeLeadSeconds + ScheduledFirstContentLeadMs / 1000;
            if (!scheduledFirstContentScheduled) {
                firstContentLeadMs = Math.round((contentStart - now) * 1000);
                firstContentCtxState = ctx.state;
            }
            const leadingSilenceMs = scheduledLeadingSilenceMs;
            if (leadingSilenceMs > 0) {
                try {
                    const rate = playbackSampleRate();
                    const leadingSilenceFrames = Math.max(1, Math.round((leadingSilenceMs / 1000) * rate));
                    const leadingSilenceBuffer = ctx.createBuffer(1, leadingSilenceFrames, rate);
                    leadingSilenceSource = ctx.createBufferSource();
                    leadingSilenceSource.buffer = leadingSilenceBuffer;
                    leadingSilenceSource.connect(gain);
                    scheduledSources.push(leadingSilenceSource);
                    leadingSilenceSource.onended = () => {
                        const idx = scheduledSources.indexOf(leadingSilenceSource);
                        if (idx >= 0) {
                            scheduledSources.splice(idx, 1);
                        }
                    };
                    leadingSilenceStartAttempted = true;
                    leadingSilenceSource.start(contentStart - leadingSilenceFrames / rate);
                }
                catch (error) {
                    if (leadingSilenceSource) {
                        const idx = scheduledSources.indexOf(leadingSilenceSource);
                        if (idx >= 0) {
                            scheduledSources.splice(idx, 1);
                        }
                        try {
                            leadingSilenceSource.disconnect();
                        }
                        catch {
                            // Den ursprünglichen Fehler beim Anlegen oder Starten der Stillequelle beibehalten.
                        }
                    }
                    try {
                        source.disconnect();
                    }
                    catch {
                        // Den ursprünglichen Fehler beim Anlegen oder Starten der Stillequelle beibehalten.
                    }
                    const detail = error instanceof Error ? error.message : "leading-silence start failed";
                    const code = leadingSilenceStartAttempted
                        ? "PCM_PLAYER_SOURCE_START_FAILED"
                        : "PCM_PLAYER_SOURCE_CREATE_FAILED";
                    throw createPcmPlayerFailure(code, `Leading-silence scheduling failed (${detail}).`);
                }
            }
            nextStartTime = contentStart;
        }
        let when = nextStartTime;
        let wasLate = false;
        if (when < ctx.currentTime) {
            scheduledUnderruns++;
            wasLate = true;
            when = ctx.currentTime;
        }
        const bufferDuration = audioBuffer.duration;
        scheduledWrittenSeconds += bufferDuration;
        scheduledOutstanding++;
        scheduledSources.push(source);
        source.onended = () => {
            scheduledOutstanding = Math.max(0, scheduledOutstanding - 1);
            scheduledPlayedOutSeconds += bufferDuration;
            const idx = scheduledSources.indexOf(source);
            if (idx >= 0) {
                scheduledSources.splice(idx, 1);
            }
            maybeCompleteScheduledDrain();
        };
        try {
            source.start(when);
        }
        catch (error) {
            source.onended = null;
            scheduledOutstanding = Math.max(0, scheduledOutstanding - 1);
            scheduledWrittenSeconds = Math.max(0, scheduledWrittenSeconds - bufferDuration);
            if (wasLate) {
                scheduledUnderruns = Math.max(0, scheduledUnderruns - 1);
            }
            const idx = scheduledSources.indexOf(source);
            if (idx >= 0) {
                scheduledSources.splice(idx, 1);
            }
            try {
                source.disconnect();
            }
            catch {
                // Aufräumfehler ignorieren, damit das Trennen und Stoppen den Startfehler nicht ersetzen.
            }
            if (leadingSilenceSource) {
                leadingSilenceSource.onended = null;
                const leadingSilenceIdx = scheduledSources.indexOf(leadingSilenceSource);
                if (leadingSilenceIdx >= 0) {
                    scheduledSources.splice(leadingSilenceIdx, 1);
                }
                try {
                    leadingSilenceSource.stop();
                }
                catch {
                    // Aufräumfehler ignorieren, damit das Trennen und Stoppen den Startfehler nicht ersetzen.
                }
                try {
                    leadingSilenceSource.disconnect();
                }
                catch {
                    // Aufräumfehler ignorieren, damit das Trennen und Stoppen den Startfehler nicht ersetzen.
                }
            }
            const detail = error instanceof Error ? error.message : "source start failed";
            throw createPcmPlayerFailure("PCM_PLAYER_SOURCE_START_FAILED", `Content source start failed (${detail}).`);
        }
        if (!cursorWasArmed) {
            scheduledCursorArmed = true;
            scheduledFirstContentScheduled = true;
        }
        scheduledNextStartTime = when + bufferDuration;
    };
    // Die letzten Samples einzeln geplanter Puffer auf null ausblenden,
    // um Spitzen bei der browserseitigen Abtastratenwandlung am Pufferende zu begrenzen.
    // Die Ausblendung verändert bewusst die letzten Samples jedes solchen Blocks.
    const BufferEndFadeFrames = 8;
    const fadeBufferEndToZero = (frames) => {
        const fadeLength = Math.min(BufferEndFadeFrames, frames.length);
        if (fadeLength < 2) {
            return;
        }
        for (let index = 0; index < fadeLength; index++) {
            const weight = (fadeLength - index - 1) / (fadeLength - 1);
            frames[frames.length - fadeLength + index] *= weight;
        }
    };
    const writeStreamingFrames = async (frames, rate, generation, onAccepted) => {
        const sink = streamingSink;
        if (!sink) {
            return false;
        }
        try {
            await sink.write(Float32Array.from(frames), microSharpPlayback ? onAccepted : undefined);
        }
        catch (error) {
            if (!microSharpPlayback || !(error instanceof Error) || error.name !== "MicroSharpPlaybackStartupError") {
                throw error;
            }
            if (generation !== scheduledGeneration) {
                return true;
            }
            sink.dispose();
            const fallback = await createLegacyStreamingSink(rate);
            if (generation !== scheduledGeneration) {
                fallback?.dispose();
                return true;
            }
            streamingSink = fallback;
            microSharpPlayback = false;
            recordCtxStateEvent("microsharp-sink-unavailable");
            if (!fallback) {
                return false;
            }
            fallback.clear(sink.routeLeadMs);
            fallback.bindInputRate(rate);
            await fallback.write(Float32Array.from(frames));
        }
        return true;
    };
    const processAndScheduleChunk = async (buffer, rate, generation, onAccepted) => {
        if (scheduledInputRate !== null && scheduledInputRate !== rate) {
            throw createPcmPlayerFailure("PCM_PLAYER_SAMPLE_RATE_CHANGED", "The PCM sample rate changed within an utterance.");
        }
        bindScheduledInputRate(rate);
        const dsp = ensureScheduledDsp();
        const dspNamespace = resolveOfflineDsp();
        const decoded = decodeInt16ToFloat(buffer);
        let processed;
        let scan;
        try {
            processed = dsp.process(decoded);
            if (processed.length === 0) {
                return;
            }
            const streamed = await writeStreamingFrames(processed, rate, generation, onAccepted);
            if (generation !== scheduledGeneration) {
                return;
            }
            if (!streamed) {
                // Nur einzeln geplante Puffer ausblenden. Der fortlaufende Worklet-Strom
                // braucht an internen Blockgrenzen keinen künstlichen Übergang zu null.
                fadeBufferEndToZero(processed);
            }
            scan = dspNamespace.scanSilentOutput(processed, ScheduledSilenceScanQuantumFrames, rate);
            if (streamed) {
                // Stillewerte auch für den Streamingpfad sammeln, bevor der Puffer weitergegeben wird.
                scheduledSilentOutputWindows += scan.silentOutputWindows;
                scheduledMaxContiguousSilentMs = Math.max(scheduledMaxContiguousSilentMs, scan.maxContiguousSilentMs);
                measureUtteranceOnset(processed);
                if (!microSharpPlayback) {
                    await onAccepted?.();
                }
                return;
            }
        }
        catch (error) {
            const detail = error instanceof Error ? error.message : "DSP processing failed";
            throw createPcmPlayerFailure("PCM_PLAYER_DSP_PROCESS_FAILED", `Chunk processing failed (${detail}).`);
        }
        scheduleProcessedBuffer(processed);
        scheduledSilentOutputWindows += scan.silentOutputWindows;
        if (scan.maxContiguousSilentMs > scheduledMaxContiguousSilentMs) {
            scheduledMaxContiguousSilentMs = scan.maxContiguousSilentMs;
        }
        await onAccepted?.();
    };
    const resetScheduledUtteranceState = () => {
        scheduledCursorArmed = false;
        scheduledNextStartTime = 0;
        scheduledWrittenSeconds = 0;
        scheduledPlayedOutSeconds = 0;
        scheduledUnderruns = 0;
        scheduledSilentOutputWindows = 0;
        scheduledMaxContiguousSilentMs = 0;
    };
    // Dauerwerte in Samples der Kontextrate umrechnen, damit emitPlaybackReport
    // sie mit derselben Rate wieder als Sekunden ausgeben kann.
    const emitScheduledPlaybackReport = (drainOutcome) => {
        const sampleRate = audioCtx && typeof audioCtx.sampleRate === "number" && audioCtx.sampleRate > 0
            ? audioCtx.sampleRate
            : 0;
        emitPlaybackReport(scheduledWrittenSeconds * sampleRate, scheduledPlayedOutSeconds * sampleRate, scheduledUnderruns, drainOutcome, {
            keepAlive: scheduledKeepAliveSource ? "on" : "off",
            silentOutputWindows: scheduledSilentOutputWindows,
            maxContiguousSilentMs: scheduledMaxContiguousSilentMs,
            outputSampleRate: sampleRate,
            playbackPath: "scheduled"
        });
    };
    // Spitzenpegel der ersten 2400 verarbeiteten Eingangssamples ermitteln.
    // Das entspricht bei 24 kHz den ersten 100 ms.
    const OnsetWindowFrames = 2400;
    let onsetFramesSeen = 0;
    let onsetPeak = 0;
    const measureUtteranceOnset = (processed) => {
        if (onsetFramesSeen >= OnsetWindowFrames) {
            return;
        }
        const take = Math.min(processed.length, OnsetWindowFrames - onsetFramesSeen);
        for (let index = 0; index < take; index++) {
            const magnitude = Math.abs(processed[index]);
            if (magnitude > onsetPeak) {
                onsetPeak = magnitude;
            }
        }
        onsetFramesSeen += take;
    };
    // writtenFrames zählt Samples der PCM-Eingangsrate, playedFrames solche der Kontextrate.
    // Vor dem gemeinsamen Bericht die geschriebenen Samples auf die Kontextrate umrechnen.
    const emitStreamingPlaybackReport = (drainOutcome) => {
        const sink = streamingSink;
        if (!sink) {
            return;
        }
        const contextRate = audioCtx && typeof audioCtx.sampleRate === "number" && audioCtx.sampleRate > 0
            ? audioCtx.sampleRate
            : 0;
        const rate = playbackSampleRate();
        const writtenFramesAtContextRate = contextRate > 0 && rate > 0
            ? (sink.writtenFrames / rate) * contextRate
            : 0;
        emitPlaybackReport(writtenFramesAtContextRate, sink.playedFrames, sink.underruns, drainOutcome, {
            keepAlive: scheduledKeepAliveSource ? "on" : "off",
            silentOutputWindows: scheduledSilentOutputWindows,
            maxContiguousSilentMs: scheduledMaxContiguousSilentMs,
            outputSampleRate: contextRate,
            playbackPath: "streaming",
            droppedFrames: sink.droppedFrames,
            starvedQuanta: sink.starvedQuanta,
            underrunOutputFrames: sink.underrunOutputFrames,
            onsetPeakDbfs: onsetFramesSeen > 0
                ? Math.round(20 * Math.log10(Math.max(onsetPeak, 1e-6)) * 10) / 10
                : null,
            onsetFrames: onsetFramesSeen
        });
    };
    // drain abschließen, wenn alle gezählten Quellen einschließlich Limiterrest
    // ihr onended gemeldet haben. Vorher den Bericht erzeugen.
    const maybeCompleteScheduledDrain = () => {
        if (!scheduledDrainRequested || scheduledOutstanding > 0) {
            return;
        }
        if (!scheduledDrainPromise || !scheduledDrainResolve) {
            return;
        }
        emitScheduledPlaybackReport("drained");
        const resolve = scheduledDrainResolve;
        clearScheduledDrainState();
        resolve();
    };
    const scheduledWarmup = async () => {
        scheduledKeepAlivePrimingRequested = true;
        await ensureScheduledContextAsync();
        ensureScheduledKeepAlive();
    };
    const scheduledInit = async (sampleRate) => {
        if (typeof sampleRate === "number" && sampleRate > 0) {
            pcmSampleRate = sampleRate;
        }
        ensureScheduledDsp();
        await ensurePlaybackOutputAsync();
        if (scheduledKeepAlivePrimingRequested) {
            ensureScheduledKeepAlive();
        }
    };
    const scheduledReset = async () => {
        ctxStateEvents = [];
        visibilityEvents = [];
        drainStartMs = 0;
        firstContentLeadMs = null;
        firstContentCtxState = null;
        recordCtxStateEvent("reset");
        stopAllScheduledSources();
        scheduledGeneration++;
        pendingEnqueues = Promise.resolve();
        scheduledInputRate = null;
        if (streamingSink) {
            // Verbleibenden Aufwachvorlauf an das Worklet übergeben und für diese Aktivierung als verwendet markieren.
            const streamingLeadMs = ScheduledFirstContentLeadMs + remainingFirstContentWakeMs();
            streamingSink.clear(streamingLeadMs);
            firstContentLeadMs = Math.round(streamingLeadMs);
            firstContentCtxState = audioCtx ? audioCtx.state : "closed";
            scheduledFirstContentScheduled = true;
        }
        if (scheduledDsp) {
            scheduledDsp.reset();
        }
        onsetFramesSeen = 0;
        onsetPeak = 0;
        resetScheduledUtteranceState();
        // Ein durch reset unterbrochenes drain regulär auflösen.
        if (scheduledDrainPromise && scheduledDrainResolve) {
            const resolve = scheduledDrainResolve;
            clearScheduledDrainState();
            resolve();
        }
        else {
            clearScheduledDrainState();
        }
        const ctx = audioCtx;
        if (ctx && ctx.state !== "closed") {
            await ensureScheduledContextRunningAsync(ctx, "reset");
        }
    };
    const scheduledEnqueue = async (pcm16leBytes, observer) => {
        const buffer = toPcmArrayBuffer(pcm16leBytes);
        if (!buffer || buffer.byteLength < 2) {
            return;
        }
        const rate = pcmSampleRate;
        const generation = scheduledGeneration;
        await ensurePlaybackOutputAsync();
        if (generation !== scheduledGeneration) {
            return;
        }
        if (scheduledKeepAlivePrimingRequested) {
            ensureScheduledKeepAlive();
        }
        const onAccepted = observer ? () => observer.invokeMethodAsync("OnAccepted") : undefined;
        const queued = pendingEnqueues.then(async () => {
            if (generation !== scheduledGeneration) {
                return;
            }
            await processAndScheduleChunk(buffer, rate, generation, onAccepted);
        });
        pendingEnqueues = queued.catch(() => { });
        await queued;
    };
    const scheduledDrain = async (observer) => {
        const generation = scheduledGeneration;
        await (playbackOutputInitPromise ?? ensureScheduledContextAsync());
        if (generation !== scheduledGeneration) {
            return;
        }
        await pendingEnqueues;
        if (generation !== scheduledGeneration) {
            return;
        }
        if (!audioCtx || audioCtx.state === "closed" || !masterGainNode) {
            throw createPcmPlayerFailure("PCM_PLAYER_GRAPH_INIT_FAILED", "The playback output graph is unavailable during drain.");
        }
        if (audioCtx.state !== "running") {
            noteContextNotRunning(audioCtx.state);
            throw createPcmPlayerFailure("AUDIO_CONTEXT_SUSPENDED", `AudioContext state is "${audioCtx.state}"; playback cannot drain.`);
        }
        if (scheduledDrainPromise) {
            return scheduledDrainPromise;
        }
        // Noch im Look-ahead-Puffer zurückgehaltene Samples ausgeben.
        // Dafür braucht auch der Streamingpfad einen Abschluss, obwohl er keine Quellen plant.
        if (scheduledDsp && scheduledInputRate !== null) {
            let tail;
            try {
                bindScheduledInputRate(pcmSampleRate);
                tail = ensureScheduledDsp().flush();
            }
            catch (error) {
                const detail = error instanceof Error ? error.message : "DSP flush failed";
                throw createPcmPlayerFailure("PCM_PLAYER_DSP_PROCESS_FAILED", `Limiter-tail flush failed (${detail}).`);
            }
            if (tail.length > 0) {
                if (streamingSink) {
                    // Am Ende des gesamten Audiostroms auch den letzten Rest auf null ausblenden.
                    fadeBufferEndToZero(tail);
                    const onAccepted = observer ? () => observer.invokeMethodAsync("OnAccepted") : undefined;
                    const streamed = await writeStreamingFrames(tail, playbackSampleRate(), generation, onAccepted);
                    if (generation !== scheduledGeneration) {
                        return;
                    }
                    if (!streamed) {
                        scheduleProcessedBuffer(tail);
                    }
                    if (!microSharpPlayback) {
                        await onAccepted?.();
                    }
                }
                else {
                    fadeBufferEndToZero(tail);
                    scheduleProcessedBuffer(tail);
                    await observer?.invokeMethodAsync("OnAccepted");
                }
                if (generation !== scheduledGeneration) {
                    return;
                }
            }
        }
        if (streamingSink) {
            const outstandingMs = Math.max(0, ((streamingSink.writtenFrames / Math.max(playbackSampleRate(), 1))
                - (streamingSink.playedFrames / Math.max(audioCtx.sampleRate, 1))) * 1000);
            drainStartMs = nowMs();
            // Neben der verbleibenden Audiodauer den bereits zugewiesenen Vorlauf einrechnen.
            // Eine erneute Berechnung der Aufwachfrist könnte nach ihrer Verwendung null liefern.
            const routeLeadBudgetMs = streamingSink.routeLeadMs;
            const outcome = await streamingSink.endOfStream(outstandingMs + routeLeadBudgetMs + ScheduledDrainWatchdogMarginMs);
            if (generation !== scheduledGeneration || outcome === "cancelled") {
                // Eine durch Unterbrechung beendete Ausgabe nicht als vollständig abgespielte
                // oder durch Zeitablauf verkürzte Äußerung melden.
                return;
            }
            // Regulären Abschluss und fehlende Abschlussmeldung bis zum Zeitlimit unterscheiden.
            emitStreamingPlaybackReport(outcome === "drained" ? "completed" : "watchdog");
            return;
        }
        drainStartMs = nowMs();
        const outstandingSeconds = Math.max(0, scheduledNextStartTime - audioCtx.currentTime);
        const ceilingMs = outstandingSeconds * 1000 + ScheduledDrainWatchdogMarginMs;
        const promise = new Promise((resolve, reject) => {
            scheduledDrainResolve = resolve;
            scheduledDrainReject = reject;
            scheduledDrainWatchdogTimer = setTimeout(() => {
                const reject2 = scheduledDrainReject;
                const state = audioCtx ? audioCtx.state : "closed";
                clearScheduledDrainState();
                reject2?.(createPcmPlayerFailure("PCM_PLAYER_DRAIN_TIMEOUT", `No scheduled play-out completion within ${Math.round(ceilingMs)} ms (context state "${state}").`));
            }, ceilingMs);
        });
        scheduledDrainPromise = promise;
        scheduledDrainRequested = true;
        maybeCompleteScheduledDrain();
        return promise;
    };
    window.pcmPlayer = {
        warmup: () => invokePcmPlayerOperation(scheduledWarmup),
        init: (sampleRate) => invokePcmPlayerOperation(() => scheduledInit(sampleRate)),
        reset: () => invokePcmPlayerOperation(scheduledReset),
        drain: (observer) => invokePcmPlayerOperation(() => scheduledDrain(observer)),
        enqueue: (pcm16leBytes, observer) => invokePcmPlayerOperation(() => scheduledEnqueue(pcm16leBytes, observer)),
        getVolume,
        setVolume,
        getMakeup,
        setMakeup: (value) => invokePcmPlayerOperation(() => {
            setMakeup(value);
            return Promise.resolve();
        }),
        getOutputLatencyMs
    };
})();
