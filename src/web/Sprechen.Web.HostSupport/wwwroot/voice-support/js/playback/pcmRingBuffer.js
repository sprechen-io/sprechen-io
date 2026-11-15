"use strict";
/**
 * Ringpuffer fester Kapazität für Mono-Audiosamples. Ein Schreiber und ein Leser
 * verwenden ihn innerhalb desselben Ausführungskontexts. Bei Platzmangel nimmt
 * write nur den passenden Teil an und überschreibt keine ungelesenen Samples.
 * Die feste Kapazität vermeidet ein Vergrößern während der Audiowiedergabe.
 */
class PcmRingBuffer {
    constructor(capacityFrames) {
        this._readIndex = 0;
        this._writeIndex = 0;
        this._available = 0;
        if (!Number.isFinite(capacityFrames) || capacityFrames < 1) {
            throw new RangeError("PcmRingBuffer capacity must be a positive frame count.");
        }
        this._capacity = Math.floor(capacityFrames);
        this._frames = new Float32Array(this._capacity);
    }
    get available() {
        return this._available;
    }
    get free() {
        return this._capacity - this._available;
    }
    get capacity() {
        return this._capacity;
    }
    /** Schreibt so viele Samples wie Platz haben und gibt ihre Anzahl zurück. Der Rest bleibt beim Aufrufer. */
    write(source) {
        // Am Pufferende wird die Kopie in höchstens zwei zusammenhängende Bereiche aufgeteilt.
        const count = Math.min(source.length, this.free);
        const firstSpan = Math.min(count, this._capacity - this._writeIndex);
        this._frames.set(source.subarray(0, firstSpan), this._writeIndex);
        if (count > firstSpan) {
            this._frames.set(source.subarray(firstSpan, count), 0);
        }
        this._writeIndex = (this._writeIndex + count) % this._capacity;
        this._available += count;
        return count;
    }
    /**
     * Liest die ältesten Samples in das Ziel und gibt ihre Anzahl zurück.
     * Bei Unterlauf entscheidet der Aufrufer, wie der übrige Zielbereich gefüllt wird.
     */
    read(target) {
        const count = Math.min(target.length, this._available);
        for (let index = 0; index < count; index++) {
            target[index] = this._frames[this._readIndex];
            this._readIndex = this._readIndex + 1 === this._capacity ? 0 : this._readIndex + 1;
        }
        this._available -= count;
        return count;
    }
    clear() {
        this._readIndex = 0;
        this._writeIndex = 0;
        this._available = 0;
    }
}
globalThis.sprechenIoPlaybackDsp = {
    ...(globalThis.sprechenIoPlaybackDsp ?? {}),
    PcmRingBuffer
};
