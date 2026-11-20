/**
 * Gemeinsamer Worklet-Namensraum, weil Ringpuffer und Resampler in eigenen Modulen geladen werden.
 */
type PlaybackDspNamespace = {
    PcmRingBuffer: new (capacityFrames: number) => {
        readonly available: number;
        readonly free: number;
        write(source: Float32Array): number;
        read(target: Float32Array): number;
        clear(): void;
    };
    StreamingResampler: new (inputRate: number, outputRate: number) => {
        readonly isPassThrough: boolean;
        process(target: Float32Array, pull: (into: Float32Array) => number): number;
        reset(): void;
    };
};

/** Nachrichten des Hauptthreads mit Generation zur Zuordnung der Äußerung. */
type PlaybackMessage =
    | { kind: "chunk"; generation: number; frames: Float32Array }
    | { kind: "clear"; generation: number; routeLeadMs: number }
    | { kind: "inputRate"; generation: number; rate: number }
    | { kind: "endOfStream"; generation: number };

const dsp = (globalThis as unknown as { sprechenIoPlaybackDsp?: PlaybackDspNamespace }).sprechenIoPlaybackDsp;

// Der Hauptthread übergibt die Ringpufferkapazität für seine Rückstaukontrolle.
// Ohne gültige Option gilt diese Vorgabe.
const FallbackRingCapacityFrames = 96000;

// Vor dem Start genügend Eingangssamples sammeln, um kurze Lieferpausen abzufangen.
const PrerollFrames = 2400;

// Vorlauf für die Audioausgabe zusätzlich zum gefüllten Puffer.
const DefaultFirstContentLeadMs = 100;


// Freien Pufferplatz in größeren Abständen melden, um den Nachrichtenverkehr zu begrenzen.
const CreditIntervalFrames = 4800;

/** Gibt den Audiostrom über einen wiederverwendeten Worklet-Knoten aus.
 * Ringpuffer und Resampler halten den Zustand über eingehende Audioblöcke hinweg.
 * Der Prozessor füllt die Ausgabe und meldet den Wiedergabefortschritt.
 */
class PcmPlaybackProcessor extends AudioWorkletProcessor {
    private readonly _ring: InstanceType<PlaybackDspNamespace["PcmRingBuffer"]>;
    private _resampler: InstanceType<PlaybackDspNamespace["StreamingResampler"]>;
    // Callback einmal anlegen, damit nicht je Verarbeitungsblock eine neue Funktion entsteht.
    private readonly _pull = (into: Float32Array): number => this._ring.read(into);
    private _playedFrames = 0;
    private _reportedFrames = 0;
    // Teilweise gefüllte Ausgabeblöcke als Unterlauf zählen. Vollständig leere Blöcke
    // werden getrennt in _starvedQuanta erfasst, solange das Datenende noch nicht gemeldet ist.
    private _underruns = 0;
    private _starvedQuanta = 0;
    private _endOfStream = false;
    private _drained = false;
    private _started = false;
    private _consumedSinceCredit = 0;
    private _gatedFrames = 0;
    // Der Hauptthread bestimmt bei clear den Vorlauf der Ausgaberoute,
    // weil nur dort der Zeitpunkt der Keep-alive-Aktivierung bekannt ist.
    private _requiredLeadFrames = 0;
    // Antworten tragen die Generation, damit der Hauptthread veraltete Äußerungen erkennt.
    private _generation = 0;

    constructor(options?: AudioWorkletNodeOptions) {
        super();
        if (!dsp) {
            throw new Error("sprechenIoPlaybackDsp must be added to the worklet before this processor.");
        }

        const inputRate = Number(options?.processorOptions?.inputSampleRate) || sampleRate;
        this._ring = new dsp.PcmRingBuffer(
            Number(options?.processorOptions?.ringCapacityFrames) || FallbackRingCapacityFrames);
        this._resampler = new dsp.StreamingResampler(inputRate, sampleRate);
        this._requiredLeadFrames = Math.round((DefaultFirstContentLeadMs / 1000) * sampleRate);
        this.port.onmessage = (event: MessageEvent<PlaybackMessage>) => this.handle(event.data);
    }

