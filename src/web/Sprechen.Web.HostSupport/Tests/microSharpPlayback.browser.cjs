const assert = require("node:assert/strict");
const { createServer } = require("node:http");
const { readFileSync } = require("node:fs");
const { join } = require("node:path");
const { chromium } = require("playwright");

const [assets, packageAssets] = process.argv.slice(2);
const scripts = [
    "vendor/standardized-audio-context/sac.global.js", "js/audioDsp.js",
    "js/playback/streamingResampler.js", "js/playback/workletPlaybackSink.js",
    "js/playback/microSharpPlaybackSink.js", "js/pcmPlayer.js"
];
const worklets = ["playback/pcmRingBuffer.js", "playback/streamingResampler.js", "worklets/pcmPlaybackProcessor.js"];
const appBase = "/_content/Sprechen.Web.HostSupport/voice-support/";
const packageBase = "/_content/MicroSharp.Browser.Playback/";
const routes = new Map();
for (const file of scripts) {
    routes.set(appBase + file, readFileSync(join(assets, file)));
}
for (const file of worklets) {
    routes.set("/js/" + file, readFileSync(join(assets, "js", file)));
}
for (const file of ["microsharp-playback.js", "pcm-playback-processor.js"]) {
    routes.set(packageBase + file, readFileSync(join(packageAssets, file)));
}

const tap = `class RenderTap extends AudioWorkletProcessor {
    process(inputs) {
        const samples = inputs[0][0];
        if (samples) this.port.postMessage({ frame: currentFrame, samples });
        return true;
    }
}
registerProcessor("render-tap", RenderTap);`;

function prepare({ route, worklets }) {
    const host = window.standardizedAudioContext;
    const { AudioContext, AudioWorkletNode } = host;
    const probe = window.probe = { packets: [], blocks: [], admissions: 0, nodes: [] };
    const fullBuffer = new Promise(resolve => { probe.bufferFull = resolve; });
    let admittedFrames = 0;
    window.standardizedAudioContext = {
        ...host,
        AudioContext: class extends AudioContext {
            constructor(options) {
                super({ ...options, sampleRate: 44100 });
                probe.context = this;
            }
            createGain() {
                const result = super.createGain();
                probe.master ??= result;
                return result;
            }
        },
        AudioWorkletNode: class extends AudioWorkletNode {
            constructor(context, name, options) {
                super(context, name, options);
                probe.nodes.push({ name, options: options.processorOptions });
                const send = this.port.postMessage.bind(this.port);
                this.port.postMessage = (message, transfer) => {
                    probe.packets.push({ kind: message.kind, frames: message.samples?.length });
                    if (message.kind === "float32") {
                        admittedFrames += message.samples.length;
                        if (admittedFrames >= 96_000) probe.bufferFull();
                    }
                    if (route === "bad-drain" && name === "microsharp-pcm-playback" && message.kind === "eof") {
                        return;
                    }
                    send(message, transfer);
                };
            }
        }
    };
    if (route !== "legacy") localStorage.setItem("sprechenIo.audio.playbackTransport", "microsharp");
    window.sprechenIoWeb = {
        pcmPlaybackWorkletUrls: worklets.map(file => "/js/" + file)
    };
    if (route === "tiny") {
        // Exercise a DSP that retains a chunk and produces its first frames during drain.
        const dsp = window.sprechenIoAudioDsp.MakeupLimiterProcessor.prototype;
        const process = dsp.process;
        dsp.process = function (frames) {
            process.call(this, frames);
            return new Float32Array();
        };
    }
    const wait = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));
    async function checked(operation) {
        const result = await operation;
        if (!result.succeeded) throw Error(result.errorCode + ": " + result.diagnosticDetail);
    }
    document.querySelector("button").onclick = event => {
        window.measurement = (async () => {
            await checked(pcmPlayer.warmup());
            await checked(pcmPlayer.warmup());
            const { context, master } = probe;
            await context.audioWorklet.addModule("/tap.js");
            const listener = new AudioWorkletNode(context, "render-tap", { numberOfOutputs: 0 });
            listener.port.onmessage = event => probe.blocks.push(event.data);
            master.connect(listener);
            await checked(pcmPlayer.init(24000));
            await checked(pcmPlayer.reset());
            await wait(150);
            await checked(pcmPlayer.init(32000));
            await checked(pcmPlayer.setMakeup(1.9));
            if (route === "reset") {
                await checked(pcmPlayer.enqueue(new Uint8Array(new Int16Array(197).fill(-16000).buffer)));
                const staleDrain = pcmPlayer.drain();
                await checked(pcmPlayer.reset());
                await checked(staleDrain);
            }
            if (route === "credit-reset") {
                const staleWrite = pcmPlayer.enqueue(new Uint8Array(new Int16Array(110000).fill(-16000).buffer));
                const bufferFilled = await Promise.race([fullBuffer.then(() => true), wait(2000).then(() => false)]);
                if (!bufferFilled) throw Error("The old generation did not fill its FIFO.");
                await checked(pcmPlayer.reset());
                await checked(staleWrite);
            }
            const observer = { invokeMethodAsync: async () => { probe.admissions++; } };
            let inputOffset = 0;
            const start = Math.round(context.currentTime * context.sampleRate);
            const blocks = route === "credits" ? [110000] : route === "tiny" ? [1] : [127, 193, 197];
            for (const frames of blocks) {
                const block = Int16Array.from({ length: frames }, (_, i) => 16000 + (inputOffset + i) % 17 * 512);
                inputOffset += frames;
                await checked(pcmPlayer.enqueue(new Uint8Array(block.buffer), probe.admissions ? undefined : observer));
                await wait(18);
            }
            if (route === "tiny" && probe.admissions !== 0) {
                throw Error("Buffered input was reported as an admission before the drain.");
            }
            const drain = await pcmPlayer.drain(probe.admissions ? undefined : observer);
            if (route === "bad-drain") {
                if (drain.succeeded) throw Error("A missing worklet acknowledgement reported success.");
            } else {
                await checked(Promise.resolve(drain));
            }
            await wait(50);
            const visible = probe.blocks.flatMap(({ frame, samples }) => Array.from(samples, (value, i) => ({ value, frame: frame + i })))
                .filter(({ value }) => Math.abs(value) > 1e-7);
            await context.close();
            return {
                trusted: event.isTrusted, admissions: probe.admissions, nodes: probe.nodes, packets: probe.packets,
                first: visible[0]?.frame, start, drain: drain.succeeded, values: visible.map(({ value }) => value)
            };
        })();
    };
}

