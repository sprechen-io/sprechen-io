"use strict";
// Zustandsbehaftete Audiosignalverarbeitung für die Wiedergabe: Ausgleichsverstärkung,
// Einblendung und Look-ahead-Begrenzer. Die gemeinsame API liegt auf globalThis
// und wird von klassischen Wiedergabeskripten und Laufzeittests verwendet.
(() => {
    "use strict";
    // Der Begrenzer hält PCM-Abtastwerte unter der konfigurierten Obergrenze.
    // -1,5 dBFS lassen Reserve.
    const DefaultLimiterLookaheadMs = 1.5;
    const DefaultLimiterReleaseMs = 40;
    const DefaultLimiterCeilingDbfs = -1.5;
    // Die Verstärkung steigt am Beginn einer Äußerung über wenige Millisekunden an.
    // Das vermindert den abrupten Übergang von Stille zum ersten verstärkten Abtastwert.
    const DefaultMakeupRampMs = 5;
    // Ein Scanfenster zählt als still, wenn sein größter absoluter Abtastwert
    // unter dieser Schwelle liegt.
    const DefaultSilentOutputPeakThreshold = 1e-4;
    /**
     * Verzögert Samples für die vorausschauende Pegelbegrenzung. Eine monoton
     * fallende Deque liefert das Maximum des gleitenden Eingangsfensters
     * in amortisiert konstanter Zeit. Float32-Rundung hält die Vergleichswerte
     * mit dem Audiopuffer konsistent. Abschließend wird der Ausgang begrenzt.
     */
    class LookaheadLimiter {
        constructor(sampleRate, lookaheadMs, releaseMs, ceilingDbfs) {
            this._index = 0;
            this._gain = 1.0;
            this._deqHead = 0;
            this._deqCount = 0;
            this._position = 0;
            const length = Math.max(1, Math.round(sampleRate * (lookaheadMs / 1000)));
            this._length = length;
            this._delay = new Float32Array(length);
            this._ceiling = Math.pow(10, ceilingDbfs / 20);
            const releaseSamples = Math.max(1, sampleRate * (releaseMs / 1000));
            this._releaseCoef = Math.exp(-1 / releaseSamples);
            this._deqValue = new Float64Array(length + 1);
            this._deqPosition = new Float64Array(length + 1);
        }
        get lookahead() {
            return this._length;
        }
        get ceiling() {
            return this._ceiling;
        }
        reset() {
            this._delay.fill(0);
            this._index = 0;
            this._gain = 1.0;
            this._deqHead = 0;
            this._deqCount = 0;
            this._position = 0;
        }
        step(input) {
            const capacity = this._deqValue.length;
            const length = this._length;
            const output = this._delay[this._index];
            this._delay[this._index] = input;
            this._index = this._index + 1 >= length ? 0 : this._index + 1;
            // Das Maximum verwendet dieselbe Float32-Genauigkeit wie der Audiopuffer.
            const abs = Math.fround(Math.abs(input));
            const position = this._position;
            this._position = position + 1;
            while (this._deqCount > 0) {
                let back = this._deqHead + this._deqCount - 1;
                if (back >= capacity) {
                    back -= capacity;
                }
                if (this._deqValue[back] <= abs) {
                    this._deqCount--;
                }
                else {
                    break;
                }
            }
            let tail = this._deqHead + this._deqCount;
            if (tail >= capacity) {
                tail -= capacity;
            }
            this._deqValue[tail] = abs;
            this._deqPosition[tail] = position;
            this._deqCount++;
            const expireAtOrBefore = position - length;
            while (this._deqPosition[this._deqHead] <= expireAtOrBefore) {
                this._deqHead = this._deqHead + 1 >= capacity ? 0 : this._deqHead + 1;
                this._deqCount--;
            }
            const peak = this._deqValue[this._deqHead];
            const target = peak > this._ceiling ? this._ceiling / peak : 1.0;
            // Die Verstärkung sinkt sofort. Ihre Rückkehr zum Zielwert wird geglättet.
            this._gain = target < this._gain
                ? target
                : target + (this._gain - target) * this._releaseCoef;
            const value = output * this._gain;
            if (value > this._ceiling) {
                return this._ceiling;
            }
            if (value < -this._ceiling) {
                return -this._ceiling;
            }
            return value;
        }
    }
    const toMakeupFactor = (value) => {
        if (typeof value === "number" && Number.isFinite(value) && value > 0) {
            return value;
        }
        if (typeof value === "string") {
            const parsed = Number.parseFloat(value);
            if (Number.isFinite(parsed) && parsed > 0) {
                return parsed;
            }
        }
        return 1.0;
    };
    /**
     * Verarbeitet Float32-Abschnitte mit Ausgleichsverstärkung, Einblendung und
     * Look-ahead-Begrenzung. Verzögerung, Verstärkung und Einblendposition bleiben
     * zwischen Aufrufen erhalten, sodass Abschnittsgrenzen die Verarbeitung nicht zurücksetzen.
     */
    class MakeupLimiterProcessor {
        constructor(sampleRate, options) {
            this._makeupRampPos = 0;
            const opts = options ?? {};
            this._makeup = toMakeupFactor(opts.makeup);
            const lookaheadMs = typeof opts.lookaheadMs === "number" && opts.lookaheadMs > 0 ? opts.lookaheadMs : DefaultLimiterLookaheadMs;
            const releaseMs = typeof opts.releaseMs === "number" && opts.releaseMs > 0 ? opts.releaseMs : DefaultLimiterReleaseMs;
            const ceilingDbfs = typeof opts.ceilingDbfs === "number" && Number.isFinite(opts.ceilingDbfs) ? opts.ceilingDbfs : DefaultLimiterCeilingDbfs;
            const makeupRampMs = typeof opts.makeupRampMs === "number" && opts.makeupRampMs > 0 ? opts.makeupRampMs : DefaultMakeupRampMs;
            this._makeupRampFrames = Math.max(1, Math.round(sampleRate * (makeupRampMs / 1000)));
            this._limiter = new LookaheadLimiter(sampleRate, lookaheadMs, releaseMs, ceilingDbfs);
        }
        // Look-ahead-Länge in Samples. flush führt entsprechend viele Nullen zu,
        // damit die verzögerten letzten Samples noch ausgegeben werden.
        get lookahead() {
            return this._limiter.lookahead;
        }
        get ceiling() {
            return this._limiter.ceiling;
        }
        setMakeup(value) {
            this._makeup = toMakeupFactor(value);
        }
        reset() {
            this._makeupRampPos = 0;
            this._limiter.reset();
        }
        // Wendet zuerst Verstärkung und Einblendung, dann den Begrenzer an.
        // Der Ausgang wird als neuer Float32Array zurückgegeben. Die Einblendposition
        // bleibt bis zum nächsten reset erhalten.
        process(input) {
            const makeup = this._makeup;
            const rampFrames = this._makeupRampFrames;
            const out = new Float32Array(input.length);
            for (let i = 0; i < input.length; i++) {
                let dry;
                if (this._makeupRampPos < rampFrames) {
                    dry = input[i] * makeup * (this._makeupRampPos / rampFrames);
                    this._makeupRampPos++;
                }
                else {
                    dry = input[i] * makeup;
                }
                out[i] = this._limiter.step(dry);
            }
            return out;
        }
        // Führt count Nullen zu (standardmäßig die Look-ahead-Länge),
        // um die noch verzögerten Samples auszugeben.
        flush(count) {
            const n = typeof count === "number" && count > 0 ? Math.floor(count) : this._limiter.lookahead;
            const out = new Float32Array(n);
            for (let i = 0; i < n; i++) {
                out[i] = this._limiter.step(0);
            }
            return out;
        }
    }
    // Untersucht den gerenderten Puffer in Fenstern von quantumFrames.
    // Fenster unterhalb der Amplitudenschwelle zählen als still. Zusammenhängende
    // stille Samples bestimmen die längste Dauer bei der angegebenen Abtastrate.
    const scanSilentOutput = (buffer, quantumFrames, sampleRate, threshold = DefaultSilentOutputPeakThreshold) => {
        const quantum = Number.isFinite(quantumFrames) && quantumFrames >= 1 ? Math.floor(quantumFrames) : 1;
        let silentOutputWindows = 0;
        let maxContiguousFrames = 0;
        let curContiguousFrames = 0;
        for (let start = 0; start < buffer.length; start += quantum) {
            const end = Math.min(start + quantum, buffer.length);
            let peak = 0;
            for (let i = start; i < end; i++) {
                const a = buffer[i] >= 0 ? buffer[i] : -buffer[i];
                if (a > peak) {
                    peak = a;
                }
            }
            const frames = end - start;
            if (peak < threshold) {
                silentOutputWindows++;
                curContiguousFrames += frames;
                if (curContiguousFrames > maxContiguousFrames) {
                    maxContiguousFrames = curContiguousFrames;
                }
            }
            else {
                curContiguousFrames = 0;
            }
        }
        const maxContiguousSilentMs = sampleRate > 0 ? (maxContiguousFrames / sampleRate) * 1000 : 0;
        return { silentOutputWindows, maxContiguousSilentMs };
    };
    const namespace = {
        LookaheadLimiter,
        MakeupLimiterProcessor,
        scanSilentOutput,
        SilentOutputPeakThreshold: DefaultSilentOutputPeakThreshold
    };
    globalThis.sprechenIoAudioDsp = namespace;
})();
