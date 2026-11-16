using Bunit;
using Sprechen.Speech.App.BrowserInfrastructure.Services;
using Sprechen.Speech.App.CaptureRuntime.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Prüft die vom Browser gemeldete Abtastrate gegen die Vorgabe des Aufnahmerings.
/// Fehlende oder abweichende Angaben führen zum Abbruch des Aufnahmestarts.
/// </summary>
public sealed class MicrophoneRecorderSampleRateOwnerTests : TestContext
{
    private static void ArrangeConnect(TestContext context, string mimeType)
    {
        context.JSInterop
            .Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true)
            .SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        context.JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult(mimeType);
        context.JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();
        context.JSInterop.SetupVoid("micRecorder.release", _ => true).SetVoidResult();
    }

    [Fact]
    public async Task StartAsync_WhenTheShellReportsAnotherRate_RejectsTheRecording()
    {
        ArrangeConnect(this, "audio/pcm;rate=48000");
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);

        var failure = await Assert.ThrowsAsync<MicrophoneAcquisitionException>(
            () => recorder.StartAsync(CancellationToken.None));

        var cause = Assert.IsType<InvalidOperationException>(failure.InnerException);
        Assert.Contains("rate=48000", cause.Message, StringComparison.Ordinal);
        Assert.Contains($"rate={InMemorySpeechSampleRing.SampleRateHz}", cause.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StartAsync_WhenTheShellReportsNoRate_RejectsTheRecording()
    {
        ArrangeConnect(this, "audio/pcm");
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);

        var failure = await Assert.ThrowsAsync<MicrophoneAcquisitionException>(
            () => recorder.StartAsync(CancellationToken.None));

        Assert.IsType<InvalidOperationException>(failure.InnerException);
    }

    [Fact]
    public async Task StartAsync_WhenTheShellReportsTheOwnersRate_KeepsTheRecording()
    {
        ArrangeConnect(this, $"audio/pcm;rate={InMemorySpeechSampleRing.SampleRateHz}");
        var recorder = MicrophoneTestRecorder.Create(JSInterop.JSRuntime);

        await using var recording = await recorder.StartAsync(CancellationToken.None);

        Assert.Equal($"audio/pcm;rate={InMemorySpeechSampleRing.SampleRateHz}", recording.MimeType);
    }
}