const server = createServer((req, res) => {
    const route = new URL(req.url, "http://127.0.0.1").pathname;
    if (route === "/") {
        res.writeHead(200, { "Content-Type": "text/html" });
        res.end("<!doctype html><button>Start</button>" + scripts
            .map(file => '<script src="' + appBase + file + '"></script>').join(""));
        return;
    }
    const asset = route === "/tap.js" ? tap : routes.get(route);
    res.writeHead(asset ? 200 : 404, { "Content-Type": "text/javascript" });
    res.end(asset);
});

(async () => {
    await new Promise(resolve => server.listen(0, "127.0.0.1", resolve));
    const browser = await chromium.launch({ headless: true, args: ["--autoplay-policy=user-gesture-required"] });
    try {
        const result = {};
        for (const route of ["legacy", "microsharp", "missing", "failed-worklet", "credits", "reset", "credit-reset", "bad-drain", "tiny"]) {
            const page = await browser.newPage();
            await page.addInitScript(() => { localStorage.setItem("sprechenIo.audio.scheduledKeepAlive", "off"); localStorage.setItem("sprechenIo.audio.scheduledFirstUtteranceWakeMs", "0"); });
            if (route === "missing") await page.route("**/MicroSharp.Browser.Playback/microsharp-playback.js", route => route.abort());
            if (route === "failed-worklet") await page.route("**/MicroSharp.Browser.Playback/pcm-playback-processor.js", route => route.abort());
            await page.goto("http://127.0.0.1:" + server.address().port);
            await page.evaluate(prepare, { route, worklets });
            await page.getByRole("button").click();
            result[route] = await page.evaluate(() => window.measurement);
            await page.close();
        }
        for (const item of Object.values(result)) assert.ok(item.trusted && item.admissions === 1);
        assert.deepEqual(result.microsharp.values, result.legacy.values);
        assert.deepEqual(result.missing.values, result.legacy.values);
        assert.deepEqual(result["failed-worklet"].values, result.legacy.values);
        assert.deepEqual(result.reset.values, result.legacy.values);
        assert.deepEqual(result["credit-reset"].values, result.legacy.values);
        assert.equal(result["bad-drain"].drain, false);
        assert.equal(result.microsharp.nodes[0].name, "microsharp-pcm-playback");
        assert.equal(result.missing.nodes[0].name, "sprechen-pcm-playback");
        assert.equal(result["failed-worklet"].nodes[0].name, "sprechen-pcm-playback");
        const creditWrites = result.credits.packets.filter(packet => packet.kind === "float32");
        assert.equal(creditWrites.reduce((total, packet) => total + packet.frames, 0),
            Math.ceil((110000 + 48) * 44100 / 32000));
        assert.ok(result.credits.packets.some(packet => packet.kind === "creditAck"));
        assert.ok(creditWrites.some(packet => packet.frames < 1024));
        for (const item of Object.values(result)) {
            item.visible = item.values.length;
            delete item.values;
            item.packets = item.packets.length;
        }
        console.log(JSON.stringify(result, null, 2));
    } finally {
        await browser.close();
        server.close();
    }
})().catch(error => { console.error(error); process.exitCode = 1; });
