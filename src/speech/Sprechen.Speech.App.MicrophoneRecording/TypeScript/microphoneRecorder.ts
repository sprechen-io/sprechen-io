/** Übergibt Audioblöcke und Fehlermeldungen an die Blazor-Sitzung. */
type DotNetCallback = {
    invokeMethodAsync(method: string, ...args: unknown[]): Promise<unknown>;
};

/** Vom Browser gemeldete Einstellungen der Mikrofonaufbereitung. */
type TrackProcessingSettings = {
    echoCancellation: boolean | null;
    noiseSuppression: boolean | null;
    autoGainControl: boolean | null;
};

/** Sitzungskennung und möglicher Fehler beim Anfordern des Mikrofons. */
type MicrophoneAcquisitionResult = {
    sessionId: string | null;
    errorName: string | null;
};

/** Globale Schnittstelle zum Verwalten von Mikrofonaufnahmen. */
type MicRecorderApi = {
    acquire(keepAliveEnabled: boolean): Promise<MicrophoneAcquisitionResult>;
    connect(sessionId: string, callbackRef: DotNetCallback, timesliceMs: number): Promise<string>;
    disconnect(sessionId: string): void;
    setMicGate(sessionId: string, enabled: boolean): void;
    release(sessionId: string): Promise<void>;
    isLive(sessionId: string): boolean;
    trackProcessing(sessionId: string): TrackProcessingSettings;
};

/** Globale Rekorderschnittstelle und vom Host bereitgestellte Audioressourcen. */
interface Window {
    micRecorder: MicRecorderApi;
    sprechenIoWeb?: {
        pcmRecorderWorkletUrl?: string;
        pcmPlaybackWorkletUrls?: string[];
        earconUrls?: Record<string, string>;
    };
}

