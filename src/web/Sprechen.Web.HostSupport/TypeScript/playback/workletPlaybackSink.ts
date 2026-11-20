/** Fortschrittswerte der Wiedergabe für den Diagnosebericht. */
type PlaybackSinkProgress = {
    readonly writtenFrames: number;
    readonly playedFrames: number;
    readonly underruns: number;
    readonly starvedQuanta: number;
    readonly droppedFrames: number;
    /** Vorlauf der Äußerung für die Berechnung der Wiedergabewartezeit. */
    readonly routeLeadMs: number;
};

/**
 * Schnittstelle des Hauptthreads zum AudioWorklet: Samples übergeben,
 * Wiedergabe zurücksetzen, das Streamende melden und Fortschritt lesen.
 * Knotenverwaltung und Nachrichtenübertragung bleiben hinter dieser Schnittstelle.
 */
type PlaybackSink = PlaybackSinkProgress & {
    /** Übergibt Samples an den Audiothread und überträgt deren Pufferbesitz. */
    write(frames: Float32Array): void;
    bindInputRate(rate: number): void;
    /**
     * Verwirft bisherige Wiedergabedaten und beginnt eine neue Äußerung.
     * routeLeadMs gibt die vom Aufrufer bestimmte Vorlaufzeit des Audioausgangs an.
     */
    clear(routeLeadMs: number): void;
    /**
     * Meldet das Streamende. Das Ergebnis unterscheidet vollständige Ausgabe,
     * Zeitüberschreitung und Abbruch, damit diese Fälle getrennt bewertet werden.
     */
    endOfStream(timeoutMs: number): Promise<PlaybackDrainOutcome>;
    /** Gibt den AudioWorklet-Knoten frei. Anschließend ist die Ausgabe nicht mehr nutzbar. */
    dispose(): void;
};

/** Grund für das Ende der Wartezeit auf die Wiedergabe. */
type PlaybackDrainOutcome = "drained" | "watchdog" | "cancelled";

/** Stellt die Erzeugungsfunktion für klassische Skripte auf globalThis bereit. */
type WorkletPlaybackSinkGlobal = {
    createWorkletPlaybackSink: typeof createWorkletPlaybackSink;
};

const ProcessorName = "sprechen-pcm-playback";

// Die Pufferkapazität wird an den Prozessor übergeben und hier für die Platzschätzung genutzt.
// 96.000 Frames entsprechen vier Sekunden bei 24 kHz.
const SinkRingCapacityFrames = 96000;

/** Konstruktorschnittstelle für einen zum Audiokontext passenden AudioWorkletNode. */
type AudioWorkletNodeCtor = new (
    context: BaseAudioContext,
    name: string,
    options?: AudioWorkletNodeOptions) => AudioWorkletNode;

// Kontext und Knoten müssen dieselbe Web-Audio-Implementierung verwenden.
// Wenn standardized-audio-context vorhanden ist, wird dessen Knoten bevorzugt.
const resolveAudioWorkletNodeCtor = (): AudioWorkletNodeCtor | undefined => {
    const ponyfill = (globalThis as unknown as {
        standardizedAudioContext?: { AudioWorkletNode?: AudioWorkletNodeCtor };
    }).standardizedAudioContext;
    if (ponyfill && typeof ponyfill.AudioWorkletNode === "function") {
        return ponyfill.AudioWorkletNode;
    }

    return (globalThis as unknown as { AudioWorkletNode?: AudioWorkletNodeCtor }).AudioWorkletNode;
};

