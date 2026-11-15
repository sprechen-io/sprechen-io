using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft akzentunabhängige Kollisionen von Bestätigungswörtern, explizite Sprachnamen
/// und den Geltungsbereich von Verneinungen beim Sitzungseinstieg. Ein vom Erkenner
/// gelieferter Akzent dient nicht als Beleg für die Sprache der sprechenden Person.
/// Mehrdeutige Bestätigungswörter reichen daher ohne eindeutigen Sprachnamen nicht aus.
/// </summary>
public sealed class SessionEntryAccentAndNegationTests
{
    [Theory]
    // Die normalisierten Bestätigungen für Italienisch und Spanisch kollidieren.
    // Auch unterschiedliche Akzente machen daraus keine eindeutige Sprachwahl.
    [InlineData("Sì.", "it")]
    [InlineData("Sì.", "es")]
    [InlineData("Sì, grazie.", "en")]
    [InlineData("Sí.", "es")]
    [InlineData("Sí, por favor.", "es")]
    [InlineData("Sí.", "it")]
    public void DasGeteilteJa_EntscheidetAufKeinerSprosse(string transcript, string askedIsoCode)
    {
        Assert.False(SessionEntryAnswerReader.AcceptsTheOfferedLanguage(transcript, askedIsoCode, "de"));
    }

    [Theory]
    // Ein ausdrücklicher Sprachname in derselben Antwort kann die Wahl dagegen eindeutig machen.
    [InlineData("Italiano.", "it")]
    [InlineData("Sì, italiano.", "it")]
    [InlineData("Español.", "es")]
    [InlineData("Sí, español.", "es")]
    public void DerName_TraegtWasDasGeteilteJaNichtMehrTraegt(string transcript, string askedIsoCode)
    {
        Assert.True(SessionEntryAnswerReader.AcceptsTheOfferedLanguage(transcript, askedIsoCode, "de"));
    }

    [Theory]
    // Englisch und Französisch schreiben das Wort identisch; es ist in keiner der beiden
    // Sprachen eine eindeutige Zustimmung.
    [InlineData("Correct.", "en")]
    [InlineData("Correct.", "fr")]
    public void EinWortDasZweiSprachenGleichSchreiben_EntscheidetKeine(string transcript, string askedIsoCode)
    {
        Assert.False(SessionEntryAnswerReader.AcceptsTheOfferedLanguage(transcript, askedIsoCode, "de"));
    }

    [Theory]
    // Beiden bleibt ihr eindeutiges Ja.
    [InlineData("Yes.", "en")]
    [InlineData("Oui.", "fr")]
    public void DasEindeutigeJa_BleibtJederSpracheErhalten(string transcript, string askedIsoCode)
    {
        Assert.True(SessionEntryAnswerReader.AcceptsTheOfferedLanguage(transcript, askedIsoCode, "de"));
    }

    [Theory]
    // Ein ausdrücklich anderer Sprachname darf nicht als Zustimmung zur angebotenen
    // oder bisherigen Sitzungssprache gelten, auch wenn ein geteiltes Ja davorsteht.
    [InlineData("Sì, italiano.", "es")]
    [InlineData("Sí, italiano.", "es")]
    public void WerEineAndereSpracheNennt_HatDieseNichtBejaht(string transcript, string sessionIsoCode)
    {
        Assert.False(SessionEntryAnswerReader.AcceptsTheSessionLanguage(transcript, sessionIsoCode));
        Assert.False(
            SessionEntryAnswerReader.AcceptsTheOfferedLanguage(transcript, sessionIsoCode, sessionIsoCode));
    }

