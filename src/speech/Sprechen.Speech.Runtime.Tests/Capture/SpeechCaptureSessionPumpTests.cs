using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Capture;

public sealed class SpeechCaptureSessionPumpTests
{
    [Fact]
    public async Task RunAsync_WhenChunkStreamEndsWithoutCancellation_SignalsSessionEndedTerminal()
    {
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        var pump = new SpeechCaptureSessionPump(ring, NullLogger.Instance);
        var recording = new EnumeratedMicrophoneRecording(
            "audio/pcm;rate=16000",
            new[] { new byte[] { 1, 2 }, new byte[] { 3, 4 } });

        Exception? failure = null;

        await pump.RunAsync(
            recording,
            static (_, _) => Task.CompletedTask,
            ex => failure = ex,
            CancellationToken.None);

        // Auch ein reguläres Streamende muss eine wartende Aufnahme beenden, statt sie offen zu lassen.
        Assert.IsType<SpeechCaptureSessionEndedException>(failure);
    }

    [Fact]
    public async Task RunAsync_WhenCancelledDuringDeliberateTeardown_DoesNotSignalSessionEnded()
    {
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        var pump = new SpeechCaptureSessionPump(ring, NullLogger.Instance);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var recording = new EnumeratedMicrophoneRecording(
            "audio/pcm;rate=16000",
            new[] { new byte[] { 1, 2 } });

        Exception? failure = null;

        await pump.RunAsync(
            recording,
            static (_, _) => Task.CompletedTask,
            ex => failure = ex,
            cancellation.Token);

        // Beim beabsichtigten Stopp ist das Sitzungstoken bereits abgebrochen;
        // der daraus folgende Abbruch der Schleife darf keinen Aufnahmefehler melden.
        Assert.Null(failure);
    }
}
