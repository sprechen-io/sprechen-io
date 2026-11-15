namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft <see cref="EscalatingCooldown"/> für wachsende Abstände zwischen wiederholten Hinweisen.
/// Die Tabelle enthält feste Sollwerte für Verdopplung und Obergrenze.
/// </summary>
public sealed class EscalatingCooldownTests
{
    [Theory]
    // Ein Grundabstand von null bleibt auch beim Verdoppeln null.
    [InlineData(0, 0, 0, 0)]
    [InlineData(0, 0, 8, 0)]
    [InlineData(0, 300, 0, 0)]
    [InlineData(0, 300, 8, 0)]
    // Entspricht die Obergrenze dem Grundabstand, bleibt das Intervall konstant.
    [InlineData(30, 30, 0, 30)]
    [InlineData(30, 30, 3, 30)]
    [InlineData(30, 30, 8, 30)]
    // Eine Verdopplung passt; weitere Schritte bleiben an der Obergrenze.
    [InlineData(30, 60, 0, 30)]
    [InlineData(30, 60, 1, 30)]
    [InlineData(30, 60, 2, 60)]
    [InlineData(30, 60, 3, 60)]
    [InlineData(30, 60, 8, 60)]
    // Zwei Verdopplungen passen; höhere Zähler ändern das Intervall nicht mehr.
    [InlineData(30, 120, 0, 30)]
    [InlineData(30, 120, 1, 30)]
    [InlineData(30, 120, 2, 60)]
    [InlineData(30, 120, 3, 120)]
    [InlineData(30, 120, 4, 120)]
    [InlineData(30, 120, 8, 120)]
    // Liegt die Obergrenze zwischen zwei Verdopplungen, wird der letzte Schritt gekürzt.
    [InlineData(30, 300, 2, 60)]
    [InlineData(30, 300, 3, 120)]
    [InlineData(30, 300, 4, 240)]
    [InlineData(30, 300, 5, 300)]
    [InlineData(30, 300, 8, 300)]
    [InlineData(60, 300, 0, 60)]
    [InlineData(60, 300, 1, 60)]
    [InlineData(60, 300, 2, 120)]
    [InlineData(60, 300, 3, 240)]
    [InlineData(60, 300, 4, 300)]
    [InlineData(60, 300, 8, 300)]
    public void IntervalAfter_MatchesTheIntervalsMeasuredBeforeTheCut(
        int baseSeconds,
        int ceilingSeconds,
        int spokenCount,
        int expectedSeconds)
    {
        var cooldown = new EscalatingCooldown(
            TimeSpan.FromSeconds(baseSeconds),
            TimeSpan.FromSeconds(ceilingSeconds));

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), cooldown.IntervalAfter(spokenCount));
    }

    [Fact]
    public void IntervalAfter_NeverExceedsTheCeiling_ForAnyCount()
    {
        var cooldown = new EscalatingCooldown(TimeSpan.FromSeconds(45), TimeSpan.FromSeconds(200));

        for (var spokenCount = 0; spokenCount <= 64; spokenCount++)
        {
            Assert.True(cooldown.IntervalAfter(spokenCount) <= TimeSpan.FromSeconds(200));
        }
    }

    [Fact]
    public void BothGuidanceKinds_ShareOneRule_ForTheSameModeledValues()
    {
        // Mikrofon- und Aktivierungshinweise haben eigene Texte und Schwellen,
        // verwenden aber bei gleichen Zeitvorgaben dieselben Wiederholungsabstände.
        var microphone = new MicrophoneAvailabilityGuidance(
            deviceNotFoundText: "kein Mikrofon",
            permissionDeniedText: "keine Freigabe",
            unavailableText: "nicht verfuegbar",
            recoveryText: "wieder da",
            isoCode: "de",
            unavailableThreshold: 3,
            cooldown: TimeSpan.FromSeconds(30),
            maxCooldown: TimeSpan.FromSeconds(300));
        var wake = new WakeRecoveryGuidance(
            primaryGuidanceText: "sag Sprechen",
            alternativesGuidanceText: "oder nutze die Taste",
            isoCode: "de",
            missThreshold: 2,
            cooldown: TimeSpan.FromSeconds(30),
            maxCooldown: TimeSpan.FromSeconds(300));

        for (var spokenCount = 0; spokenCount <= 8; spokenCount++)
        {
            Assert.Equal(
                microphone.Backoff.IntervalAfter(spokenCount),
                wake.Backoff.IntervalAfter(spokenCount));
        }
    }

    [Fact]
    public void Constructor_WhenTheBaseIntervalIsNegative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new EscalatingCooldown(TimeSpan.FromSeconds(-1), TimeSpan.FromSeconds(300)));
    }

    [Fact]
    public void Constructor_WhenTheCeilingIsBelowTheBaseInterval_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new EscalatingCooldown(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30)));
    }

    [Fact]
    public void IntervalAfter_WhenTheSpokenCountIsNegative_Throws()
    {
        var cooldown = new EscalatingCooldown(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(300));

        Assert.Throws<ArgumentOutOfRangeException>(() => cooldown.IntervalAfter(-1));
    }
}