const createWorkletPlaybackSink = async (
    context: BaseAudioContext,
    destination: AudioNode,
    inputSampleRate: number,
    moduleUrls: readonly string[]): Promise<PlaybackSink> => {
    if (!context.audioWorklet || typeof context.audioWorklet.addModule !== "function") {
        throw new Error("AudioWorklet is unavailable.");
    }

    // Ringpuffer und Resampler werden vor dem Prozessor im gemeinsamen
    // AudioWorkletGlobalScope geladen.
    for (const url of moduleUrls) {
        await context.audioWorklet.addModule(url);
    }

    const AudioWorkletNodeCtor = resolveAudioWorkletNodeCtor();
    if (!AudioWorkletNodeCtor) {
        throw new Error("AudioWorkletNode is unavailable.");
    }

    const node = new AudioWorkletNodeCtor(context, ProcessorName, {
        numberOfInputs: 0,
        numberOfOutputs: 1,
        outputChannelCount: [1],
        processorOptions: { inputSampleRate, ringCapacityFrames: SinkRingCapacityFrames }
    });

    // Noch nicht übertragene Samples warten im Hauptthread, bis der Prozessor freien Platz meldet.
    // So muss der begrenzte Ringpuffer überschüssige Abschnitte nicht sofort verwerfen.
    const pending: Float32Array[] = [];
    let endOfStreamPending = false;
    let lastRouteLeadMs = 0;
    // Die Generation ordnet jede Nachricht ihrer Äußerung zu. Veraltete Platzmeldungen
    // dürfen nach clear die Schätzung für die neue Äußerung nicht überschreiben.
    let generation = 0;
    let boundInputRate = inputSampleRate;
    let estimatedFree = SinkRingCapacityFrames;
    let writtenFrames = 0;
    let playedFrames = 0;
    let underruns = 0;
    let starvedQuanta = 0;
    let droppedFrames = 0;
    let drainResolve: ((outcome: PlaybackDrainOutcome) => void) | null = null;
    let drainTimer: number | null = null;

    const settleDrain = (outcome: PlaybackDrainOutcome = "watchdog"): void => {
        if (drainTimer !== null) {
            clearTimeout(drainTimer);
            drainTimer = null;
        }

        const resolve = drainResolve;
        drainResolve = null;
        resolve?.(outcome);
    };

    node.port.onmessage = (event: MessageEvent): void => {
        const message = event.data as {
            kind?: string;
            generation?: number;
            playedFrames?: number;
            underruns?: number;
            starvedQuanta?: number;
            droppedFrames?: number;
            free?: number;
        };
        if (message?.generation !== generation) {
            return;
        }

        if (typeof message?.playedFrames === "number") {
            playedFrames = message.playedFrames;
        }

        if (typeof message?.underruns === "number") {
            underruns = message.underruns;
        }

        if (typeof message?.starvedQuanta === "number") {
            starvedQuanta = message.starvedQuanta;
        }

        if (typeof message?.free === "number") {
            estimatedFree = message.free;
            pump();
        }

        if (message?.kind === "overflow" && typeof message.droppedFrames === "number") {
            // Verworfene Frames werden gezählt, damit ein Pufferüberlauf im Bericht sichtbar bleibt.
            droppedFrames += message.droppedFrames;
            return;
        }

        if (message?.kind === "drained") {
            settleDrain("drained");
        }
    };

    // Überträgt jeweils einen ganzen Abschnitt, sobald dafür ausreichend Platz gemeldet ist.
    const pump = (): void => {
        while (pending.length > 0 && pending[0].length <= estimatedFree) {
            const frames = pending.shift() as Float32Array;
            estimatedFree -= frames.length;
            node.port.postMessage({ kind: "chunk", generation, frames }, [frames.buffer]);
        }

        // Streamende erst nach dem letzten gepufferten Abschnitt melden, damit spätere
        // Audiodaten die Abschlussmeldung des Prozessors nicht wieder aufheben.
        if (endOfStreamPending && pending.length === 0) {
            endOfStreamPending = false;
            node.port.postMessage({ kind: "endOfStream", generation });
        }
    };

    node.connect(destination);

    return {
        get writtenFrames(): number {
            return writtenFrames;
        },
        get playedFrames(): number {
            return playedFrames;
        },
        get underruns(): number {
            return underruns;
        },
        get starvedQuanta(): number {
            return starvedQuanta;
        },
        get droppedFrames(): number {
            return droppedFrames;
        },
        get routeLeadMs(): number {
            return lastRouteLeadMs;
        },
        bindInputRate(rate: number): void {
            if (rate !== boundInputRate) {
                node.port.postMessage({ kind: "inputRate", generation, rate });
                boundInputRate = rate;
            }
        },
        write(frames: Float32Array): void {
            writtenFrames += frames.length;
            pending.push(frames);
            pump();
        },
        clear(routeLeadMs: number): void {
            lastRouteLeadMs = routeLeadMs;
            // Gepufferte Samples gehören zur abgebrochenen Äußerung.
            pending.length = 0;
            endOfStreamPending = false;
            estimatedFree = SinkRingCapacityFrames;
            writtenFrames = 0;
            playedFrames = 0;
            underruns = 0;
            starvedQuanta = 0;
            droppedFrames = 0;
            generation++;
            node.port.postMessage({ kind: "clear", generation, routeLeadMs });
            // Eine unterbrochene Wiedergabe endet als cancelled, nicht als Zeitüberschreitung.
            settleDrain("cancelled");
            settleDrain();
        },
        endOfStream(timeoutMs: number): Promise<PlaybackDrainOutcome> {
            if (drainResolve) {
                settleDrain("cancelled");
            }

            return new Promise<PlaybackDrainOutcome>((resolve) => {
                drainResolve = resolve;
                // Die Zeitüberschreitung liefert ein eigenes Ergebnis, falls keine Abschlussmeldung eintrifft.
                drainTimer = setTimeout(() => settleDrain("watchdog"), Math.max(0, timeoutMs)) as unknown as number;
                endOfStreamPending = true;
                pump();
            });
        },
        dispose(): void {
            settleDrain();
            node.port.onmessage = null;
            try {
                node.disconnect();
            } catch {
                // Ein bereits getrennter Knoten darf die Freigabe nicht unterbrechen.
            }
        }
    };
};

(globalThis as unknown as { sprechenIoPlaybackSink?: Partial<WorkletPlaybackSinkGlobal> }).sprechenIoPlaybackSink = {
    ...((globalThis as unknown as { sprechenIoPlaybackSink?: Partial<WorkletPlaybackSinkGlobal> }).sprechenIoPlaybackSink ?? {}),
    createWorkletPlaybackSink
};
