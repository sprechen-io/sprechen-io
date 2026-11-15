using Sprechen.Speech.Runtime.Tests.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sprechen.Speech.Runtime.Tests.Capture;

public sealed class SpeechCaptureChunkDispatcherTests
{
    [Fact]
    public async Task RegisterAdditionalConsumer_Twice_DeliversEachChunkOnce()
    {
        var dispatcher = CreateDispatcher();
        var consumer = new RecordingSpeechCaptureChunkConsumer();
        dispatcher.RegisterAdditionalConsumer(consumer);
        dispatcher.RegisterAdditionalConsumer(consumer);

        await dispatcher.ProcessChunkAsync(new byte[] { 1, 2 }, CancellationToken.None);

        Assert.Single(consumer.Chunks);
    }

    [Fact]
    public async Task UnregisterAdditionalConsumer_ForAnUnregisteredConsumer_IsANoOp()
    {
        var dispatcher = CreateDispatcher();
        var registered = new RecordingSpeechCaptureChunkConsumer();
        var stranger = new RecordingSpeechCaptureChunkConsumer();
        dispatcher.RegisterAdditionalConsumer(registered);

        dispatcher.UnregisterAdditionalConsumer(stranger);
        await dispatcher.ProcessChunkAsync(new byte[] { 1, 2 }, CancellationToken.None);
        dispatcher.FailActiveConsumers(new InvalidOperationException("boom"));
        dispatcher.CancelActiveCaptures(CaptureCancellationKind.Abort, CancellationToken.None);

        Assert.Single(registered.Chunks);
        Assert.Equal(1, registered.FailureCount);
        Assert.Equal(1, registered.CancellationCount);
        Assert.Empty(stranger.Chunks);
        Assert.Equal(0, stranger.FailureCount);
    }

    [Fact]
    public async Task ProcessChunkAsync_WhenAConsumerRegistersAndUnregistersDuringDelivery_DeliversSafely()
    {
        var dispatcher = CreateDispatcher();
        var late = new RecordingSpeechCaptureChunkConsumer();
        RecordingSpeechCaptureChunkConsumer? first = null;
        first = new RecordingSpeechCaptureChunkConsumer(self =>
        {
            // Eine Änderung der Verbraucher während der Zustellung darf den laufenden
            // Durchlauf nicht verändern; sie gilt erst für den nächsten Audioblock.
            dispatcher.RegisterAdditionalConsumer(late);
            dispatcher.UnregisterAdditionalConsumer(self);
        });
        dispatcher.RegisterAdditionalConsumer(first);

        await dispatcher.ProcessChunkAsync(new byte[] { 1 }, CancellationToken.None);
        await dispatcher.ProcessChunkAsync(new byte[] { 2 }, CancellationToken.None);

        Assert.Single(first.Chunks);
        Assert.Single(late.Chunks);
        Assert.Equal(2, late.Chunks[0][0]);
    }

    [Fact]
    public async Task RegisterAdditionalConsumer_DuringDelivery_JoinsFromTheNextChunkOn()
    {
        var dispatcher = CreateDispatcher();
        var late = new RecordingSpeechCaptureChunkConsumer();
        var first = new RecordingSpeechCaptureChunkConsumer(_ => dispatcher.RegisterAdditionalConsumer(late));
        dispatcher.RegisterAdditionalConsumer(first);

        await dispatcher.ProcessChunkAsync(new byte[] { 1 }, CancellationToken.None);
        await dispatcher.ProcessChunkAsync(new byte[] { 2 }, CancellationToken.None);

        Assert.Equal(2, first.Chunks.Count);
        Assert.Single(late.Chunks);
        Assert.Equal(2, late.Chunks[0][0]);
    }

    [Fact]
    public async Task UnregisterAdditionalConsumer_DuringDelivery_StillDeliversTheRunningChunkToTheRemainingConsumer()
    {
        var dispatcher = CreateDispatcher();
        RecordingSpeechCaptureChunkConsumer? leaving = null;
        leaving = new RecordingSpeechCaptureChunkConsumer(self => dispatcher.UnregisterAdditionalConsumer(self));
        var staying = new RecordingSpeechCaptureChunkConsumer();
        dispatcher.RegisterAdditionalConsumer(leaving);
        dispatcher.RegisterAdditionalConsumer(staying);

        await dispatcher.ProcessChunkAsync(new byte[] { 1 }, CancellationToken.None);
        await dispatcher.ProcessChunkAsync(new byte[] { 2 }, CancellationToken.None);

        Assert.Single(leaving.Chunks);
        Assert.Equal(2, staying.Chunks.Count);
        Assert.Equal(1, staying.Chunks[0][0]);
        Assert.Equal(2, staying.Chunks[1][0]);
    }

    [Fact]
    public async Task ProcessChunkAsync_AllocatesNothingPerRegisteredConsumer()
    {
        // Die Differenz zwischen drei Verbrauchern und keinem isoliert deren zusätzlichen
        // Allokationsaufwand. Gemeinsame Grundkosten des Verteilers fallen aus dem Vergleich heraus.
        const int chunks = 2_000;
        var chunk = new byte[] { 1, 2, 3, 4 };
        var bare = CreateDispatcher();
        var loaded = CreateDispatcher();
        var consumers = new[]
        {
            new CountingSpeechCaptureChunkConsumer(),
            new CountingSpeechCaptureChunkConsumer(),
            new CountingSpeechCaptureChunkConsumer()
        };
        foreach (var consumer in consumers)
        {
            loaded.RegisterAdditionalConsumer(consumer);
        }

        await MeasureAllocatedBytesAsync(bare, chunk, chunks);
        await MeasureAllocatedBytesAsync(loaded, chunk, chunks);
        var bareBytes = await MeasureAllocatedBytesAsync(bare, chunk, chunks);
        var loadedBytes = await MeasureAllocatedBytesAsync(loaded, chunk, chunks);

        Assert.All(consumers, consumer => Assert.Equal(2 * chunks, consumer.ChunkCount));
        Assert.True(
            loadedBytes - bareBytes < chunks,
            $"Registered consumers allocated {loadedBytes - bareBytes} bytes over {chunks} chunks; expected less than one byte per chunk.");
    }

    private static async Task<long> MeasureAllocatedBytesAsync(SpeechCaptureChunkDispatcher dispatcher, byte[] chunk, int chunks)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < chunks; index++)
        {
            await dispatcher.ProcessChunkAsync(chunk, CancellationToken.None);
        }

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static SpeechCaptureChunkDispatcher CreateDispatcher()
    {
        var ring = new InMemorySpeechSampleRing();
        var segmenterFactory = new SpeechCaptureSegmenterFactory(
            new SpeechCaptureSegmenterOptionsFactory(
                Options.Create(new SpeechCaptureClientOptions()),
                Options.Create(new PcmSpeechCaptureSegmenterOptions())));
        var activeCaptureCoordinator = new SpeechCaptureActiveCaptureCoordinator(
            new NoOpUserSpeechBargeInHandler(),
            NullLogger<SpeechCaptureActiveCaptureCoordinator>.Instance,
            new NoOpBrowserDebugConsole(),
            segmenterFactory,
            ring,
            new StubMicGateState(isHardOff: false));
        return new SpeechCaptureChunkDispatcher(activeCaptureCoordinator);
    }
}