    [Theory]
    // Eine verneinte Nennung des Sprachnamens darf keine Zustimmung auslösen.
    // Die Fälle decken Verneinungsformen aus allen fünf unterstützten Sprachen ab.
    [InlineData("Not English.", "en")]
    [InlineData("I don't speak English.", "en")]
    [InlineData("Pas le français.", "fr")]
    [InlineData("Je ne parle pas français.", "fr")]
    [InlineData("No hablo español.", "es")]
    [InlineData("Non parlo italiano.", "it")]
    [InlineData("kein italiano", "it")]
    [InlineData("non voglio italiano", "it")]
    public void EinAbgelehnterName_IstInJederSpracheEineAblehnung(string transcript, string askedIsoCode)
    {
        Assert.False(SessionEntryAnswerReader.AcceptsTheOfferedLanguage(transcript, askedIsoCode, "de"));
    }

    [Theory]
    // Bei „nicht X, sondern Y“ bezieht sich die Verneinung auf X.
    // Der anschließend bejahte Sprachname Y muss als Wechselziel erhalten bleiben.
    [InlineData("Non voglio il tedesco, voglio l'italiano.", "it")]
    [InlineData("I don't want German, I want English.", "en")]
    public void EineKontrastiveAntwort_WaehltDieGenannteSprache(string transcript, string askedIsoCode)
    {
        Assert.True(SessionEntryAnswerReader.AcceptsTheOfferedLanguage(transcript, askedIsoCode, "de"));
    }

    [Fact]
    public void EineKontrastiveAntwort_BehaeltAuchDieSitzungssprache()
    {
        Assert.True(
            SessionEntryAnswerReader.AcceptsTheSessionLanguage("Ich will kein Englisch, ich will Deutsch.", "de"));
    }

    [Theory]
    // Ein Höflichkeitsmarker macht aus einer vollständigen Antwort keine Nichtverstehens-Meldung.
    // „Scusi“ und „Pardon“ dürfen die anschließende Zustimmung mit Sprachnamen nicht verwerfen.
    [InlineData("Scusi, sì, italiano.", "it")]
    [InlineData("Pardon, oui, français.", "fr")]
    public void EinHoeflichkeitsmarkerVorEinerAntwort_VerwirftSieNicht(string transcript, string askedIsoCode)
    {
        Assert.True(SessionEntryAnswerReader.AcceptsTheOfferedLanguage(transcript, askedIsoCode, "de"));
    }

    [Fact]
    public void EinReinesNichtverstehen_BleibtEinNichtverstehen()
    {
        // Ohne Zustimmung und ohne Sprachwahl bleibt „Non capisco“ eine Nichtverstehensmeldung.
        Assert.False(SessionEntryAnswerReader.AcceptsTheOfferedLanguage("Non capisco", "it", "de"));
        Assert.True(SessionEntryAnswerReader.IsNonComprehension("Non capisco"));
    }

    [Theory]
    // Die Reihenfolge der Sprachangebote wird unabhängig vom ausführenden Ablauf geprüft.
    [InlineData("de", "de,en,fr,es,it")]
    [InlineData("en", "en,de,fr,es,it")]
    [InlineData("fr", "fr,de,en,es,it")]
    [InlineData("es", "es,de,en,fr,it")]
    [InlineData("it", "it,de,en,fr,es")]
    public void DieLeiterBeginntBeiDerSitzungssprache_UndFolgtSonstDemKatalog(string sessionIsoCode, string expected)
    {
        Assert.Equal(expected.Split(','), SessionEntryLadder.Order(sessionIsoCode));
        Assert.Equal(expected.Split(',')[1..], SessionEntryLadder.Others(sessionIsoCode));
    }

    [Fact]
    public void DieLeiterListetJedeSpracheGenauEinmal()
    {
        foreach (var sessionIsoCode in VoiceInterfaceModelCatalog.SupportedLanguages)
        {
            var order = SessionEntryLadder.Order(sessionIsoCode);

            Assert.Equal(VoiceInterfaceModelCatalog.SupportedLanguages.Count, order.Count);
            Assert.Equal(order.Count, order.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.Equal(
                VoiceInterfaceModelCatalog.SupportedLanguages.Order(StringComparer.Ordinal),
                order.Order(StringComparer.Ordinal));
        }
    }
}
