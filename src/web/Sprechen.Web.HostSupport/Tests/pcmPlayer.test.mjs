import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import { runInNewContext } from "node:vm";

const playerSource = readFileSync(
    new URL("../wwwroot/voice-support/js/pcmPlayer.js", import.meta.url), "utf8");

function gate() {
    let open;
    const promise = new Promise(resolve => { open = resolve; });
    return { promise, open };
}

function fixture(useWorklet) {
    const resume = gate();
    const workletStarted = gate();
    const workletReady = gate();
    const dspRates = [];
    const scanRates = [];
    const output = [];

    class AudioContext {
        state = "suspended";
        sampleRate = 44100;
        currentTime = 0;
        destination = {};

        async resume() {
            await resume.promise;
            this.state = "running";
        }
        createGain() {
            return { gain: { value: 1 }, connect() {} };
        }
        createBuffer(_channels, frames, rate) {
            const samples = new Float32Array(frames);
            return { duration: frames / rate, rate, getChannelData: () => samples };
        }
        createBufferSource() {
            return {
                connect() {}, disconnect() {}, stop() {},
                start() {
                    output.push({ route: "scheduled", rate: this.buffer.rate,
                        sample: this.buffer.getChannelData()[0], duration: this.buffer.duration });
                }
            };
        }
    }

    class MakeupLimiterProcessor {
        constructor(rate) { this.rate = rate; }
        reset() {}
        process(samples) {
            dspRates.push(this.rate);
            return samples;
        }
    }

    const window = {
        AudioContext,
        localStorage: { getItem(key) {
            return {
                "sprechenIo.audio.scheduledKeepAlive": "off",
                "sprechenIo.audio.scheduledLeadingSilenceMs": "0",
                "sprechenIo.audio.scheduledFirstUtteranceWakeMs": "0"
            }[key] ?? null;
        } },
        sprechenIoWeb: { pcmPlaybackWorkletUrls: useWorklet ? ["processor.js"] : [] }
    };
    runInNewContext(playerSource, {
        window, console, ArrayBuffer, Uint8Array, Float32Array,
        setTimeout, clearTimeout,
        sprechenIoAudioDsp: {
            MakeupLimiterProcessor,
            scanSilentOutput(_samples, _quantum, rate) {
                scanRates.push(rate);
                return { silentOutputWindows: 0, maxContiguousSilentMs: 0 };
            }
        },
        sprechenIoPlaybackSink: {
            async createWorkletPlaybackSink(_context, _destination, rate) {
                workletStarted.open();
                await workletReady.promise;
                return {
                    bindInputRate(newRate) { rate = newRate; },
                    clear() {},
                    write(samples) { output.push({ route: "worklet", rate, sample: samples[0] }); }
                };
            }
        }
    });
    return { player: window.pcmPlayer, resume, workletStarted, workletReady, dspRates, scanRates, output };
}

const firstChunk = new Uint8Array([0, 64, 0, 64]);
const laterChunk = new Uint8Array([0, 32, 0, 32]);

for (const useWorklet of [false, true]) {
    const route = useWorklet ? "worklet" : "scheduled";
    test(`queued PCM keeps its rate across delayed start on the ${route} route`, { timeout: 5000 }, async () => {
        const { player, resume, workletStarted, workletReady, dspRates, scanRates, output } = fixture(useWorklet);
        const firstInit = player.init(32000);
        const first = player.enqueue(firstChunk);
        const secondInit = player.init(48000);
        const ignored = [player.enqueue("not PCM"), player.enqueue(new Uint8Array()),
            player.enqueue(new Uint8Array([1]))];
        const later = player.enqueue(laterChunk);
        assert.deepEqual(output, []);
        resume.open();
        if (useWorklet) {
            await workletStarted.promise;
            workletReady.open();
        }
        const [written, rateChange, ...initialized] = await Promise.all([first, later, firstInit, secondInit]);
        assert.equal(written.succeeded, true);
        assert.ok(initialized.every(result => result.succeeded));
        assert.deepEqual(dspRates, [32000]);
        assert.deepEqual(scanRates, [32000]);
        assert.deepEqual(output.map(({ route, rate, sample }) => ({ route, rate, sample })),
            [{ route, rate: 32000, sample: 0.5 }]);
        if (!useWorklet) assert.equal(output[0].duration, 2 / 32000);
        assert.equal(rateChange.errorCode, "PCM_PLAYER_SAMPLE_RATE_CHANGED");
        assert.ok((await Promise.all(ignored)).every(result => result.succeeded));
        assert.equal((await player.reset()).succeeded, true);
        assert.equal((await player.enqueue(laterChunk)).succeeded, true);
        assert.deepEqual(output.map(({ rate, sample }) => [rate, sample]), [[32000, 0.5], [48000, 0.25]]);
    });
}

test("reset discards a chunk still waiting for the worklet", { timeout: 5000 }, async () => {
    const { player, resume, workletStarted, workletReady, output } = fixture(true);
    const init = player.init(32000);
    const stale = player.enqueue(firstChunk);
    resume.open();
    await workletStarted.promise;
    await player.reset();
    const nextInit = player.init(48000);
    const next = player.enqueue(laterChunk);
    workletReady.open();
    const results = await Promise.all([stale, next, init, nextInit]);
    assert.ok(results.every(result => result.succeeded));
    assert.deepEqual(output, [{ route: "worklet", rate: 48000, sample: 0.25 }]);
});
