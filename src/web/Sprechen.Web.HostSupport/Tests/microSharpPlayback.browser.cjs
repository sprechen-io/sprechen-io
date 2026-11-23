const assert = require("node:assert/strict");
const { createServer } = require("node:http");
const { readFileSync } = require("node:fs");
const { join } = require("node:path");
const { chromium } = require(process.env.PLAYWRIGHT_PACKAGE || "playwright");

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

function prepare(route, worklets) {
    const host = window.standardizedAudioContext;
    const { AudioContext, AudioWorkletNode } = host;
    const probe = window.probe = { packets: [], blocks: [], admissions: 0, nodes: [] };
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
                    send(message, transfer);
                };
            }
        }
    };
    if (route !== "legacy") localStorage.setItem("sprechenIo.audio.playbackTransport", "microsharp");
    window.sprechenIoWeb = {
        pcmPlaybackWorkletUrls: worklets.map(file => "/js/" + file)
    };
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
            const observer = { invokeMethodAsync: async () => { probe.admissions++; } };
            let inputOffset = 0;
            const start = Math.round(context.currentTime * context.sampleRate);
            for (const frames of route === "credits" ? [110000] : [127, 193, 197]) {
                const block = Int16Array.from({ length: frames }, (_, i) => 16000 + (inputOffset + i) % 17 * 512);
                inputOffset += frames;
                await checked(pcmPlayer.enqueue(new Uint8Array(block.buffer), probe.admissions ? undefined : observer));
                await wait(18);
            }
            await checked(pcmPlayer.drain());
            await wait(50);
            const visible = probe.blocks.flatMap(({ frame, samples }) => Array.from(samples, (value, i) => ({ value, frame: frame + i })))
                .filter(({ value }) => Math.abs(value) > 1e-7);
            await context.close();
            return {
                trusted: event.isTrusted, admissions: probe.admissions, nodes: probe.nodes, packets: probe.packets,
                first: visible[0]?.frame, start, values: visible.map(({ value }) => value)
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
        for (const route of ["legacy", "microsharp", "missing", "failed-worklet", "credits"]) {
            const page = await browser.newPage();
            await page.addInitScript(() => { localStorage.setItem("sprechenIo.audio.scheduledKeepAlive", "off"); localStorage.setItem("sprechenIo.audio.scheduledFirstUtteranceWakeMs", "0"); });
            if (route === "missing") await page.route("**/MicroSharp.Browser.Playback/microsharp-playback.js", route => route.abort());
            if (route === "failed-worklet") await page.route("**/MicroSharp.Browser.Playback/pcm-playback-processor.js", route => route.abort());
            await page.goto("http://127.0.0.1:" + server.address().port);
            await page.evaluate(([fn, selected, modules]) => Function("return (" + fn + ")")()(selected, modules), [prepare.toString(), route, worklets]);
            await page.getByRole("button").click();
            result[route] = await page.evaluate(() => window.measurement);
            await page.close();
        }
        for (const item of Object.values(result)) assert.ok(item.trusted && item.admissions === 1);
        assert.deepEqual(result.microsharp.values, result.legacy.values);
        assert.deepEqual(result.missing.values, result.legacy.values);
        assert.deepEqual(result["failed-worklet"].values, result.legacy.values);
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
