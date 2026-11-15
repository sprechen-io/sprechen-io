namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Fehlerschwelle und wachsende Wartezeiten für Aktivierungshinweise
/// mit einer steuerbaren Uhr. Eine erfolgreiche Aktivierung unterdrückt weitere Hinweise.
/// </summary>
public sealed class WakeRecoveryGuidanceGateTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    private static WakeRecoveryGuidance Guidance(int missThreshold = 2, int cooldownSeconds = 60, int maxCooldownSeconds = 300)
        => new(
            "Um zu starten, sagen Sie: Okay Alex.",
            "Um zu starten, sagen Sie: Okay Alex. Sie können auch Hey Alex oder Ok Alex sagen.",
            "de",
            missThreshold,
            TimeSpan.FromSeconds(cooldownSeconds),
            TimeSpan.FromSeconds(maxCooldownSeconds));

    [Fact]
    public void ShouldSpeakGuidance_BelowThreshold_IsFalse()
    {
        var gate = new WakeRecoveryGuidanceGate(new MutableClock(T0));
        var guidance = Guidance(missThreshold: 2);

        gate.RecordMiss();

        Assert.False(gate.ShouldSpeakGuidance(guidance));
    }

    [Fact]
    public void ShouldSpeakGuidance_OnNthConsecutiveMiss_IsTrue()
    {
        var gate = new WakeRecoveryGuidanceGate(new MutableClock(T0));
        var guidance = Guidance(missThreshold: 2);

        gate.RecordMiss();
        gate.RecordMiss();

        Assert.True(gate.ShouldSpeakGuidance(guidance));
    }

    [Fact]
    public void ShouldSpeakGuidance_AfterFirstWake_IsNeverTrue()
    {
        var gate = new WakeRecoveryGuidanceGate(new MutableClock(T0));
        var guidance = Guidance(missThreshold: 2);

        gate.RecordWake();
        gate.RecordMiss();
        gate.RecordMiss();
        gate.RecordMiss();

        Assert.False(gate.ShouldSpeakGuidance(guidance));
    }

    [Fact]
    public void RecordWake_ResetsTheMissStreak()
    {
        // Nach der Aktivierung bleiben Hinweise gesperrt; dieser Test isoliert
        // die Rücksetzung des Fehlerzählers nicht von dieser Sperre.
        var gate = new WakeRecoveryGuidanceGate(new MutableClock(T0));
        var guidance = Guidance(missThreshold: 2);

        gate.RecordMiss();
        gate.RecordWake();
        gate.RecordMiss();

        Assert.False(gate.ShouldSpeakGuidance(guidance));
    }

    [Fact]
    public void ShouldSpeakGuidance_WithinCooldown_IsSuppressed_AndResumesAfterCooldown()
    {
        var clock = new MutableClock(T0);
        var gate = new WakeRecoveryGuidanceGate(clock);
        var guidance = Guidance(missThreshold: 2, cooldownSeconds: 60);

        gate.RecordMiss();
        gate.RecordMiss();
        Assert.True(gate.ShouldSpeakGuidance(guidance));
        gate.RecordGuidanceSpoken();

        // Neue Fehlversuche dürfen die laufende Wartezeit nicht umgehen.
        gate.RecordMiss();
        gate.RecordMiss();
        Assert.False(gate.ShouldSpeakGuidance(guidance));

        clock.UtcNow = T0.AddSeconds(60);
        Assert.True(gate.ShouldSpeakGuidance(guidance));
    }

    [Fact]
    public void ShouldSpeakGuidance_PersistentMisses_NeverGoBuzzOnly_AndKeepFiringPastTheOldCap()
    {
        // Acht aufeinanderfolgende Hinweisfreigaben prüfen, dass eine niedrige
        // feste Obergrenze die Unterstützung nicht vorzeitig beendet.
        var clock = new MutableClock(T0);
        var gate = new WakeRecoveryGuidanceGate(clock);
        var guidance = Guidance(missThreshold: 2, cooldownSeconds: 60, maxCooldownSeconds: 300);

        for (var emission = 1; emission <= 8; emission++)
        {
            // Der Zeitfortschritt überschreitet jede konfigurierte Wartezeit.
            clock.UtcNow = T0.AddSeconds(emission * 10_000);
            gate.RecordMiss();
            gate.RecordMiss();
            Assert.True(gate.ShouldSpeakGuidance(guidance), $"guidance emission {emission} must still fire (no buzz-only dead-end)");
            gate.RecordGuidanceSpoken();
        }
    }

    [Fact]
    public void ShouldSpeakGuidance_CooldownEscalatesByDoubling_ThenHoldsAtTheCeiling()
    {
        // Die Wartezeit verdoppelt sich von 60 auf 120 und 240 Sekunden
        // und bleibt anschließend auf 300 Sekunden begrenzt.
        var clock = new MutableClock(T0);
        var gate = new WakeRecoveryGuidanceGate(clock);
        var guidance = Guidance(missThreshold: 2, cooldownSeconds: 60, maxCooldownSeconds: 300);

        int[] expectedIntervals = [60, 120, 240, 300, 300];
        var now = T0;

        gate.RecordMiss();
        gate.RecordMiss();
        Assert.True(gate.ShouldSpeakGuidance(guidance));
        gate.RecordGuidanceSpoken();

        foreach (var interval in expectedIntervals)
        {
            // Eine neue vollständige Fehlerfolge reicht vor Ablauf der Wartezeit noch nicht aus.
            gate.RecordMiss();
            gate.RecordMiss();
            clock.UtcNow = now.AddSeconds(interval - 1);
            Assert.False(gate.ShouldSpeakGuidance(guidance), $"must be suppressed before {interval}s backoff elapses");

            clock.UtcNow = now.AddSeconds(interval);
            Assert.True(gate.ShouldSpeakGuidance(guidance), $"must resume at {interval}s backoff");
            gate.RecordGuidanceSpoken();
            now = clock.UtcNow;
        }
    }

    [Fact]
    public void SelectGuidanceText_UsesPrimaryFirst_ThenTheVaryingAlternativesHelp()
    {
        var clock = new MutableClock(T0);
        var gate = new WakeRecoveryGuidanceGate(clock);
        var guidance = Guidance(missThreshold: 2, cooldownSeconds: 60, maxCooldownSeconds: 300);

        gate.RecordMiss();
        gate.RecordMiss();
        Assert.True(gate.ShouldSpeakGuidance(guidance));
        Assert.Equal(guidance.PrimaryGuidanceText, gate.SelectGuidanceText(guidance));
        gate.RecordGuidanceSpoken();

        clock.UtcNow = T0.AddSeconds(60);
        gate.RecordMiss();
        gate.RecordMiss();
        Assert.True(gate.ShouldSpeakGuidance(guidance));
        Assert.Equal(guidance.AlternativesGuidanceText, gate.SelectGuidanceText(guidance));
        Assert.NotEqual(guidance.PrimaryGuidanceText, gate.SelectGuidanceText(guidance));
        gate.RecordGuidanceSpoken();

        // Auch die dritte Freigabe verwendet den Text mit alternativen Aktivierungsformen.
        clock.UtcNow = T0.AddSeconds(60 + 120);
        gate.RecordMiss();
        gate.RecordMiss();
        Assert.True(gate.ShouldSpeakGuidance(guidance));
        Assert.Equal(guidance.AlternativesGuidanceText, gate.SelectGuidanceText(guidance));
    }

    [Fact]
    public void RecordGuidanceSpoken_ResetsStreak_SoAFreshRunOfMissesIsRequired()
    {
        var clock = new MutableClock(T0);
        var gate = new WakeRecoveryGuidanceGate(clock);
        var guidance = Guidance(missThreshold: 2, cooldownSeconds: 0, maxCooldownSeconds: 0);

        gate.RecordMiss();
        gate.RecordMiss();
        Assert.True(gate.ShouldSpeakGuidance(guidance));
        gate.RecordGuidanceSpoken();

        // Bei Wartezeit null zeigt die erneute Fehlerschwelle allein die Rücksetzung des Zählers.
        gate.RecordMiss();
        Assert.False(gate.ShouldSpeakGuidance(guidance));

        gate.RecordMiss();
        Assert.True(gate.ShouldSpeakGuidance(guidance));
    }

    [Fact]
    public void Constructor_WhenClockIsNull_Throws()
        => Assert.Throws<ArgumentNullException>(() => new WakeRecoveryGuidanceGate(clock: null!));
}