    private handle(message: PlaybackMessage): void {
        if (message.kind === "inputRate") {
            if (dsp && message.generation === this._generation && this._ring.available === 0 && this._playedFrames === 0) {
                this._resampler = new dsp.StreamingResampler(message.rate, sampleRate);
            }
            return;
        }

        this._generation = message.generation;

        if (message.kind === "chunk") {
            const taken = this._ring.write(message.frames);
            if (taken < message.frames.length) {
                // Vorhandenes Audio nicht überschreiben. Verlorene neue Samples dem Hauptthread melden.
                this.port.postMessage({
                    kind: "overflow",
                    generation: this._generation,
                    droppedFrames: message.frames.length - taken
                });
            }

            this._endOfStream = false;
            this._drained = false;
            return;
        }

        if (message.kind === "clear") {
            // Bei einer Unterbrechung ungespielte Samples und den Resamplerzustand verwerfen.
            this._ring.clear();
            this._resampler.reset();
            this._endOfStream = false;
            this._drained = true;
            // Zähler für die neue Äußerung zurücksetzen, damit Fortschrittswerte nicht kumulieren.
            this._started = false;
            this._gatedFrames = 0;
            const routeLeadMs = Number(message.routeLeadMs);
            this._requiredLeadFrames = Math.max(0, Math.round(
                ((Number.isFinite(routeLeadMs) ? routeLeadMs : DefaultFirstContentLeadMs) / 1000) * sampleRate));
            this._playedFrames = 0;
            this._reportedFrames = 0;
            this._underruns = 0;
            this._starvedQuanta = 0;
            this._consumedSinceCredit = 0;
            // Keine zusätzliche Platzmeldung: Der Hauptthread setzt seine Kapazitätsschätzung
            // bereits synchron zurück. Eine verzögerte Meldung könnte sie wieder überschreiben.
            return;
        }

        this._endOfStream = true;
    }

    process(_inputs: Float32Array[][], outputs: Float32Array[][]): boolean {
        const channel = outputs[0]?.[0];
        if (!channel) {
            return true;
        }

        // Start erst nach Vorpuffer und Vorlauf. Am Datenende darf der Restpuffer kürzer sein.
        if (!this._started) {
            // Das Datenende hebt nur die Mindestfüllung auf. Der Vorlauf der Ausgaberoute
            // gilt weiterhin, auch wenn bereits die gesamte Äußerung im Puffer liegt.
            const requiredLead = this._requiredLeadFrames;
            const waitingForBuffer = this._ring.available < PrerollFrames && !this._endOfStream;
            const waitingForRoute = this._gatedFrames < requiredLead;
            if (waitingForBuffer || waitingForRoute) {
                if (this._ring.available > 0) {
                    // Vorlauf erst zählen, sobald Audiodaten vorliegen, nicht schon in der Pause davor.
                    this._gatedFrames += channel.length;
                }

                for (let index = 0; index < channel.length; index++) {
                    channel[index] = 0;
                }

                return true;
            }

            this._started = true;
        }

        const availableBefore = this._ring.available;
        const written = this._resampler.process(channel, this._pull);
        this._consumedSinceCredit += availableBefore - this._ring.available;
        if (this._consumedSinceCredit >= CreditIntervalFrames) {
            this._consumedSinceCredit = 0;
            this.port.postMessage({ kind: "credit", generation: this._generation, free: this._ring.free });
        }

        // Fehlende Samples mit Stille auffüllen. Nur tatsächlich gelieferte Samples zählen.
        for (let index = written; index < channel.length; index++) {
            channel[index] = 0;
        }

        if (written < channel.length && !this._endOfStream) {
            if (written > 0) {
                this._underruns++;
            } else {
                this._starvedQuanta++;
            }
        }

        this._playedFrames += written;
        this.reportProgress(written);

        // Prozessor in Pausen weiterlaufen lassen, damit der Knoten wiederverwendbar bleibt.
        return true;
    }

    private reportProgress(written: number): void {
        if (this._endOfStream && !this._drained && written === 0 && this._ring.available === 0) {
            this._drained = true;
            this.port.postMessage({
                kind: "drained",
                generation: this._generation,
                playedFrames: this._playedFrames,
                underruns: this._underruns,
                starvedQuanta: this._starvedQuanta
            });
            return;
        }

        // Fortschritt etwa alle 250 ms melden, um den Nachrichtenverkehr zu begrenzen.
        if (this._playedFrames - this._reportedFrames >= sampleRate / 4) {
            this._reportedFrames = this._playedFrames;
            this.port.postMessage({
                kind: "progress",
                generation: this._generation,
                playedFrames: this._playedFrames,
                underruns: this._underruns,
                starvedQuanta: this._starvedQuanta
            });
        }
    }
}

registerProcessor("sprechen-pcm-playback", PcmPlaybackProcessor);
