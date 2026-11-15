using Sprechen.Speech.TestDoubles;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Fehlerschwelle, wachsende Wartezeit und Rücksetzung der Mikrofonhinweise.
/// </summary>
public sealed class MicrophoneAvailabilityGateTests
{
    private static MicrophoneAvailabilityGuidance Guidance(int threshold = 3, int cooldownSeconds = 30, int maxCooldownSeconds = 120)
        => new(
            deviceNotFoundText: "no mic",
            permissionDeniedText: "blocked",
            unavailableText: "unavailable",
            recoveryText: "available again",
            isoCode: "de",
            unavailableThreshold: threshold,
            cooldown: TimeSpan.FromSeconds(cooldownSeconds),
            maxCooldown: TimeSpan.FromSeconds(maxCooldownSeconds));

    [Fact]
    public void ShouldSpeak_OnlyAfterThresholdConsecutiveUnavailableCaptures()
    {
        var gate = new MicrophoneAvailabilityGate(new MutableClock(DateTimeOffset.UnixEpoch));
        var guidance = Guidance(threshold: 3);

        gate.RecordUnavailable();
        Assert.False(gate.ShouldSpeak(guidance));
        gate.RecordUnavailable();
        Assert.False(gate.ShouldSpeak(guidance));
        gate.RecordUnavailable();
        Assert.True(gate.ShouldSpeak(guidance));
    }

    [Fact]
    public void RecordHealthyCapture_RequiresAFreshThreshold()
    {
        var gate = new MicrophoneAvailabilityGate(new MutableClock(DateTimeOffset.UnixEpoch));
        var guidance = Guidance(threshold: 2);

        gate.RecordUnavailable();
        Assert.False(gate.RecordHealthyCapture());
        gate.RecordUnavailable();
        Assert.False(gate.ShouldSpeak(guidance)); // Nach der erfolgreichen Aufnahme zählt erst wieder ein Fehler.

        gate.RecordUnavailable();
        Assert.True(gate.ShouldSpeak(guidance));
    }

    [Fact]
    public void RecoveryAcknowledgement_RemainsPendingUntilSuccessfulPlaybackIsCommitted()
    {
        var gate = new MicrophoneAvailabilityGate(new MutableClock(DateTimeOffset.UnixEpoch));
        var guidance = Guidance(threshold: 1);

        gate.RecordUnavailable();
        Assert.True(gate.ShouldSpeak(guidance));
        gate.RecordAnnouncementSpoken();

        Assert.True(gate.RecordHealthyCapture());
        Assert.True(gate.RecordHealthyCapture());
        gate.RecordRecoveryAnnouncementSpoken();
        Assert.False(gate.RecordHealthyCapture());
    }

    [Fact]
    public void RecordWake_ResetsStreakButLaterPermanentFailureRearmsAnnouncement()
    {
        var gate = new MicrophoneAvailabilityGate(new MutableClock(DateTimeOffset.UnixEpoch));
        var guidance = Guidance(threshold: 2);

        gate.RecordUnavailable();
        gate.RecordWake();
        gate.RecordUnavailable();
        Assert.False(gate.ShouldSpeak(guidance));
        gate.RecordUnavailable();
        Assert.True(gate.ShouldSpeak(guidance));
    }

    [Fact]
    public void RecordWake_AfterFailedRecoveryPlayback_KeepsRecoveryPendingUntilItIsSpoken()
    {
        var gate = new MicrophoneAvailabilityGate(new MutableClock(DateTimeOffset.UnixEpoch));

        gate.RecordUnavailable();
        gate.RecordAnnouncementSpoken();
        Assert.True(gate.RecordHealthyCapture());

        gate.RecordWake();

        Assert.True(gate.RecordHealthyCapture());
        gate.RecordRecoveryAnnouncementSpoken();
        Assert.False(gate.RecordHealthyCapture());
    }

    [Fact]
    public void Cooldown_HoldsBetweenAnnouncements_AndDoublesAfterEach()
    {
        var clock = new MutableClock(DateTimeOffset.UnixEpoch);
        var gate = new MicrophoneAvailabilityGate(clock);
        var guidance = Guidance(threshold: 1, cooldownSeconds: 30, maxCooldownSeconds: 120);

        gate.RecordUnavailable();
        Assert.True(gate.ShouldSpeak(guidance));
        gate.RecordAnnouncementSpoken();

        // Eine neue Fehlerfolge hebt die laufende Wartezeit nicht auf.
        clock.UtcNow += TimeSpan.FromSeconds(20);
        gate.RecordUnavailable();
        Assert.False(gate.ShouldSpeak(guidance));

        clock.UtcNow += TimeSpan.FromSeconds(11);
        Assert.True(gate.ShouldSpeak(guidance));
        gate.RecordAnnouncementSpoken();

        // Nach dem zweiten Hinweis gilt die doppelte Wartezeit von 60 Sekunden.
        clock.UtcNow += TimeSpan.FromSeconds(40);
        gate.RecordUnavailable();
        Assert.False(gate.ShouldSpeak(guidance));

        clock.UtcNow += TimeSpan.FromSeconds(25);
        Assert.True(gate.ShouldSpeak(guidance));
    }

    [Fact]
    public void BeginListeningRun_ClearsTheCooldownAnchorButKeepsEscalationAndPendingRecovery()
    {
        // Ein neuer Zuhörlauf hebt die laufende Wartezeit auf. Die Anzahl bereits ausgegebener
        // Hinweise und die ausstehende Meldung über die Wiederherstellung bleiben erhalten.
        var clock = new MutableClock(DateTimeOffset.UnixEpoch);
        var gate = new MicrophoneAvailabilityGate(clock);
        var guidance = Guidance(threshold: 2, cooldownSeconds: 60, maxCooldownSeconds: 300);

        gate.RecordUnavailable();
        gate.RecordUnavailable();
        Assert.True(gate.ShouldSpeak(guidance));
        gate.RecordAnnouncementSpoken();

        gate.RecordUnavailable();
        gate.RecordUnavailable();
        Assert.False(gate.ShouldSpeak(guidance));

        gate.BeginListeningRun();
        Assert.True(gate.ShouldSpeak(guidance));
        gate.RecordAnnouncementSpoken();

        // Der zweite Hinweis verlängert die Wartezeit auf 120 Sekunden.
        gate.RecordUnavailable();
        gate.RecordUnavailable();
        clock.UtcNow += TimeSpan.FromSeconds(61);
        Assert.False(gate.ShouldSpeak(guidance));
        clock.UtcNow += TimeSpan.FromSeconds(60);
        Assert.True(gate.ShouldSpeak(guidance));

        Assert.True(gate.RecordHealthyCapture());
    }

    [Fact]
    public void ShouldSpeak_IsPureQuery_DoesNotAdvanceState()
    {
        var gate = new MicrophoneAvailabilityGate(new MutableClock(DateTimeOffset.UnixEpoch));
        var guidance = Guidance(threshold: 1);

        gate.RecordUnavailable();
        Assert.True(gate.ShouldSpeak(guidance));
        Assert.True(gate.ShouldSpeak(guidance));
    }
}
