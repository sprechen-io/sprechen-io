namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Entscheidung zur Wiederholung eines Hinweises: nur bei Freigabe
/// und vorhandenem Text. Die eigentliche Audioausgabe ist nicht Teil dieser Tests.
/// </summary>
public sealed class CaptureReplayPolicyTests
{
    [Fact]
    public void Decide_Relation_ReVoicesExactlyTheCurrentFieldTurnAnnouncement()
    {
        var policy = new CaptureReplayPolicy();

        var decision = policy.Decide(autoReplayEligible: true, "Das Datum muss in der Vergangenheit liegen.");

        Assert.True(decision.ShouldReplay);
        Assert.Equal("Das Datum muss in der Vergangenheit liegen.", decision.Announcement);
    }

    [Fact]
    public void Decide_DeadInput_DoesNotReplay()
    {
        var policy = new CaptureReplayPolicy();

        // Nach einem Aufnahmefehler kann der Aufrufer die Wiederholung sperren.
        // Der Test prüft diese Sperre; er erzeugt keinen tatsächlichen Mikrofonfehler.
        var decision = policy.Decide(autoReplayEligible: false, "Das Datum muss in der Vergangenheit liegen.");

        Assert.False(decision.ShouldReplay);
        Assert.Null(decision.Announcement);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Decide_NoFreshAnnouncement_DoesNothing(string? announcement)
    {
        var policy = new CaptureReplayPolicy();

        var decision = policy.Decide(autoReplayEligible: true, announcement);

        Assert.False(decision.ShouldReplay);
        Assert.Null(decision.Announcement);
    }
}
