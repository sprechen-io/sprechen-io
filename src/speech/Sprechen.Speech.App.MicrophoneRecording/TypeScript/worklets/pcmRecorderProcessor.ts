((): void => {
    "use strict";

    const toPositiveNumber = (value: unknown, fallback: number): number => {
        if (typeof value === "number" && Number.isFinite(value) && value > 0) {
            return value;
        }

        if (typeof value === "string") {
            const parsed = Number.parseFloat(value);
            if (Number.isFinite(parsed) && parsed > 0) {
                return parsed;
            }
        }

        return fallback;
    };

    // Der Tiefpass begrenzt Aliasing beim Herunterrechnen der Abtastrate.
    // Die Filterlänge muss dabei auch die für die Spracherkennung wichtigen hohen
    // Frequenzen im Durchlassbereich erhalten.
    const AntiAliasHalfLength = 48;

    // Durchlasskante als Bruchteil der Ausgangs-Nyquistfrequenz.
    const AntiAliasPassbandFraction = 0.95;

    const buildAntiAliasKernel = (ratio: number): Float32Array | null => {
        if (!(ratio > 1)) {
            // Bei gleicher oder niedrigerer Eingangsrate ist kein Anti-Alias-Tiefpass nötig.
            return null;
        }

        const length = AntiAliasHalfLength * 2 + 1;
        const kernel = new Float32Array(length);
        const cutoff = (AntiAliasPassbandFraction * 0.5) / ratio;
        let sum = 0;

        for (let i = 0; i < length; i++) {
            const n = i - AntiAliasHalfLength;
            const sinc = n === 0 ? 2 * cutoff : Math.sin(2 * Math.PI * cutoff * n) / (Math.PI * n);
            const window = 0.54 - 0.46 * Math.cos((2 * Math.PI * i) / (length - 1));
            const tap = sinc * window;
            kernel[i] = tap;
            sum += tap;
        }

        for (let i = 0; i < length; i++) {
            kernel[i] /= sum;
        }

        return kernel;
    };

    /** Filtert und resampelt den Mikrofonstrom zu Int16-Blöcken in der konfigurierten Zielrate. */
    class PcmRecorderProcessor extends AudioWorkletProcessor {
        private readonly _targetSampleRate: number;
        private readonly _ratio: number;
        private readonly _chunkSamples: number;
        private readonly _kernel: Float32Array | null;
        private readonly _history: Float32Array;

        private _historyIndex = 0;
        private _currentSample = 0;
        private _inputPosition = -1;
        private _nextOutputPosition = 0;
        private _bufferIndex = 0;
        private _buffer: Int16Array;
        private _probePosted = false;

        constructor(options?: AudioWorkletProcessorOptions) {
            super();

            const opts = options?.processorOptions ?? {};
            this._targetSampleRate = toPositiveNumber(opts.targetSampleRate, 16000);
            const chunkMs = toPositiveNumber(opts.chunkMs, 20);

            const inputSampleRate = sampleRate;
            this._ratio = inputSampleRate / this._targetSampleRate;

            this._chunkSamples = Math.max(1, Math.round(this._targetSampleRate * (chunkMs / 1000)));
            this._buffer = new Int16Array(this._chunkSamples);
            this._kernel = buildAntiAliasKernel(this._ratio);
            this._history = new Float32Array(this._kernel ? this._kernel.length : 1);
        }

        // Der Ringpuffer hält den Filterzustand über Verarbeitungsblöcke hinweg.
        // Die Auswertung erfolgt nur für benötigte Ausgangswerte. Zwei Schleifen
        // vermeiden eine Prüfung des Pufferendes bei jedem Filterkoeffizienten.
        private filterNewest(): number {
            const kernel = this._kernel as Float32Array;
            const history = this._history;
            const length = history.length;
            const start = this._historyIndex;
            const first = length - start;

            let sum = 0;
            for (let tap = 0; tap < first; tap++) {
                sum += kernel[tap] * history[start + tap];
            }
            for (let tap = first; tap < length; tap++) {
                sum += kernel[tap] * history[tap - first];
            }

            return sum;
        }

        process(inputs: Float32Array[][], _outputs: Float32Array[][], _parameters: Record<string, Float32Array>): boolean {
            const input = inputs[0];
            const channel = input?.[0];
            if (!channel || channel.length === 0) {
                return true;
            }

            const ratio = this._ratio;
            const kernel = this._kernel;
            const history = this._history;
            const historyLength = history.length;

            for (let position = 0; position < channel.length; position++) {
                const sample = channel[position];
                const previous = this._currentSample;
                let current = sample;
                // Jeder Eingangsabtastwert kommt in den Ringpuffer. Der Filter wird nur
                // für benötigte Ausgangswerte ausgewertet.
                let currentComputed = kernel === null;
                if (kernel !== null) {
                    history[this._historyIndex] = sample;
                    this._historyIndex = this._historyIndex + 1 === historyLength ? 0 : this._historyIndex + 1;
                }
                this._inputPosition++;

                // Ausgangswerte im abgeschlossenen Eingangsintervall interpolieren.
                // Der vorherige Wert bleibt auch über Verarbeitungsblöcke hinweg erhalten.
                while (this._nextOutputPosition <= this._inputPosition) {
                    const frac = this._nextOutputPosition - (this._inputPosition - 1);
                    if (!currentComputed) {
                        current = this.filterNewest();
                        currentComputed = true;
                    }

                    const v = frac === 1 ? current : (previous * (1 - frac)) + (current * frac);

                    const clamped = Math.max(-1, Math.min(1, v));
                    const s = clamped < 0 ? (clamped * 32768) : (clamped * 32767);
                    this._buffer[this._bufferIndex++] = s;

                    if (this._bufferIndex >= this._buffer.length) {
                        const out = this._buffer.buffer;
                        this.port.postMessage(out, [out]);
                        this._buffer = new Int16Array(this._chunkSamples);
                        this._bufferIndex = 0;

                        if (!this._probePosted) {
                            // Die konfigurierte Ausgangsrate einmal nach dem ersten Audioblock melden,
                            // damit der Hauptthread sie gegen die erwartete Rate prüfen kann.
                            this._probePosted = true;
                            this.port.postMessage({ kind: "outputSampleRate", value: this._targetSampleRate });
                        }
                    }

                    this._nextOutputPosition += ratio;
                }

                // Liegt der nächste Ausgang zwischen diesem und dem nächsten Eingang,
                // wird der gefilterte aktuelle Wert für die spätere Interpolation benötigt.
                if (!currentComputed && this._nextOutputPosition < this._inputPosition + 1) {
                    current = this.filterNewest();
                    currentComputed = true;
                }

                this._currentSample = current;
            }

            // Relative Positionen je Verarbeitungsblock zurücksetzen, damit lange
            // Sitzungen die Gleitkommagenauigkeit der Positionsvergleiche nicht verringern.
            this._nextOutputPosition -= this._inputPosition + 1;
            this._inputPosition = -1;

            return true;
        }
    }

    registerProcessor("pcm-recorder", PcmRecorderProcessor);
})();
