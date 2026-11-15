using Sprechen.Speech.Runtime.Tests.Services;

namespace Sprechen.Speech.Runtime.Tests.Capture;

/// <summary>
/// Prüft, dass eine Aufnahme bei gesperrtem Mikrofon wartet und nach Freigabe
/// starten kann. Laufende Assistentenwiedergabe soll den Dialogzug nicht als
/// Aufnahmefehler beenden; ein angeforderter Abbruch bleibt möglich.
/// </summary>
public sealed class ActiveCaptureMicGateGuardTests
{
    [Fact]
    public async Task WaitUntilCanStart_WhenGateIsOpen_ReturnsImmediately()
    {
        var guard = new ActiveCaptureMicGateGuard(new StubMicGateState(isHardOff: false));

        var wait = guard.WaitUntilCanStartAsync(CancellationToken.None);

        Assert.True(wait.IsCompletedSuccessfully);
        await wait;
    }

    [Fact]
    public async Task WaitUntilCanStart_WhileSpeaking_WaitsInsteadOfFailingTheTurn()
    {
        var gate = new StubMicGateState(isHardOff: true);
        var guard = new ActiveCaptureMicGateGuard(gate);

        var wait = guard.WaitUntilCanStartAsync(CancellationToken.None).AsTask();

        Assert.False(wait.IsCompleted, "eine laufende Wiedergabe darf den Zug nicht sofort beenden");

        gate.Open();

        await wait.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task WaitUntilCanStart_WhenTheTurnIsCancelledWhileSpeaking_Cancels()
    {
        var guard = new ActiveCaptureMicGateGuard(new StubMicGateState(isHardOff: true));
        using var cancellation = new CancellationTokenSource();

        var wait = guard.WaitUntilCanStartAsync(cancellation.Token).AsTask();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
    }
}
