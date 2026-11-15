interface AudioWorkletProcessorOptions {
    processorOptions?: Record<string, unknown>;
}

declare abstract class AudioWorkletProcessor {
    readonly port: MessagePort;

    protected constructor(options?: AudioWorkletProcessorOptions);

    abstract process(
        inputs: Float32Array[][],
        outputs: Float32Array[][],
        parameters: Record<string, Float32Array>
    ): boolean;
}

declare const sampleRate: number;

declare function registerProcessor(
    name: string,
    processorCtor: new (options?: AudioWorkletProcessorOptions) => AudioWorkletProcessor
): void;
