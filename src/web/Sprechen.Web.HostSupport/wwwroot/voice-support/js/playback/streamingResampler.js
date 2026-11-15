"use strict";
const linearInterpolator = (previous, current, fraction) => (previous * (1 - fraction)) + (current * fraction);
/**
 * Wandelt die Abtastrate eines stückweise eintreffenden Audiostroms. Leseposition
 * und Interpolationszustand bleiben zwischen Aufrufen erhalten, damit die
 * Verarbeitung nicht an jeder Abschnittsgrenze neu beginnt.
 * Diese Implementierung enthält keinen Anti-Aliasing-Filter.
 */
class StreamingResampler {
    constructor(inputRate, outputRate, interpolate = linearInterpolator) {
        this._previous = 0;
        this._current = 0;
        this._primed = false;
        // Der Ein-Sample-Puffer wird wiederverwendet, um Allokationen pro Sample zu vermeiden.
        this._frame = new Float32Array(1);
        // Relative Leseposition zwischen den zuletzt gelesenen Eingangssamples.
        // Ihr Bruchteil bleibt über die Aufrufe hinweg erhalten.
        this._position = 0;
        if (!(inputRate > 0) || !(outputRate > 0)) {
            throw new RangeError("StreamingResampler needs positive input and output rates.");
        }
        this._ratio = inputRate / outputRate;
        this._interpolate = interpolate;
    }
    get isPassThrough() {
        return this._ratio === 1;
    }
    /** Benötigte Eingangssamples für outputFrames unter Berücksichtigung der aktuellen Phase. */
    inputFramesFor(outputFrames) {
        const end = this._position + (outputFrames * this._ratio);
        return Math.max(0, Math.ceil(end) - Math.floor(this._position) + 1);
    }
    /**
     * Erzeugt Ausgangssamples in target. pull liefert weitere Eingangssamples.
     * Bei fehlender Eingabe endet der Aufruf. Der Rückgabewert nennt die Anzahl
     * der geschriebenen Ausgangssamples.
     */
    process(target, pull) {
        if (this.isPassThrough) {
            return pull(target);
        }
        let written = 0;
        while (written < target.length) {
            while (this._position >= 1 || !this._primed) {
                const filled = pull(this._frame);
                if (filled < 1) {
                    return written;
                }
                this._previous = this._current;
                this._current = this._frame[0];
                if (this._primed) {
                    this._position -= 1;
                }
                else {
                    this._primed = true;
                }
            }
            target[written] = this._interpolate(this._previous, this._current, this._position);
            written++;
            this._position += this._ratio;
        }
        return written;
    }
    /** Setzt Phase und Interpolationszustand für einen neuen Audiostrom zurück. */
    reset() {
        this._previous = 0;
        this._current = 0;
        this._primed = false;
        this._position = 0;
    }
}
globalThis.sprechenIoPlaybackDsp = {
    ...(globalThis.sprechenIoPlaybackDsp ?? {}),
    StreamingResampler,
    linearInterpolator
};
