(() => {
    type Snapshot = {
        contentOutputFrames: number;
        underrunOutputFrames: number;
        rejectedInputFrames: number;
    };

    type Transport = {
        start(): Promise<number>;
        reserve(frames: number): number;
        enqueue(frames: Float32Array): void;
        waitForCredit(signal: AbortSignal): Promise<void>;
        drain(): Promise<void>;
        snapshot(): Promise<Snapshot>;
        stop(): Promise<void>;
    };

    type Factory = (
        context: BaseAudioContext,
        destination: AudioNode,
        inputRate: number,
        capacity: number,
        nodeConstructor: typeof AudioWorkletNode,
        startup: { minimumBufferedInputFrames: number; earliestOutputFrame: number }
    ) => Transport;

    const LegacyPrefillInputFrames = 2400;
    const TransportCapacityFrames = 96_000;
    const ResamplerBatchFrames = 1024;
    const TransportStartupWindowMs = 2_000;

    const createMicroSharpPlaybackSink = async (
        context: BaseAudioContext,
        destination: AudioNode,
        moduleUrl: string,
        initialRate: number,
        initialLeadMs: number
    ) => {
        const url = new URL(moduleUrl, document.baseURI);
        const { createBorrowedPlayer } = await import(url.href) as { createBorrowedPlayer: Factory };
        const host = (globalThis as typeof globalThis & {
            standardizedAudioContext?: { AudioWorkletNode?: typeof AudioWorkletNode };
        }).standardizedAudioContext;
        const nodeConstructor = host?.AudioWorkletNode ?? globalThis.AudioWorkletNode;
        if (typeof createBorrowedPlayer !== "function" || !nodeConstructor || !context.audioWorklet) {
            throw new Error("Borrowed MicroSharp playback is unavailable.");
        }
        await context.audioWorklet.addModule(new URL("pcm-playback-processor.js", url).href);

        const createGeneration = (leadMs: number) => ({
            leadMs,
            controller: new AbortController(),
            resampler: new StreamingResampler(initialRate, context.sampleRate),
            transport: null as Transport | null,
            earliestOutputFrame: 0,
            write: Promise.resolve(),
            drain: null as Promise<"drained" | "cancelled"> | null,
            written: 0,
            played: 0,
            underrunOutputFrames: 0,
        });
        let current = createGeneration(initialLeadMs);
        let rate = initialRate;
        const waitForStartupWindow = async (generation: ReturnType<typeof createGeneration>): Promise<void> => {
            const waitMs = 1000 * (generation.earliestOutputFrame / context.sampleRate - context.currentTime)
                - TransportStartupWindowMs;
            if (waitMs <= 0) {
                return;
            }

            // The package starts its own credit and drain deadlines when called.
            // Keep a deliberately delayed route outside those deadlines until it can render.
            const { signal } = generation.controller;
            let timer: number | undefined;
            let abort: (() => void) | undefined;
            try {
                await new Promise<void>((resolve, reject) => {
                    signal.throwIfAborted();
                    abort = () => reject(signal.reason);
                    signal.addEventListener("abort", abort, { once: true });
                    timer = window.setTimeout(resolve, waitMs);
                });
                signal.throwIfAborted();
            } finally {
                window.clearTimeout(timer);
                if (abort) signal.removeEventListener("abort", abort);
            }
        };
        const stop = () => {
            const generation = current;
            generation.controller.abort();
            if (generation.transport) {
                void generation.transport.stop().catch(() => {
                    // The generation is already fenced from later playback.
                });
            }
        };

        return {
            get writtenFrames() { return current.written; },
            get playedFrames() { return current.played; },
            get underrunOutputFrames() { return current.underrunOutputFrames; },
            get droppedFrames() { return 0; },
            get routeLeadMs() { return current.leadMs; },
            bindInputRate(inputRate: number) {
                rate = inputRate;
                current.resampler = new StreamingResampler(rate, context.sampleRate);
            },
            clear(routeLeadMs: number) {
                stop();
                current = createGeneration(routeLeadMs);
            },
            write(frames: Float32Array, onAccepted?: () => Promise<void>) {
                const generation = current;
                const inputRate = rate;
                const signal = generation.controller.signal;
                const write = generation.write.then(async () => {
                    signal.throwIfAborted();
                    if (!generation.transport) {
                        generation.earliestOutputFrame = Math.ceil(
                            (context.currentTime + generation.leadMs / 1000) * context.sampleRate);
                        const prefill = Math.min(TransportCapacityFrames,
                            Math.ceil(LegacyPrefillInputFrames * context.sampleRate / inputRate));
                        generation.transport = createBorrowedPlayer(
                            context, destination, context.sampleRate, TransportCapacityFrames, nodeConstructor,
                            { earliestOutputFrame: generation.earliestOutputFrame, minimumBufferedInputFrames: prefill });
                        await generation.transport.start();
                    }

                    const transport = generation.transport;
                    const batch = new Float32Array(ResamplerBatchFrames);
                    let inputOffset = 0;
                    let firstAdmission = onAccepted;
                    for (;;) {
                        signal.throwIfAborted();
                        const count = generation.resampler.process(batch, target => {
                            const taken = Math.min(target.length, frames.length - inputOffset);
                            target.set(frames.subarray(inputOffset, inputOffset + taken));
                            inputOffset += taken;
                            return taken;
                        });
                        if (count === 0) {
                            break;
                        }

                        let offset = 0;
                        while (offset < count) {
                            signal.throwIfAborted();
                            const accepted = transport.reserve(count - offset);
                            if (accepted === 0) {
                                await waitForStartupWindow(generation);
                                await transport.waitForCredit(signal);
                                continue;
                            }

                            transport.enqueue(batch.subarray(offset, offset + accepted));
                            offset += accepted;
                            if (firstAdmission) {
                                await firstAdmission();
                                firstAdmission = undefined;
                            }
                        }
                    }
                    generation.written += frames.length;
                });
                generation.write = write;
                return write.catch(error => {
                    if (!signal.aborted) throw error;
                });
            },
            endOfStream() {
                const generation = current;
                generation.drain ??= (async () => {
                    try {
                        await generation.write;
                        const transport = generation.transport;
                        if (transport) {
                            await waitForStartupWindow(generation);
                            await transport.drain();
                            const snapshot = await transport.snapshot();
                            generation.played = snapshot.contentOutputFrames;
                            generation.underrunOutputFrames = snapshot.underrunOutputFrames;
                            await transport.stop();
                        }
                        return generation.controller.signal.aborted ? "cancelled" : "drained";
                    } catch (error) {
                        if (generation.controller.signal.aborted) return "cancelled";
                        throw error;
                    }
                })();
                return generation.drain;
            },
            dispose: stop,
        };
    };

    (globalThis as typeof globalThis & {
        sprechenIoMicroSharpPlayback?: { createMicroSharpPlaybackSink: typeof createMicroSharpPlaybackSink };
    }).sprechenIoMicroSharpPlayback = { createMicroSharpPlaybackSink };
})();
