using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.ModelRuntime.PromptTemplates;
using Sprechen.Speech.ModelRuntime.Services;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Fehlerbehandlung beim Einstieg sowie die angeforderten Abschluss- und Wechselansagen.
/// </summary>
public sealed class SessionEntryWhenSomethingBreaksTests
{
    [Theory]
    [InlineData(MicrophoneAcquisitionFailureCode.PermissionDenied)]
    [InlineData(MicrophoneAcquisitionFailureCode.DeviceNotFound)]
    public async Task DauerhafterMikrofonfehler_ErreichtDieOberflaecheUndDerNaechsteStartFragtErneut(
        MicrophoneAcquisitionFailureCode failureCode)
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true,
            captureSessionFailure: failureCode);

        await Assert.ThrowsAsync<MicrophoneAcquisitionException>(() =>
            harness.Turn.DecideLanguageAsync("de", Deadline()).AsTask());
        await Assert.ThrowsAsync<MicrophoneAcquisitionException>(() =>
            harness.Turn.DecideLanguageAsync("de", Deadline()).AsTask());

        Assert.Empty(harness.Spoken);
        Assert.Empty(harness.MicrophoneEvents);
        Assert.Equal(0, harness.SpeechClient.RecordCallCount);
    }

    [Fact]
    public async Task EineAngenommeneSpracheDieNichtGespeichertWerdenKann_WirdAngesagt()
    {
        // Scheitert das Speichern, nennt die Rückmeldung die beibehaltene Sprache in der angefragten Sprache.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true,
            transcripts: [null, null, null, null, "italiano"],
            savingTheLanguageFails: true);

        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), await harness.Turn.DecideLanguageAsync("de", Deadline()));

        Assert.Contains(
            harness.Spoken,
            segment => segment.IsoCode == "it"
                && segment.Text == FormDialogFlowPromptTextResolver.GetKeptPrompt("it", Endonym("de")));
    }

    [Fact]
    public async Task EineAngenommeneSpracheDieDerSpeicherNichtUebernimmt_WirdAngesagt()
    {
        // Dieselbe Lage, anderer Weg: Der Speicher wirft nicht, gibt aber etwas Unbrauchbares
        // zurück. Der Speicher bestimmt die tatsächlich geschriebene Sprache.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true,
            transcripts: [null, null, null, null, "italiano"],
            persistedIsoCodeOverride: " ");

        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), await harness.Turn.DecideLanguageAsync("de", Deadline()));

        Assert.Contains(
            harness.Spoken,
            segment => segment.IsoCode == "it"
                && segment.Text == FormDialogFlowPromptTextResolver.GetKeptPrompt("it", Endonym("de")));
    }

    [Fact]
    public async Task EinFehlschlagBeimMerker_LaesstDieSitzungTrotzdemStarten()
    {
        // Ein fehlgeschlagener Marker verkürzt keine aktuelle Sitzung; er führt beim nächsten Einstieg zum längeren Fenster und darf den Start nicht verhindern.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true,
            transcripts: ["Deutsch"],
            savingTheMarkerFails: true);

        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), await harness.Turn.DecideLanguageAsync("de", Deadline()));
    }

    [Fact]
    public async Task EineUnbeantworteteLeiter_SagtWoDieSitzungGelandetIst()
    {
        // Nach einer unbeantworteten Leiter benennt die Abschlussansage die beibehaltene Sprache.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: SessionEntryLanguageOfferTurnHarness.SilenceOnEveryRung());

        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), await harness.Turn.DecideLanguageAsync("de", Deadline()));

        Assert.Equal(
            FormDialogFlowPromptTextResolver.GetKeptPrompt("de", Endonym("de")),
            harness.Spoken[^1].Text);
    }

    [Fact]
    public async Task EineBeantworteteLeiter_SagtNichtsWeiter()
    {
        // Eine angenommene erste Frage braucht keine zusätzliche Ansage zur unveränderten Sprache.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(enabled: true, transcripts: ["Deutsch"]);

        await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Single(harness.Spoken);
    }

    [Fact]
    public async Task EineUnbeantworteteLeiter_HaeltNichtsFest()
    {
        // Ohne Annahme bleibt der Merker leer, damit die nächste Sitzung das Erstkontaktfenster erhält.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: SessionEntryLanguageOfferTurnHarness.SilenceOnEveryRung());

        await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Empty(harness.MarkedAsHeard);
    }

    [Fact]
    public async Task EineBeantworteteLeiter_HaeltFest()
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(enabled: true, transcripts: ["Deutsch"]);

        await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Equal(["de"], harness.MarkedAsHeard);
    }

    [Fact]
    public async Task EineSprosseDieWirft_WirdAufIhrerSprosseAufgefangen()
    {
        // Auch nach einem Erkennungsfehler auf der letzten Stufe wird die beibehaltene Sprache angesagt.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true,
            transcripts: SessionEntryLanguageOfferTurnHarness.SilenceOnEveryRung(),
            recogniserFailsOnRung: VoiceInterfaceModelCatalog.SupportedLanguages.Count);

        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), await harness.Turn.DecideLanguageAsync("de", Deadline()));

        Assert.Equal(
            FormDialogFlowPromptTextResolver.GetKeptPrompt("de", Endonym("de")),
            harness.Spoken[^1].Text);
    }

    [Fact]
    public async Task EineAngenommeneFremdeSprache_WirdInIhrSelbstAngesagt()
    {
        // Nach einem angenommenen Wechsel benennt ein Satz in der neuen Sprache das Ergebnis.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: [null, null, null, null, "italiano"]);

        Assert.Equal(SessionEntryOutcome.ContinueIn("it"), await harness.Turn.DecideLanguageAsync("de", Deadline()));

        var spoken = harness.Spoken[^1];
        Assert.Equal("it", spoken.IsoCode);
        Assert.Equal(
            SessionLanguageOfferPromptTextResolver.GetAcceptedSegment("it", PolitenessLevel.Formal).Text,
            spoken.Text);
    }

    [Fact]
    public async Task EineBehalteneSprache_BekommtDieseAnsageNicht()
    {
        // Die Annahme der bisherigen Sprache fügt keinen entsprechenden Wechselsatz hinzu.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(enabled: true, transcripts: ["Deutsch"]);

        await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Single(harness.Spoken);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void JedeUnterstuetzteSprache_HatIhreBeidenFragen(string isoCode)
    {
        // Für jede aufgeführte Sprache müssen Behalten- und Wechsel-Frage einen Text im richtigen Sprachcode liefern.
        foreach (var segment in new[]
                 {
                     SessionLanguageOfferPromptTextResolver.GetKeepQuestionSegment(isoCode, PolitenessLevel.Formal),
                     SessionLanguageOfferPromptTextResolver.GetLanguageQuestionSegment(isoCode, PolitenessLevel.Formal),
                 })
        {
            Assert.Equal(isoCode, segment.IsoCode);
            Assert.False(string.IsNullOrWhiteSpace(segment.Text));
        }
    }

    private static string Endonym(string isoCode)
        => VoiceInterfaceLanguageCatalog.CreateDefault().GetEndonym(LanguageCode.ParseIso6391(isoCode));

    private static CancellationToken Deadline()
        => new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;
}