((): void => {
    "use strict";

    const TargetSampleRate = 16000;

    // Die Sprachbeginn-Erkennung nutzt eine absolute Pegelschwelle.
    // Echo-Unterdrückung, Rauschunterdrückung und automatische Verstärkung anfordern,
    // um die dafür erwartete Mikrofonaufbereitung zu erhalten.
    const micConstraints: MediaStreamConstraints = Object.freeze({
        audio: {
            channelCount: 1,
            echoCancellation: true,
            noiseSuppression: true,
            autoGainControl: true
        }
    });

    /** Hält die Mikrofonspur und ihre Audioknoten bis zur gemeinsamen Freigabe. */
    type RecorderHandles = {
        audioCtx: AudioContext | null;
        keepAliveGain: GainNode | null;
        keepAliveOscillator: OscillatorNode | null;
        keepAliveOscillatorStarted: boolean;
        stream: MediaStream;
        sourceNode: MediaStreamAudioSourceNode | null;
        workletNode: AudioWorkletNode | null;
        dotNetRef: DotNetCallback | null;
    };

    const sessions = new Map<string, RecorderHandles>();
    const createSessionId = (): string => {
        const bytes = new Uint8Array(16);
        crypto.getRandomValues(bytes);
        return Array.from(bytes, (value) => value.toString(16).padStart(2, "0")).join("");
    };
    let workletModuleUrl: string | null = null;

    /**
     * Konstruktor von AudioContext, weil Ponyfill und nativer Kontext nicht austauschbar sind.
     */
    type AudioContextCtor = new (options?: AudioContextOptions) => AudioContext;
    /**
     * Konstruktor von AudioWorkletNode, weil der Ponyfill-Kontext den nativen Knoten ablehnt.
     */
    type AudioWorkletNodeCtor = new (context: BaseAudioContext, name: string, options?: AudioWorkletNodeOptions) => AudioWorkletNode;

    // Kontext und Worklet-Knoten möglichst aus demselben Kompatibilitätspaket beziehen.
    // Ohne Paket die verfügbaren nativen Konstruktoren verwenden.
    const resolveStandardizedAudioContext = (): { AudioContext?: AudioContextCtor; AudioWorkletNode?: AudioWorkletNodeCtor } | undefined =>
        (window as unknown as { standardizedAudioContext?: { AudioContext?: AudioContextCtor; AudioWorkletNode?: AudioWorkletNodeCtor } }).standardizedAudioContext;

    const resolveAudioContextCtor = (): AudioContextCtor | undefined => {
        const ponyfill = resolveStandardizedAudioContext();
        if (ponyfill && typeof ponyfill.AudioContext === "function") {
            return ponyfill.AudioContext;
        }

        return window.AudioContext || (window as unknown as { webkitAudioContext?: AudioContextCtor }).webkitAudioContext;
    };

    const resolveAudioWorkletNodeCtor = (): AudioWorkletNodeCtor | undefined => {
        const ponyfill = resolveStandardizedAudioContext();
        if (ponyfill && typeof ponyfill.AudioWorkletNode === "function") {
            return ponyfill.AudioWorkletNode;
        }

        return (window as unknown as { AudioWorkletNode?: AudioWorkletNodeCtor }).AudioWorkletNode;
    };

    const getErrorMessage = (err: unknown): string =>
        err && typeof err === "object" && "message" in err ? String((err as { message: unknown }).message) : String(err);

    const getErrorName = (err: unknown): string =>
        err && typeof err === "object" && "name" in err ? String((err as { name: unknown }).name) : "Error";

    const isDisposedDotNetCallbackError = (err: unknown): boolean => {
        const message = getErrorMessage(err);
        return message.includes("There is no tracked object with id")
            || message.includes("DotNetObjectReference instance was already disposed");
    };

    const disconnectCaptureGraph = (handles: RecorderHandles | null, failures?: unknown[]): void => {
        if (!handles) {
            return;
        }

        try {
            handles.sourceNode?.disconnect();
            handles.sourceNode = null;
        } catch (err) {
            failures?.push(err);
        }

        try {
            if (handles.workletNode) {
                handles.workletNode.port.onmessage = null;
                handles.workletNode.disconnect();
            }
            handles.workletNode = null;
        } catch (err) {
            failures?.push(err);
        }

        try {
            for (const track of handles.stream.getAudioTracks()) {
                track.onended = null;
            }
        } catch (err) {
            failures?.push(err);
        }

        if (handles.audioCtx) {
            handles.audioCtx.onstatechange = null;
        }
    };

    const fullRelease = async (handles: RecorderHandles | null): Promise<void> => {
        const failures: unknown[] = [];
        disconnectCaptureGraph(handles, failures);

        if (handles) {
            handles.dotNetRef = null;
            let tracks: MediaStreamTrack[] = [];
            try {
                tracks = handles.stream.getTracks();
            } catch (err) {
                failures.push(err);
            }

            for (const track of tracks) {
                if (track.readyState !== "ended") {
                    try {
                        track.stop();
                    } catch (err) {
                        failures.push(err);
                    }
                }
            }

            const oscillator = handles.keepAliveOscillator;
            const gain = handles.keepAliveGain;
            if (oscillator && handles.keepAliveOscillatorStarted) {
                try {
                    oscillator.stop();
                    handles.keepAliveOscillatorStarted = false;
                } catch (err) {
                    failures.push(err);
                }
            }

            try {
                oscillator?.disconnect();
                handles.keepAliveOscillator = null;
            } catch (err) {
                failures.push(err);
            }

            try {
                gain?.disconnect();
                handles.keepAliveGain = null;
            } catch (err) {
                failures.push(err);
            }

            const closingContext = handles.audioCtx;
            if (closingContext && closingContext.state !== "closed") {
                try {
                    await closingContext.close();
                } catch (err) {
                    failures.push(err);
                }
            }
        }

        if (failures.length > 0) {
            const error = new Error("Microphone release did not complete.");
            (error as Error & { errors: readonly unknown[] }).errors = failures;
            throw error;
        }
    };

    const releaseStaleDotNetCallback = (
        handles: RecorderHandles,
        callback: DotNetCallback | null,
        err: unknown): boolean => {
        if (!callback || !isDisposedDotNetCallbackError(err)) {
            return false;
        }

        if (handles.dotNetRef !== callback) {
            return true;
        }

        handles.dotNetRef = null;
        disconnectCaptureGraph(handles);
        return true;
    };

    const invokeDotNetAsync = async (
        handles: RecorderHandles,
        callback: DotNetCallback | null,
        method: string,
        ...args: unknown[]): Promise<boolean> => {
        if (!callback) {
            return false;
        }

        try {
            await callback.invokeMethodAsync(method, ...args);
            return true;
        } catch (err) {
            if (releaseStaleDotNetCallback(handles, callback, err)) {
                return false;
            }

            throw err;
        }
    };

    const reportCallbackError = (handles: RecorderHandles, callback: DotNetCallback | null, err: unknown): void => {
        void invokeDotNetAsync(handles, callback, "OnError", getErrorMessage(err)).catch(() => {
            // Einen Fehler beim Melden des ursprünglichen Fehlers nicht erneut weiterreichen.
        });
    };

    const ensureSupported = (): void => {
        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            throw new Error("Microphone capture is not supported in this browser (missing getUserMedia).");
        }

        if (!resolveAudioContextCtor()) {
            throw new Error("Microphone capture is not supported in this browser (missing AudioContext).");
        }

        if (!resolveAudioWorkletNodeCtor()) {
            throw new Error("Microphone capture is not supported in this browser (missing AudioWorklet).");
        }
    };

    const createAudioContext = (): AudioContext => {
        const audioContextCtor = resolveAudioContextCtor();
        if (!audioContextCtor) {
            throw new Error("Microphone capture is not supported in this browser (missing AudioContext).");
        }

        try {
            // Die Standardrate des AudioContext verwenden, um Unterschiede zur
            // Mikrofonquelle zu vermeiden. Auf 16 kHz rechnet erst das Worklet um.
            return new audioContextCtor({ latencyHint: "interactive" });
        } catch {
            return new audioContextCtor();
        }
    };

    const acquireStream = async (): Promise<MediaStream> =>
        navigator.mediaDevices.getUserMedia(micConstraints);

    const resolveWorkletModuleUrl = (): string => {
        if (workletModuleUrl) {
            return workletModuleUrl;
        }

        const configuredPath = window.sprechenIoWeb?.pcmRecorderWorkletUrl;
        if (typeof configuredPath !== "string" || configuredPath.length === 0) {
            throw new Error("WORKLET_URL_NOT_CONFIGURED: window.sprechenIoWeb.pcmRecorderWorkletUrl is not set; the host document must configure the recorder worklet URL.");
        }

        workletModuleUrl = new URL(configuredPath, document.baseURI).toString();
        return workletModuleUrl;
    };

    const ensureWorkletLoaded = async (ctx: AudioContext): Promise<void> => {
        const moduleUrl = resolveWorkletModuleUrl();
        await ctx.audioWorklet.addModule(moduleUrl);
    };

    const createHandles = async (keepAliveEnabled: boolean): Promise<{
        handles: RecorderHandles | null;
        errorName: string | null;
    }> => {
        let handles: RecorderHandles | null = null;
        try {
            ensureSupported();
            resolveWorkletModuleUrl();
            const stream = await acquireStream();
            handles = {
                audioCtx: null,
                keepAliveGain: null,
                keepAliveOscillator: null,
                keepAliveOscillatorStarted: false,
                stream,
                sourceNode: null,
                workletNode: null,
                dotNetRef: null
            };
            const audioCtx = createAudioContext();
            handles.audioCtx = audioCtx;
            if (audioCtx.state === "suspended") {
                await audioCtx.resume();
            }

            if (keepAliveEnabled) {
                handles.keepAliveGain = audioCtx.createGain();
                handles.keepAliveGain.gain.value = 0;
                handles.keepAliveGain.connect(audioCtx.destination);

                handles.keepAliveOscillator = audioCtx.createOscillator();
                handles.keepAliveOscillator.frequency.value = 440;
                handles.keepAliveOscillator.connect(handles.keepAliveGain);
                handles.keepAliveOscillator.start();
                handles.keepAliveOscillatorStarted = true;
            }

            await ensureWorkletLoaded(audioCtx);
            return { handles, errorName: null };
        } catch (err) {
            if (handles) {
                try {
                    await fullRelease(handles);
                    handles = null;
                } catch {
                    // Die unfertige Sitzung bleibt adressierbar, damit C# die Freigabe erneut versuchen kann.
                }
            }

            return { handles, errorName: getErrorName(err) };
        }
    };

    const attachCaptureGraph = (handles: RecorderHandles, callbackRef: DotNetCallback, chunkMs: number): void => {
        const WorkletNodeCtor = resolveAudioWorkletNodeCtor();
        if (!WorkletNodeCtor) {
            throw new Error("Microphone capture is not supported in this browser (missing AudioWorklet).");
        }

        const audioCtx = handles.audioCtx;
        if (!audioCtx) {
            throw new Error("MIC_SESSION_INCOMPLETE");
        }

        handles.dotNetRef = callbackRef;
        const reportEnded = (reason: string): void => {
            void invokeDotNetAsync(handles, handles.dotNetRef, "OnBrowserSessionEnded", reason)
                .catch(err => reportCallbackError(handles, handles.dotNetRef, err));
        };
        for (const track of handles.stream.getAudioTracks()) {
            track.onended = () => reportEnded("MICROPHONE_TRACK_ENDED");
        }
        audioCtx.onstatechange = () => {
            if (audioCtx.state === "closed") {
                reportEnded("MICROPHONE_AUDIO_CONTEXT_CLOSED");
            } else if (audioCtx.state !== "running") {
                reportEnded("MICROPHONE_AUDIO_CONTEXT_NOT_RUNNING");
            }
        };
        handles.sourceNode = audioCtx.createMediaStreamSource(handles.stream);
        handles.workletNode = new WorkletNodeCtor(audioCtx, "pcm-recorder", {
            numberOfInputs: 1,
            numberOfOutputs: 0,
            channelCount: 1,
            processorOptions: {
                targetSampleRate: TargetSampleRate,
                chunkMs
            }
        });

        handles.workletNode.port.onmessage = (e: MessageEvent<unknown>) => {
            try {
                const callback = handles.dotNetRef;
                if (!callback) {
                    return;
                }

                const data = e.data;

                // Das Worklet meldet nach dem ersten Audioblock seine konfigurierte Zielrate.
                // Abweichungen an .NET melden, damit der PCM-Strom nicht mit falscher Rate interpretiert wird.
                if (data && typeof data === "object" && !(data instanceof ArrayBuffer)) {
                    const message = data as { kind?: unknown; value?: unknown };
                    if (message.kind === "outputSampleRate") {
                        const value = typeof message.value === "number" ? message.value : Number.NaN;
                        if (value !== TargetSampleRate) {
                            void invokeDotNetAsync(handles, callback, "OnError", `WORKLET_OUTPUT_SAMPLE_RATE_INVALID: expected=${TargetSampleRate} actual=${value}`)
                                .catch(err => {
                                    reportCallbackError(handles, callback, err);
                                });
                        }
                    }
                    return;
                }

                if (!(data instanceof ArrayBuffer) || data.byteLength === 0) {
                    return;
                }

                void invokeDotNetAsync(handles, callback, "OnChunk", new Uint8Array(data))
                    .catch(err => {
                        reportCallbackError(handles, callback, err);
                    });
            } catch (err) {
                reportCallbackError(handles, handles.dotNetRef, err);
            }
        };

        handles.sourceNode.connect(handles.workletNode);
    };

    const acquire = async (keepAliveEnabled: boolean): Promise<MicrophoneAcquisitionResult> => {
        const result = await createHandles(keepAliveEnabled === true);
        if (result.handles) {
            const sessionId = createSessionId();
            sessions.set(sessionId, result.handles);
            return { sessionId, errorName: result.errorName };
        }

        return { sessionId: null, errorName: result.errorName };
    };

    const requireSession = (sessionId: string): RecorderHandles => {
        const handles = sessions.get(sessionId);
        if (!handles) {
            throw new Error(`MIC_SESSION_NOT_FOUND: ${sessionId}`);
        }

        return handles;
    };

    const connect = async (
        sessionId: string,
        callbackRef: DotNetCallback,
        timesliceMs: number): Promise<string> => {
        if (!callbackRef) {
            throw new Error("dotNetRef is required.");
        }

        const handles = requireSession(sessionId);
        attachCaptureGraph(handles, callbackRef, timesliceMs);
        if (!handles.audioCtx
            || handles.audioCtx.state !== "running"
            || !handles.stream.getAudioTracks().some(track => track.readyState === "live")) {
            throw new Error("MIC_SESSION_NOT_LIVE");
        }

        return `audio/pcm;rate=${TargetSampleRate}`;
    };

    const disconnect = (sessionId: string): void => {
        const handles = sessions.get(sessionId);
        if (!handles) {
            return;
        }

        const failures: unknown[] = [];
        disconnectCaptureGraph(handles, failures);
        if (failures.length > 0) {
            const error = new Error("Microphone disconnect did not complete.");
            (error as Error & { errors: readonly unknown[] }).errors = failures;
            throw error;
        }

        handles.dotNetRef = null;
    };

    const release = async (sessionId: string): Promise<void> => {
        const handles = sessions.get(sessionId) ?? null;
        await fullRelease(handles);
        sessions.delete(sessionId);
    };

    const isLive = (sessionId: string): boolean => {
        const handles = sessions.get(sessionId);
        return !!handles
            && !!handles.audioCtx
            && handles.audioCtx.state === "running"
            && handles.stream.getAudioTracks().some(track => track.readyState === "live");
    };

    const asBool = (value: unknown): boolean | null => typeof value === "boolean" ? value : null;

    const trackProcessing = (sessionId: string): TrackProcessingSettings => {
        const unknown: TrackProcessingSettings = {
            echoCancellation: null,
            noiseSuppression: null,
            autoGainControl: null
        };
        const track = sessions.get(sessionId)?.stream.getAudioTracks()[0];
        if (!track || typeof track.getSettings !== "function") {
            return unknown;
        }

        try {
            const settings = track.getSettings();
            return {
                echoCancellation: asBool(settings.echoCancellation),
                noiseSuppression: asBool(settings.noiseSuppression),
                autoGainControl: asBool(settings.autoGainControl)
            };
        } catch {
            return unknown;
        }
    };

    const setMicGate = (sessionId: string, enabled: boolean): void => {
        const activeStream = sessions.get(sessionId)?.stream;
        if (!activeStream) {
            return;
        }

        for (const track of activeStream.getAudioTracks()) {
            track.enabled = enabled;
        }
    };

    window.micRecorder = {
        acquire,
        connect,
        disconnect,
        setMicGate,
        release,
        isLive,
        trackProcessing
    };
})();
