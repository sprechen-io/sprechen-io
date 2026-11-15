using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft den sprachbezogenen Erstkontakt-Merker und die Auswahl des Antwortfensters.
/// Nur ein angenommener Einstieg setzt den Merker; ein fehlender oder unlesbarer Merker
/// führt zum längeren Erstkontaktfenster.
/// </summary>
public sealed class SessionEntryFirstContactAndRepeatTests
{
    [Theory]
    [InlineData("Deutsch")]
    [InlineData("ja")]
    public async Task EinBeantworteterEinstieg_HaeltFestDassGefragtWurde(string firstAnswer)
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true,
            transcripts: [firstAnswer],
            entryOfferHeardIsoCode: null);

        await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Equal(["de"], harness.MarkedAsHeard);
    }

    [Theory]
    [InlineData(null)]
    // Ein deutsches Nein auf jeder Stufe führt ebenfalls zu keiner Zustimmung.
    [InlineData("nein")]
    public async Task EinEinstiegOhneZustimmung_HaeltNichtsFest(string? everyAnswer)
    {
        // Ohne angenommene Antwort bleibt der Merker leer, damit der nächste Einstieg
        // weiterhin das Erstkontaktfenster erhält.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true,
            transcripts: [everyAnswer, everyAnswer, everyAnswer, everyAnswer, everyAnswer],
            entryOfferHeardIsoCode: null);

        await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Empty(harness.MarkedAsHeard);
    }

    [Fact]
    public async Task NachEinemWechsel_TraegtDerMerkerDieJetztGeltendeSprache()
    {
        // Der Merker speichert die angenommene Sprache, damit ein späterer Einstieg in dieser
        // Sprache das Wiederholungsfenster verwenden kann.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: [null, null, null, null, "italiano"], entryOfferHeardIsoCode: null);

        Assert.Equal(SessionEntryOutcome.ContinueIn("it"), await harness.Turn.DecideLanguageAsync("de", Deadline()));
        Assert.Equal(["it"], harness.MarkedAsHeard);
    }

    [Fact]
    public async Task DerErstkontakt_BekommtDasLangeFenster()
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: ["Deutsch"], entryOfferHeardIsoCode: null);

        await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Equal([(TimeSpan?)Requested(profile => profile.FirstContactInitialSilenceTimeoutMs)],
            harness.SpeechClient.AnswerWindows);
    }

    [Fact]
    public async Task DieZweiteSitzungInDerselbenSprache_BekommtDasKurzeFenster()
    {
        // Die erste Sitzung schreibt den Merker, die zweite liest ihn über den Speicher.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: ["Deutsch", "Deutsch"], entryOfferHeardIsoCode: null);

        await harness.Turn.DecideLanguageAsync("de", Deadline());
        await harness.NextSession().DecideLanguageAsync("de", Deadline());

        Assert.Equal(
            [
                (TimeSpan?)Requested(profile => profile.FirstContactInitialSilenceTimeoutMs),
                (TimeSpan?)Requested(profile => profile.RepeatInitialSilenceTimeoutMs),
            ],
            harness.SpeechClient.AnswerWindows);
    }

    [Fact]
    public async Task EinMerkerAusEinerAnderenSprache_ZaehltNichtAlsVertrautheit()
    {
        // Ein deutscher Merker gilt nicht für den Einstieg auf Spanisch.
        // Für die spanische Frage gilt deshalb das Erstkontaktfenster.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: ["español"], entryOfferHeardIsoCode: "de");

        await harness.Turn.DecideLanguageAsync("es", Deadline());

        Assert.Equal([(TimeSpan?)Requested(profile => profile.FirstContactInitialSilenceTimeoutMs)],
            harness.SpeechClient.AnswerWindows);
    }

    [Fact]
    public async Task EinUnlesbarerSpeicher_FragtTrotzdem_UndGibtDieLaengereZeit()
    {
        // Ist der Speicher unlesbar, bleibt die längere Bedenkzeit erhalten.
        // Ein fälschlich angenommenes Wiederholungsfenster könnte eine erste Antwort abschneiden.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: ["Deutsch"], storeIsUnreadable: true);

        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), await harness.Turn.DecideLanguageAsync("de", Deadline()));
        Assert.Equal([(TimeSpan?)Requested(profile => profile.FirstContactInitialSilenceTimeoutMs)],
            harness.SpeechClient.AnswerWindows);
    }

    [Fact]
    public async Task EinAngehaltenerEinstieg_HaeltNichtsFest()
    {
        // Das Mikrofon abzuschalten ist eine Anweisung, keine Antwort. Den Einstieg als gehört zu
        // verbuchen würde den einen Erstkontakt für eine Sitzung ausgeben, die der Teilnehmer
        // gestoppt hat.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(enabled: true, entryOfferHeardIsoCode: null);
        harness.SpeechClient.ArmVoiceInputPauseOnNextCancellation();

        using var cancellation = new CancellationTokenSource();
        var decision = harness.Turn.DecideLanguageAsync("de", cancellation.Token).AsTask();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<VoiceInputPausedException>(() => decision.WaitAsync(TimeSpan.FromSeconds(30)));
        Assert.Empty(harness.MarkedAsHeard);
    }

    [Fact]
    public async Task EinAbgebrochenerEinstieg_HaeltNichtsFest()
    {
        // Ein abgebrochener Start wird wiederholt, und die Wiederholung muss die Entscheidung erneut
        // stellen können.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(enabled: true, entryOfferHeardIsoCode: null);

        using var cancellation = new CancellationTokenSource();
        var decision = harness.Turn.DecideLanguageAsync("de", cancellation.Token).AsTask();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => decision.WaitAsync(TimeSpan.FromSeconds(30)));
        Assert.Empty(harness.MarkedAsHeard);
    }

    [Fact]
    public async Task Abgeschaltet_HaeltNichtsFest_UndOeffnetKeinMikrofon()
    {
        // Ein Marker bei abgeschaltetem Einstieg würde nach dessen Einschalten das erste Antwortfenster unberechtigt verkürzen.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(enabled: false, entryOfferHeardIsoCode: null);

        await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Empty(harness.MarkedAsHeard);
        Assert.Empty(harness.Spoken);

        // Fehlende Sprachausgabe allein schließt eine Aufnahme nicht aus.
        // Deshalb werden auch Aufnahmeaufrufe und Mikrofonereignisse geprüft.
        Assert.Equal(0, harness.SpeechClient.RecordCallCount);
        Assert.Empty(harness.MicrophoneEvents);
    }

    /// <summary>
    /// Angeforderte Aufnahmedauer aus gewählter Antwortzeit und modellierter Zusatzzeit.
    /// </summary>
    private static TimeSpan Requested(
        Func<Sprechen.Mda.Contracts.V1.SpeechCapture.SpeechCaptureSessionEntryProfile, int> select)
    {
        var profile = SpeechCaptureProfileCatalogReader.GetSessionEntryProfile();
        Assert.NotNull(profile);
        return TimeSpan.FromMilliseconds(select(profile) + profile.PlaybackSettleAllowanceMs);
    }

    private static CancellationToken Deadline()
        => new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;
}
