using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.ModelRuntime.PromptTemplates;
using Sprechen.Speech.ModelRuntime.Services;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Reihenfolge der Sprachangebote, Annahme, Speicherung, Aufnahmemodus
/// und Verhalten bei deaktivierter Einstiegsentscheidung.
/// </summary>
public sealed class SessionEntryLanguageOfferTurnTests
{
    [Fact]
    public async Task SwitchedOff_SaysNothingAtAll_AndKeepsTheResolvedLanguage()
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(enabled: false);

        var result = await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), result);
        Assert.Empty(harness.Spoken);
        Assert.Empty(harness.Persisted);
    }

    [Fact]
    public async Task TheFirstQuestion_IsTheSessionsOwnLanguage_AskedInIt()
    {
        // Zuerst wird die aufgelöste Sitzungssprache angeboten, damit sie mit einer Antwort beibehalten werden kann.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(enabled: true, transcripts: ["Deutsch"]);

        await harness.Turn.DecideLanguageAsync("de", Deadline());

        var expected = SessionLanguageOfferPromptTextResolver.GetKeepQuestionSegment("de", PolitenessLevel.Formal);
        var first = harness.Spoken[0];
        Assert.Equal(expected.Text, first.Text);
        Assert.Equal("de", first.IsoCode);

        // Die Frage nennt ihre Sprache und macht damit die mögliche Antwort ausdrücklich.
        Assert.Contains("Deutsch", first.Text, StringComparison.Ordinal);
    }

    [Theory]
    // Der Sprachname benennt die gewünschte Sprache eindeutig.
    [InlineData("Deutsch")]
    // Das deutsche Ja nimmt die deutsche Frage ebenfalls an.
    [InlineData("ja")]
    public async Task AcceptingTheSessionLanguage_EndsTheLadderAtOnce(string answer)
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(enabled: true, transcripts: [answer]);

        var result = await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), result);

        // Nach Annahme der ersten Frage darf kein weiteres Antwortfenster folgen.
        Assert.Equal(1, harness.SpeechClient.RecordCallCount);
        Assert.Empty(harness.Persisted);
    }

    [Fact]
    public async Task NotAcceptingTheSessionLanguage_OffersTheNextOne_InItsOwnVoice()
    {
        // Eine Ablehnung führt zum nächsten Sprachangebot.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(enabled: true, transcripts: ["nein", "yes"]);

        var result = await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Equal(SessionEntryOutcome.ContinueIn("en"), result);

        var second = harness.Spoken[1];
        Assert.Equal("en", second.IsoCode);
        Assert.Equal(
            SessionLanguageOfferPromptTextResolver.GetLanguageQuestionSegment("en", PolitenessLevel.Formal).Text,
            second.Text);
    }

    [Fact]
    public async Task TheLadderWalksEveryOtherLanguage_InTheCatalogsOrder()
    {
        // Die Katalogreihenfolge hält die Angebotsfolge bei wiederholtem Einstieg in derselben Sprache vorhersehbar.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: SessionEntryLanguageOfferTurnHarness.SilenceOnEveryRung());

        await harness.Turn.DecideLanguageAsync("de", Deadline());

        // Ohne den Abschlusssatz am Ende: der gehört zum Ausgang, nicht zur Leiter.
        Assert.Equal(
            ["de", .. VoiceInterfaceModelCatalog.SupportedLanguages.Where(iso => iso != "de")],
            harness.Spoken.Take(VoiceInterfaceModelCatalog.SupportedLanguages.Count).Select(segment => segment.IsoCode));
    }

    [Fact]
    public async Task AcceptingAForeignLanguage_ChangesTheSession_AndStoresIt()
    {
        // Der italienische Eigenname nimmt das letzte Angebot an und wählt Italienisch.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: [null, null, null, null, "italiano"]);

        var result = await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Equal(SessionEntryOutcome.ContinueIn("it"), result);

        // Die Auswahl muss gespeichert werden, damit der nächste Start sie nicht wieder durch die frühere Sprache ersetzt.
        Assert.Equal(["it"], harness.Persisted);
    }

    [Fact]
    public async Task AnAcceptedQuestion_IsNotConfirmedASecondTime()
    {
        // Das Sprachangebot ist bereits eine Bestätigungsfrage. Seine Annahme darf kein
        // zusätzliches Aufnahmefenster nach den fünf vorgesehenen Fenstern öffnen.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: [null, null, null, null, "italiano"]);

        await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Equal(5, harness.SpeechClient.RecordCallCount);

        // Die abschließende Mitteilung benennt die geltende Sprache. Ansage- und Aufnahmezahl
        // werden getrennt geprüft, um eine Mitteilung von einer weiteren Frage zu unterscheiden.
        Assert.Equal(6, harness.Spoken.Count);
        Assert.Equal("it", harness.Spoken[^1].IsoCode);
    }

    [Fact]
    public async Task NobodyAcceptingAnything_KeepsTheLanguage_AndSaysNothingFurther()
    {
        // Ohne Annahme bleibt die bisherige Sprache erhalten; ein weiteres Antwortfenster wird nicht geöffnet.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: SessionEntryLanguageOfferTurnHarness.SilenceOnEveryRung());

        var result = await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), result);
        Assert.Empty(harness.Persisted);
        Assert.Equal(
            VoiceInterfaceModelCatalog.SupportedLanguages.Count,
            harness.SpeechClient.RecordCallCount);
    }

    [Fact]
    public async Task AnAnswerIsReadInTheLanguageItsQuestionWasAskedIn()
    {
        // Zustimmung gilt für die gerade angebotene Sprache: Deutsches Ja darf das
        // italienische Angebot nicht annehmen, das deutsche Angebot dagegen schon.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: [null, null, null, null, "ja"]);

        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), await harness.Turn.DecideLanguageAsync("de", Deadline()));

        var german = SessionEntryLanguageOfferTurnHarness.Create(enabled: true, transcripts: ["ja"]);

        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), await german.Turn.DecideLanguageAsync("de", Deadline()));
        Assert.Equal(1, german.SpeechClient.RecordCallCount);
    }

    [Fact]
    public async Task SayingNo_ToEveryQuestion_LeavesEverythingAsItWas()
    {
        // Werden alle Angebote abgelehnt, müssen Sprache und gespeicherte Präferenz unverändert bleiben.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: ["nein", "no", "non", "no", "no"]);

        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), await harness.Turn.DecideLanguageAsync("de", Deadline()));
        Assert.Empty(harness.Persisted);
    }

    [Fact]
    public async Task TheDecision_IsPutOncePerSession()
    {
        // Nach abgeschlossenem Einstieg darf dieselbe Instanz bei erneutem Aufruf nicht nochmals fragen.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(enabled: true, transcripts: ["Deutsch"]);

        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), await harness.Turn.DecideLanguageAsync("de", Deadline()));
        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), await harness.Turn.DecideLanguageAsync("de", Deadline()));
        Assert.Equal(1, harness.SpeechClient.RecordCallCount);
    }

    [Fact]
    public async Task ThePersistedLanguageIsWhatTheSessionContinuesIn_EvenWhenTheStoreDisagrees()
    {
        // Die Speicherantwort bestimmt die tatsächlich gespeicherte Sprache. Ist sie
        // unbrauchbar, bleibt die Sitzung bei ihrer bisherigen Sprache.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: [null, null, null, null, "italiano"], persistedIsoCodeOverride: " ");

        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), await harness.Turn.DecideLanguageAsync("de", Deadline()));
    }

    [Fact]
    public async Task EveryAnswer_IsRecordedAsAShortConfirmation()
    {
        // ShortConfirmation ist für kurze Antworten nach Wiedergabe vorgesehen; der Test prüft die Modusanfrage, keine Erkennungsqualität.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: SessionEntryLanguageOfferTurnHarness.SilenceOnEveryRung());

        await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.All(
            harness.SpeechClient.RecordModes,
            mode => Assert.Equal(SpeechCaptureMode.ShortConfirmation, mode));
    }

    /// <summary>
    /// Begrenzt den Testlauf, damit fehlende skriptierte Antworten nicht unbegrenzt warten lassen.
    /// </summary>
    private static CancellationToken Deadline()
        => new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;
}
