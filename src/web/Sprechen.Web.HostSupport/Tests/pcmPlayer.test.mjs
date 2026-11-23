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

function fixture(useWorklet, drainOutcome = "cancelled", createMicroSharpPlaybackSink, bufferedTail = false) {
    const resume = gate();
    const workletStarted = gate();
    const workletReady = gate();
    const drainEnded = gate();
    const dspRates = [];
    const scanRates = [];
    const output = [];
    const drains = [];
    const reports = [];
    const starts = { context: 0, worklet: 0 };

    class AudioContext {
        state = "suspended";
        sampleRate = 44100;
        currentTime = 0;
        destination = {};

        async resume() {
            starts.context++;
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
        flush() {
            drains.push("flush");
            return bufferedTail ? Float32Array.of(0.5) : new Float32Array();
        }
        process(samples) {
            dspRates.push(this.rate);
            return bufferedTail ? new Float32Array() : samples;
        }
    }

    const window = {
        AudioContext,
        localStorage: { getItem(key) {
            return {
                "sprechenIo.audio.scheduledKeepAlive": "off",
                "sprechenIo.audio.scheduledLeadingSilenceMs": "0",
                "sprechenIo.audio.scheduledFirstUtteranceWakeMs": "0",
                "sprechenIo.audio.playbackTransport": createMicroSharpPlaybackSink ? "microsharp" : null,
            }[key] ?? null;
        } },
        sprechenIoWeb: {
            pcmPlaybackWorkletUrls: useWorklet ? ["processor.js"] : []
        }
    };
    runInNewContext(playerSource, {
        window, ArrayBuffer, Uint8Array, Float32Array, Error,
        console: { ...console, info(message) {
            if (message.startsWith("pcmPlayer: playback report")) reports.push(message);
        } },
        setTimeout, clearTimeout,
        sprechenIoMicroSharpPlayback: { createMicroSharpPlaybackSink },
        sprechenIoAudioDsp: {
            MakeupLimiterProcessor,
            scanSilentOutput(_samples, _quantum, rate) {
                scanRates.push(rate);
                return { silentOutputWindows: 0, maxContiguousSilentMs: 0 };
            }
        },
        sprechenIoPlaybackSink: {
            async createWorkletPlaybackSink(_context, _destination, rate) {
                starts.worklet++;
                workletStarted.open();
                await workletReady.promise;
                return {
                    bindInputRate(newRate) { rate = newRate; },
                    clear() {},
                    dispose() {},
                    write(samples) { output.push({ route: "worklet", rate, sample: samples[0] }); },
                    writtenFrames: 0,
                    playedFrames: 0,
                    underruns: 0,
                    routeLeadMs: 0,
                    async endOfStream() {
                        drains.push("endOfStream");
                        drainEnded.open();
                        return drainOutcome;
                    }
                };
            }
        }
    });
    return { player: window.pcmPlayer, resume, workletStarted, workletReady, drainEnded,
        dspRates, scanRates, output, drains, reports, starts };
}

const firstChunk = new Uint8Array([0, 64, 0, 64]);
const laterChunk = new Uint8Array([0, 32, 0, 32]);

test("opt-in playback reports the first positive admission before the enqueue settles",
    { timeout: 5000 }, async () => {
        const entered = gate();
        const credit = gate();
        const accepted = gate();
        const complete = gate();
        const { player, resume, starts } = fixture(true, "drained", async () => ({
            bindInputRate() {},
            async write(_frames, onAccepted) {
                entered.open();
                await credit.promise;
                await onAccepted();
                await complete.promise;
            }
        }));
        const init = player.init(32000);
        resume.open();
        assert.ok((await init).succeeded);
        let reported = 0;
        let settled = false;
        const enqueue = player.enqueue(firstChunk, { async invokeMethodAsync() {
            reported++;
            accepted.open();
        } }).then(result => { settled = true; return result; });
        await entered.promise;
        assert.equal(reported, 0);
        credit.open();
        await accepted.promise;
        assert.equal(reported, 1);
        assert.equal(settled, false);
        complete.open();
        assert.ok((await enqueue).succeeded);
        assert.equal(starts.worklet, 0);
    });

test("failure to prepare opt-in playback selects the legacy worklet before admission",
    { timeout: 5000 }, async () => {
        const { player, resume, workletReady, output } = fixture(true, "drained", async () => {
            throw new Error("Playback module unavailable");
        });
        const init = player.init(32000);
        resume.open();
        workletReady.open();
        assert.ok((await init).succeeded);
        let reported = 0;
        assert.ok((await player.enqueue(firstChunk, { async invokeMethodAsync() { reported++; } })).succeeded);
        assert.equal(reported, 1);
        assert.deepEqual(output, [{ route: "worklet", rate: 32000, sample: 0.5 }]);
    });

test("a transport startup failure reuses the already processed frames on the legacy route",
    { timeout: 5000 }, async () => {
        let disposed = 0;
        const { player, resume, workletReady, output, dspRates } = fixture(true, "drained", async () => ({
            bindInputRate() {},
            dispose() { disposed++; },
            async write() {
                const error = new Error("AudioWorklet did not start");
                error.name = "MicroSharpPlaybackStartupError";
                throw error;
            }
        }));
        const init = player.init(32000);
        resume.open();
        workletReady.open();
        assert.ok((await init).succeeded);
        let reported = 0;
        assert.ok((await player.enqueue(firstChunk, { async invokeMethodAsync() { reported++; } })).succeeded);
        assert.equal(reported, 1);
        assert.equal(disposed, 1);
        assert.deepEqual(dspRates, [32000]);
        assert.deepEqual(output, [{ route: "worklet", rate: 32000, sample: 0.5 }]);
    });

test("an error after admission never replays frames on the legacy route", { timeout: 5000 }, async () => {
    const { player, resume, starts, output } = fixture(true, "drained", async () => ({
        bindInputRate() {},
        async write(_frames, onAccepted) {
            await onAccepted();
            throw new Error("Processor failed after the first admission");
        }
    }));
    const init = player.init(32000);
    resume.open();
    assert.ok((await init).succeeded);
    let reported = 0;
    const result = await player.enqueue(firstChunk, { async invokeMethodAsync() { reported++; } });
    assert.equal(reported, 1);
    assert.equal(result.succeeded, false);
    assert.equal(starts.worklet, 0);
    assert.deepEqual(output, []);
});

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

test("reset invalidates a drain still waiting for playback startup", { timeout: 5000 }, async () => {
    const { player, resume, workletStarted, workletReady, output, drains } = fixture(true);
    const init = player.init(32000);
    resume.open();
    await workletStarted.promise;
    const staleDrain = player.drain();
    assert.equal((await player.reset()).succeeded, true);
    const nextInit = player.init(48000);
    const next = player.enqueue(laterChunk);
    workletReady.open();

    const results = await Promise.all([staleDrain, init, nextInit, next]);
    assert.ok(results.every(result => result.succeeded));
    assert.deepEqual(drains, []);
    assert.deepEqual(output, [{ route: "worklet", rate: 48000, sample: 0.25 }]);

    assert.equal((await player.drain()).succeeded, true);
    assert.deepEqual(drains, ["flush", "endOfStream"]);
});

test("reset suppresses a completed worklet drain report from the old generation", { timeout: 5000 }, async () => {
    const { player, resume, workletReady, drainEnded, reports } = fixture(true, "drained");
    const init = player.init(32000);
    resume.open();
    workletReady.open();
    assert.equal((await init).succeeded, true);
    const staleDrain = player.drain();
    await drainEnded.promise;
    assert.equal((await player.reset()).succeeded, true);

    assert.equal((await staleDrain).succeeded, true);
    assert.deepEqual(reports, []);
});

test("reset fences a drain whose admission callback is still pending", { timeout: 5000 }, async () => {
    const { player, resume, workletReady, drains } = fixture(true, "drained", undefined, true);
    const init = player.init(32000);
    resume.open();
    workletReady.open();
    assert.ok((await init).succeeded);
    assert.ok((await player.enqueue(firstChunk)).succeeded);

    const accepted = gate();
    const callback = gate();
    const drain = player.drain({ async invokeMethodAsync() {
        accepted.open();
        await callback.promise;
    } });
    await accepted.promise;
    assert.ok((await player.reset()).succeeded);
    callback.open();
    assert.ok((await drain).succeeded);
    assert.deepEqual(drains, ["flush"]);
});

test("concurrent drains share completion while admission is pending", { timeout: 5000 }, async () => {
    const { player, resume, workletReady, drains } = fixture(true, "drained", undefined, true);
    const init = player.init(32000);
    resume.open();
    workletReady.open();
    assert.ok((await init).succeeded);
    assert.ok((await player.enqueue(firstChunk)).succeeded);

    const accepted = gate();
    const callback = gate();
    const first = player.drain({ async invokeMethodAsync() {
        accepted.open();
        await callback.promise;
    } });
    await accepted.promise;

    let secondFinished = false;
    let secondAccepted = 0;
    const second = player.drain({ async invokeMethodAsync() {
        secondAccepted++;
    } }).then(result => {
        secondFinished = true;
        return result;
    });
    await new Promise(resolve => setImmediate(resolve));
    const finishedDuringAdmission = secondFinished;
    callback.open();

    const results = await Promise.all([first, second]);
    assert.ok(results.every(result => result.succeeded));
    assert.equal(finishedDuringAdmission, false);
    assert.equal(secondAccepted, 1);
    assert.deepEqual(drains, ["flush", "endOfStream"]);
});

test("an unavailable drain observer does not fail other callers or stop playback",
    { timeout: 5000 }, async () => {
        const { player, resume, workletReady, drains } = fixture(true, "drained", undefined, true);
        const init = player.init(32000);
        resume.open();
        workletReady.open();
        assert.ok((await init).succeeded);
        assert.ok((await player.enqueue(firstChunk)).succeeded);
        let othersNotified = 0;

        const unavailable = player.drain({ async invokeMethodAsync() {
            throw Error("interop reference unavailable");
        } });
        const other = player.drain({ async invokeMethodAsync() {
            othersNotified++;
        } });

        assert.equal((await unavailable).succeeded, false);
        assert.equal((await other).succeeded, true);
        assert.equal(othersNotified, 1);
        assert.deepEqual(drains, ["flush", "endOfStream"]);
    });

test("enqueue failure is reported by drains until reset without breaking the queue",
    { timeout: 5000 }, async () => {
        let writes = 0;
        const { player, resume } = fixture(true, "drained", async () => ({
            async write() {
                if (++writes === 2) throw Error("worklet write failed");
            },
            bindInputRate() {},
            clear() {},
            async endOfStream() { return "drained"; }
        }));
        const init = player.init(32000);
        resume.open();
        assert.ok((await init).succeeded);
        assert.ok((await player.enqueue(firstChunk)).succeeded);
        const rejected = player.enqueue(firstChunk);
        const drain = player.drain();
        const concurrent = player.drain();
        assert.equal((await rejected).succeeded, false);
        assert.ok((await player.enqueue(firstChunk)).succeeded);
        assert.equal((await drain).succeeded, false);
        assert.equal((await concurrent).succeeded, false);

        assert.ok((await player.reset()).succeeded);
        assert.ok((await player.enqueue(firstChunk)).succeeded);
        assert.ok((await player.drain()).succeeded);
    });

test("concurrent startup shares the output and preserves enqueue order as it becomes ready",
    { timeout: 5000 }, async () => {
        const { player, resume, workletStarted, workletReady, output, starts } = fixture(true);
        const firstInit = player.init(32000);
        const first = player.enqueue(firstChunk);
        const secondInit = player.init(32000);
        resume.open();
        await workletStarted.promise;
        assert.deepEqual(output, []);
        workletReady.open();

        // Submit distinct chunks on successive microtasks spanning worklet startup completion.
        const submissions = [];
        const expected = [0.5];
        for (let frame = 1; frame <= 16; frame++) {
            await Promise.resolve();
            submissions.push(player.enqueue(new Uint8Array([0, frame])));
            expected.push(frame / 128);
        }

        const results = await Promise.all([firstInit, first, secondInit, ...submissions]);
        assert.ok(results.every(result => result.succeeded));
        assert.deepEqual(starts, { context: 1, worklet: 1 });
        assert.deepEqual(output.map(entry => entry.sample), expected);
    });
