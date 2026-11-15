using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Lesen, Schreiben und Fehlerbehandlung des Erstkontakt-Merkers
/// sowie die Delegation dieser Aufgaben durch den Einstieg.
/// </summary>
public sealed class SessionEntryFirstContactTests
{
    [Fact]
    public async Task EinLesbarerMerkerDerselbenSprache_IstKeinErstkontakt()
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(enabled: true, transcripts: [], entryOfferHeardIsoCode: "de");
        var firstContact = new SessionEntryFirstContact(harness.Dependencies);

        Assert.False(await firstContact.IsFirstContactAsync("de", CancellationToken.None));
        Assert.True(await firstContact.IsFirstContactAsync("es", CancellationToken.None));
    }

    [Fact]
    public async Task EinUnlesbarerSpeicher_HeisstErstkontakt()
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true,
            transcripts: [],
            entryOfferHeardIsoCode: "de",
            storeIsUnreadable: true);
        var firstContact = new SessionEntryFirstContact(harness.Dependencies);

        Assert.True(await firstContact.IsFirstContactAsync("de", CancellationToken.None));
        Assert.Contains(harness.Logged.Entries, entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task EinFehlgeschlagenesSchreiben_FaelltDenStartNicht()
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true,
            transcripts: [],
            entryOfferHeardIsoCode: null,
            savingTheMarkerFails: true);
        var firstContact = new SessionEntryFirstContact(harness.Dependencies);

        await firstContact.RememberOfferHeardAsync("de", CancellationToken.None);

        Assert.Equal(["de"], harness.MarkedAsHeard);
        Assert.Contains(harness.Logged.Entries, entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task EinGelungenesSchreiben_MachtDieSpracheVertraut()
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(enabled: true, transcripts: [], entryOfferHeardIsoCode: null);
        var firstContact = new SessionEntryFirstContact(harness.Dependencies);

        await firstContact.RememberOfferHeardAsync("de", CancellationToken.None);

        Assert.False(await firstContact.IsFirstContactAsync("de", CancellationToken.None));
    }
}
