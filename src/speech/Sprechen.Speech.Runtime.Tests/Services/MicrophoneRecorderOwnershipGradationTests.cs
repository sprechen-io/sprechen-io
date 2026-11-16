namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Prüft verspätete Fehler beim Setzen des MicGate-Zustands, nachdem ein neuer Zustand angefordert
/// oder die Mikrofonsitzung ersetzt wurde.
/// </summary>
public sealed class MicrophoneRecorderOwnershipGradationTests
{
    [Fact]
    public async Task StaleMicGateFailure_AfterANewerGateWish_RetriesInsteadOfReleasingTheSession()
    {
        // Der Fehler des älteren Aufrufs darf den inzwischen geänderten Gatezustand nicht verhindern.
        var jsRuntime = new DelayedGateJsRuntime(firstGateFails: true);
        var recorder = MicrophoneTestRecorder.Create(jsRuntime);
        await recorder.RequestAccessAsync(CancellationToken.None);

        var staleGateWrite = recorder.SetMicGateAsync(false, CancellationToken.None);
        await jsRuntime.WaitForFirstGateWriteAsync();
        var latestGateWrite = recorder.SetMicGateAsync(true, CancellationToken.None);
        Assert.Equal(1, jsRuntime.GateCallCount);

        jsRuntime.CompleteFirstGateWrite();
        await staleGateWrite.WaitAsync(TimeSpan.FromSeconds(5));
        await latestGateWrite.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal([("1", false), ("1", true)], jsRuntime.MicGateCalls);
        Assert.Empty(jsRuntime.ReleasedSessionIds);
        Assert.Equal(1, jsRuntime.AcquireCallCount);
    }

    [Fact]
    public async Task StaleMicGateFailure_AfterASessionReplacementWithinTheSameGeneration_LeavesTheStartIntact()
    {
        // Ein verspäteter Fehler für die alte Sitzung darf die inzwischen gestartete Sitzung nicht freigeben.
        var jsRuntime = new ReplacedSessionGateJsRuntime();
        var recorder = MicrophoneTestRecorder.Create(jsRuntime);
        await recorder.RequestAccessAsync(CancellationToken.None);

        var staleGateWrite = recorder.SetMicGateAsync(false, CancellationToken.None);
        await jsRuntime.WaitForFirstGateWriteAsync();
        var start = recorder.StartAsync(CancellationToken.None);
        await jsRuntime.WaitForSecondAcquisitionAsync();
        Assert.Equal(1, jsRuntime.GateCallCount);

        jsRuntime.CompleteFirstGateWrite();
        await staleGateWrite.WaitAsync(TimeSpan.FromSeconds(5));
        await using var recording = await start.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("audio/pcm;rate=16000", recording.MimeType);
        Assert.Equal(["1"], jsRuntime.ReleasedSessionIds);
        Assert.Equal([("1", false), ("2", false)], jsRuntime.MicGateCalls);
    }
}
