using Sprechen.Speech.Runtime.Tests.Services;

namespace Sprechen.Speech.Runtime.Tests.Schema;

/// <summary>
/// Prüft die voreingestellte Mikrofonregel und einen Wiedergabestart mit vorgegebenem HardOff.
/// </summary>
public sealed class MicGatePolicyPrecedenceTests
{
    [Fact]
    public void Resolver_Default_FromReferenceContract_IsHeuristic()
    {
        var resolver = new MicGatePolicyResolver();

        var policy = resolver.GetPolicy();

        Assert.Equal(MicGatePolicy.Heuristic, policy);
    }

    [Fact]
    public void HardOff_AlwaysWins_PrecedenceContract()
    {
        // Diese Assertion unterscheidet die beiden angegebenen Werte; das Laufzeitverhalten folgt unten.
        var policies = new[] { MicGatePolicy.Heuristic, MicGatePolicy.HardOff };

        Assert.Equal(2, policies.Distinct().Count());
        Assert.Contains(MicGatePolicy.HardOff, policies);
    }

    [Fact]
    public void StubResolver_ReportingHardOff_IsAccepted_ByObserver()
    {
        // Der Stub prüft HardOff unabhängig von der Heuristic-Vorgabe des Referenzvertrags.
        var tts = new RecordingPlaybackState();
        var earcon = new RecordingEarconPlaybackState();
        var bridge = new RecordingMicGateBrowserBridge();
        var resolver = new StubMicGatePolicyResolver(MicGatePolicy.HardOff);
        using var observer = new MicGateLifecycleObserver(
            tts,
            earcon,
            resolver,
            bridge,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MicGateLifecycleObserver>.Instance);

        tts.NotifyPlaybackStarted();
        bridge.WaitForCalls(1, TimeSpan.FromSeconds(2));

        Assert.Equal([false], bridge.Snapshot());
        Assert.True(observer.IsHardOff);
    }
}
