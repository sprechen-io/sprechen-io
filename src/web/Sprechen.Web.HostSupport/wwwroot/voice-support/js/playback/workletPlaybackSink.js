"use strict";
const ProcessorName = "sprechen-pcm-playback";
// Die Pufferkapazität wird an den Prozessor übergeben und hier für die Platzschätzung genutzt.
// 96.000 Frames entsprechen vier Sekunden bei 24 kHz.
const SinkRingCapacityFrames = 96000;
// Kontext und Knoten müssen dieselbe Web-Audio-Implementierung verwenden.
// Wenn standardized-audio-context vorhanden ist, wird dessen Knoten bevorzugt.
const resolveAudioWorkletNodeCtor = () => {
    const ponyfill = globalThis.standardizedAudioContext;
    if (ponyfill && typeof ponyfill.AudioWorkletNode === "function") {
        return ponyfill.AudioWorkletNode;
    }
    return globalThis.AudioWorkletNode;
};
const createWorkletPlaybackSink = async (context, destination, inputSampleRate, moduleUrls) => {
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
    const pending = [];
    let endOfStreamPending = false;
    let lastRouteLeadMs = 0;
    // Die Generation ordnet jede Nachricht ihrer Äußerung zu. Veraltete Platzmeldungen
    // dürfen nach clear die Schätzung für die neue Äußerung nicht überschreiben.
    let generation = 0;
    let estimatedFree = SinkRingCapacityFrames;
    let writtenFrames = 0;
    let playedFrames = 0;
    let underruns = 0;
    let starvedQuanta = 0;
    let droppedFrames = 0;
    let drainResolve = null;
    let drainTimer = null;
    const settleDrain = (outcome = "watchdog") => {
        if (drainTimer !== null) {
            clearTimeout(drainTimer);
            drainTimer = null;
        }
        const resolve = drainResolve;
        drainResolve = null;
        resolve?.(outcome);
    };
    node.port.onmessage = (event) => {
        const message = event.data;
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
    const pump = () => {
        while (pending.length > 0 && pending[0].length <= estimatedFree) {
            const frames = pending.shift();
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
        get writtenFrames() {
            return writtenFrames;
        },
        get playedFrames() {
            return playedFrames;
        },
        get underruns() {
            return underruns;
        },
        get starvedQuanta() {
            return starvedQuanta;
        },
        get droppedFrames() {
            return droppedFrames;
        },
        get routeLeadMs() {
            return lastRouteLeadMs;
        },
        write(frames) {
            writtenFrames += frames.length;
            pending.push(frames);
            pump();
        },
        clear(routeLeadMs) {
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
        endOfStream(timeoutMs) {
            if (drainResolve) {
                settleDrain("cancelled");
            }
            return new Promise((resolve) => {
                drainResolve = resolve;
                // Die Zeitüberschreitung liefert ein eigenes Ergebnis, falls keine Abschlussmeldung eintrifft.
                drainTimer = setTimeout(() => settleDrain("watchdog"), Math.max(0, timeoutMs));
                endOfStreamPending = true;
                pump();
            });
        },
        dispose() {
            settleDrain();
            node.port.onmessage = null;
            try {
                node.disconnect();
            }
            catch {
                // Ein bereits getrennter Knoten darf die Freigabe nicht unterbrechen.
            }
        }
    };
};
globalThis.sprechenIoPlaybackSink = {
    ...(globalThis.sprechenIoPlaybackSink ?? {}),
    createWorkletPlaybackSink
};
