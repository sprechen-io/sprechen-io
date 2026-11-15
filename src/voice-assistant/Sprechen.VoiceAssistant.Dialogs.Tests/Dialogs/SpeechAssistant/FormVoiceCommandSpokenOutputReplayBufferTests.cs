namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandSpokenOutputReplayBufferTests
{
    [Fact]
    public void Record_ThenLastSpokenOutput_ReturnsRecordedText()
    {
        var buffer = new FormVoiceCommandSpokenOutputReplayBuffer();

        buffer.Record("Die Eingabe ist hier nicht gueltig.");

        Assert.Equal("Die Eingabe ist hier nicht gueltig.", buffer.LastSpokenOutput);
    }

    [Fact]
    public void Record_KeepsOnlyTheMostRecentOutput()
    {
        var buffer = new FormVoiceCommandSpokenOutputReplayBuffer();

        // Der Speicher hält nur den zuletzt hinterlegten Text zur Wiederholung vor.
        buffer.Record("Pflichtfeld darf nicht leer sein.");
        buffer.Record("Ich konnte nicht aufnehmen. Bitte erneut sprechen.");

        Assert.Equal("Ich konnte nicht aufnehmen. Bitte erneut sprechen.", buffer.LastSpokenOutput);
    }

    [Fact]
    public void Record_TrimsAndIgnoresBlankOutput()
    {
        var buffer = new FormVoiceCommandSpokenOutputReplayBuffer();

        buffer.Record("  Letzte Meldung.  ");
        buffer.Record("   ");

        Assert.Equal("Letzte Meldung.", buffer.LastSpokenOutput);
    }

    [Fact]
    public void LastSpokenOutput_WhenNothingRecorded_IsNull()
    {
        var buffer = new FormVoiceCommandSpokenOutputReplayBuffer();

        Assert.Null(buffer.LastSpokenOutput);
    }

    [Fact]
    public void CurrentFieldTurnAnnouncement_Relation_ExposesTheAnnouncementRecordedForThatFieldTurn()
    {
        var buffer = new FormVoiceCommandSpokenOutputReplayBuffer();

        buffer.BeginFieldTurn("date_of_birth");
        buffer.Record("Das Datum muss in der Vergangenheit liegen.");

        Assert.Equal("Das Datum muss in der Vergangenheit liegen.", buffer.CurrentFieldTurnAnnouncement);
    }

    [Fact]
    public void CurrentFieldTurnAnnouncement_StaleCrossField_FieldBSilenceNeverReVoicesFieldA()
    {
        var buffer = new FormVoiceCommandSpokenOutputReplayBuffer();

        // Nach dem Beginn eines anderen Felddurchgangs darf die vorherige Feldansage nicht mehr automatisch wiederholt werden.
        buffer.BeginFieldTurn("field_a");
        buffer.Record("Feld A: ungueltig.");
        buffer.BeginFieldTurn("field_b");

        Assert.Null(buffer.CurrentFieldTurnAnnouncement);
    }

    [Fact]
    public void CurrentFieldTurnAnnouncement_QuantityCap_IsSpentAfterBeingMarkedReplayed()
    {
        var buffer = new FormVoiceCommandSpokenOutputReplayBuffer();
        buffer.BeginFieldTurn("email");
        buffer.Record("Bitte geben Sie eine gueltige E-Mail an.");

        var first = buffer.CurrentFieldTurnAnnouncement;
        buffer.MarkCurrentFieldTurnAnnouncementReplayed();
        var second = buffer.CurrentFieldTurnAnnouncement;

        Assert.Equal("Bitte geben Sie eine gueltige E-Mail an.", first);
        Assert.Null(second);
    }

    [Fact]
    public void CurrentFieldTurnAnnouncement_ReArmsWhenAFreshAnnouncementIsRecordedAfterAReplay()
    {
        var buffer = new FormVoiceCommandSpokenOutputReplayBuffer();
        buffer.BeginFieldTurn("amount");
        buffer.Record("Erste Meldung.");
        buffer.MarkCurrentFieldTurnAnnouncementReplayed();

        // Ein neuer Text desselben Feldes wird wieder zur einmaligen automatischen Wiederholung verfügbar.
        buffer.Record("Zweite Meldung.");

        Assert.Equal("Zweite Meldung.", buffer.CurrentFieldTurnAnnouncement);
    }

    [Fact]
    public void LastSpokenOutput_StaysStaleTolerantAcrossFields_SoAnchoredRepeatIsUnchanged()
    {
        var buffer = new FormVoiceCommandSpokenOutputReplayBuffer();

        // Der letzte Text bleibt für einen ausdrücklichen Wiederholungsbefehl erhalten, auch wenn die automatische Feldwiederholung bereits verbraucht ist.
        buffer.BeginFieldTurn("field_a");
        buffer.Record("Feld A: ungueltig.");
        buffer.BeginFieldTurn("field_b");
        buffer.MarkCurrentFieldTurnAnnouncementReplayed();

        Assert.Equal("Feld A: ungueltig.", buffer.LastSpokenOutput);
        Assert.Null(buffer.CurrentFieldTurnAnnouncement);
    }
}
