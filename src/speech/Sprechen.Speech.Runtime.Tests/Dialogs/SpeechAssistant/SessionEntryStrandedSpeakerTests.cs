using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.Speech.LanguageContracts;
using Sprechen.Speech.ModelRuntime.PromptTemplates;
using Sprechen.Speech.ModelRuntime.Services;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Sprachwahl bei abweichender Sitzungssprache: Endonyme, sprachgebundene Zustimmung,
/// mehrdeutige Ja-Wörter, Verneinungen und Nichtverstehen. Die Transkripte sind skriptierte
/// Eingaben; diese Tests messen keine akustische Erkennungsqualität.
/// </summary>
public sealed class SessionEntryStrandedSpeakerTests
{
    [Fact]
    public async Task DerItalienerImDeutschenBrowser_ErreichtSeineSprache()
    {
        // Nach vier unbeantworteten Fragen nimmt Italiano die italienische Frage an.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: [null, null, null, null, "Italiano."]);

        Assert.Equal(SessionEntryOutcome.ContinueIn("it"), await harness.Turn.DecideLanguageAsync("de", Deadline()));
        Assert.Equal(["it"], harness.Persisted);
    }

    [Fact]
    public void JedeFrage_WirdInDerSpracheGestellt_UmDieSieGeht()
    {
        // Jede Frage trägt den Sprachcode ihrer Zielsprache und einen nichtleeren Text.
        foreach (var isoCode in VoiceInterfaceModelCatalog.SupportedLanguages)
        {
            var question = SessionLanguageOfferPromptTextResolver
                .GetLanguageQuestionSegment(isoCode, PolitenessLevel.Formal);

            Assert.Equal(isoCode, question.IsoCode);
            Assert.False(string.IsNullOrWhiteSpace(question.Text), $"'{isoCode}' hat keine Frage");
        }
    }

    [Fact]
    public async Task DieAntwortWirdInDerSpracheErkannt_InDerGefragtWurde()
    {
        // Die erste Erkennung jeder Stufe verwendet die Sprache der gestellten Frage.
        // Skriptierte Antworten auf jeder Stufe machen deren Erkennungssprache beobachtbar.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: ["nein", "no", "non", "no", "italiano"]);

        Assert.Equal(SessionEntryOutcome.ContinueIn("it"), await harness.Turn.DecideLanguageAsync("de", Deadline()));

        Assert.Equal(
            ["de", .. VoiceInterfaceModelCatalog.SupportedLanguages.Where(iso => iso != "de")],
            harness.SpeechClient.SpeechToTextCalls.Select(call => call.LanguageCode));

        // Der innere Client erhält eine erste Erkennung je Aufnahme; Zweiterkennungen liegen separat.
        Assert.Equal(harness.SpeechClient.RecordCallCount, harness.SpeechClient.SpeechToTextCallCount);
    }

    [Theory]
    // Italienische Formulierungen für Nichtverstehen.
    [InlineData("Non capisco.")]
    [InlineData("Non ho capito")]
    // Auch die verkürzte Variante wird als Nichtverstehen geprüft.
    [InlineData("No capisco")]
    // Entsprechende Formulierungen in den anderen vier unterstützten Sprachen.
    [InlineData("Ich verstehe nicht")]
    [InlineData("I don't understand")]
    [InlineData("Je ne comprends pas")]
    [InlineData("No entiendo")]
    // Kurze Rückfragen ergänzen die vollständigen Nichtverstehenssätze.
    [InlineData("Scusi?")]
    [InlineData("Pardon?")]
    [InlineData("Wie bitte?")]
    public void WerSagtDassErNichtVersteht_HatNichtZugestimmt(string transcript)
    {
        // Die ergänzende Ja-Probe prüft, ob Nichtverstehen Vorrang vor einer möglichen Zustimmung hat.
        Assert.False(SessionEntryAnswerReader.AcceptsTheSessionLanguage(transcript, "de"));
        Assert.False(SessionEntryAnswerReader.AcceptsTheOfferedLanguage(transcript, "de", "en"));
        Assert.True(
            SessionEntryNotUnderstoodProbe.IsMarker(transcript),
            $"'{transcript}' steht im Modell, wirkt aber nicht");
    }

    [Fact]
    public void DiePhonetischenTruemmerDesFalschenModells_SindNichtModelliert_UndDuerfenEsNichtSein()
    {
        // No Capisko wird nicht als eigener Nichtverstehensmarker ins Lexikon aufgenommen.
        Assert.False(SessionEntryNotUnderstoodProbe.IsMarker("No Capisko."));
    }

    [Theory]
    // Die gewöhnlichen Antworten behalten ihre Bedeutung.
    [InlineData("nein danke", YesNoClassification.No)]
    [InlineData("no grazie", YesNoClassification.No)]
    [InlineData("nein", YesNoClassification.No)]
    [InlineData("ja", YesNoClassification.Yes)]
    [InlineData("sì", YesNoClassification.Yes)]
    public void DieGewoehnlichenAntworten_BehaltenIhreBedeutung(string transcript, YesNoClassification expected)
    {
        var classification = new YesNoTranscriptClassifier()
            .Classify(VoiceInterfaceModelCatalog.SupportedLanguages, transcript);

        Assert.Equal(expected, classification);
    }

    [Theory]
    // Das angebotene Endonym darf auch in längeren Antworten die Sprachwahl tragen.
    [InlineData("Italiano, per favore.")]
    [InlineData("Studio italiano.")]
    [InlineData("Parla italiano.")]
    [InlineData("Vorrei l'italiano.")]
    public void DerNameInEinerAntwort_IstZustimmung(string transcript)
    {
        Assert.True(SessionEntryAnswerReader.AcceptsTheOfferedLanguage(transcript, "it", "de"));
    }

    [Theory]
    // Ein ausdrücklich verneintes Endonym darf weder Wechsel noch Behalten auslösen.
    [InlineData("kein italiano")]
    [InlineData("nicht italiano")]
    [InlineData("nie italiano")]
    [InlineData("no italiano")]
    [InlineData("non italiano")]
    public void EinAbgelehnterName_IstKeineZustimmung(string transcript)
    {
        Assert.False(SessionEntryAnswerReader.AcceptsTheOfferedLanguage(transcript, "it", "de"));
        Assert.False(SessionEntryAnswerReader.AcceptsTheSessionLanguage(transcript, "it"));
    }

    [Theory]
    // Das geteilte si ohne Akzent darf weder Spanisch noch Italienisch allein auswählen.
    [InlineData("si", "es", "de")]
    [InlineData("si", "it", "de")]
    [InlineData("si", "es", "it")]
    public void EinJaOhneAkzent_EntscheidetKeineDerBeidenSprachen(
        string transcript, string askedIsoCode, string sessionIsoCode)
    {
        Assert.False(
            SessionEntryAnswerReader.AcceptsTheOfferedLanguage(transcript, askedIsoCode, sessionIsoCode),
            $"'{transcript}' hat die Sprosse '{askedIsoCode}' entschieden");
    }

    [Theory]
    // Das geteilte Ja behält auch die Sitzungssprache nicht; der Akzent allein genügt nicht.
    [InlineData("sì", "it")]
    [InlineData("sí", "es")]
    public void DasGeteilteJa_BehaeltAuchDieSitzungsspracheNicht(string transcript, string sessionIsoCode)
    {
        Assert.False(
            SessionEntryAnswerReader.AcceptsTheSessionLanguage(transcript, sessionIsoCode),
            $"'{transcript}' hat die Sitzungssprache '{sessionIsoCode}' entschieden");
    }

    [Theory]
    // Eindeutige Ja-Wörter und das passende Endonym können die Sitzungssprache behalten.
    [InlineData("ja", "de")]
    [InlineData("yes", "en")]
    [InlineData("oui", "fr")]
    [InlineData("italiano", "it")]
    [InlineData("español", "es")]
    public void DasEindeutigeJaUndDerName_BehaltenDieSitzungssprache(string transcript, string sessionIsoCode)
    {
        Assert.True(
            SessionEntryAnswerReader.AcceptsTheSessionLanguage(transcript, sessionIsoCode),
            $"'{transcript}' hat die Sitzungssprache '{sessionIsoCode}' nicht behalten");
    }


    [Theory]
    // Okay und Ok dürfen auch zusammen mit einem geteilten Ja keine Sprachwahl auslösen.
    [InlineData("okay", "de", "de")]
    [InlineData("ok", "de", "de")]
    [InlineData("Okay, sì.", "de", "de")]
    [InlineData("Ok, sì.", "de", "de")]
    [InlineData("Sì, okay.", "de", "de")]
    [InlineData("okay", "en", "de")]
    [InlineData("ok", "it", "de")]
    public void DasWortDasErSagenSollte_IstKeineAntwort(
        string transcript, string askedIsoCode, string sessionIsoCode)
    {
        Assert.False(
            SessionEntryAnswerReader.AcceptsTheOfferedLanguage(transcript, askedIsoCode, sessionIsoCode),
            $"'{transcript}' hat die Sprosse '{askedIsoCode}' entschieden");

        if (askedIsoCode == sessionIsoCode)
        {
            Assert.False(
                SessionEntryAnswerReader.AcceptsTheSessionLanguage(transcript, askedIsoCode),
                $"'{transcript}' hat die Sitzungssprache '{askedIsoCode}' entschieden");
        }
    }

    [Theory]
    // Das eindeutige Ja der angebotenen Sprache nimmt ihr Angebot weiterhin an.
    [InlineData("ja", "de", "en")]
    [InlineData("yes", "en", "de")]
    [InlineData("oui", "fr", "de")]
    public void DasJaDerGefragtenSprache_EntscheidetIhreSprosse(
        string transcript, string askedIsoCode, string sessionIsoCode)
    {
        Assert.True(
            SessionEntryAnswerReader.AcceptsTheOfferedLanguage(transcript, askedIsoCode, sessionIsoCode));
        Assert.True(SessionEntryAnswerReader.AcceptsTheSessionLanguage(transcript, askedIsoCode));
    }

    [Theory]
    // Ein Ja aus einer anderen Sprache darf die aktuell angebotene Sprache nicht auswählen.
    [InlineData("oui", "en", "de")]
    [InlineData("ja", "en", "de")]
    [InlineData("ja", "it", "de")]
    [InlineData("yes", "fr", "de")]
    [InlineData("sì, grazie", "en", "de")]
    [InlineData("oui, merci", "es", "de")]
    public void EinFremdesJa_EntscheidetKeineSprosse(
        string transcript, string askedIsoCode, string sessionIsoCode)
    {
        Assert.False(
            SessionEntryAnswerReader.AcceptsTheOfferedLanguage(transcript, askedIsoCode, sessionIsoCode),
            $"'{transcript}' hat die Sprosse '{askedIsoCode}' entschieden");
    }

    [Theory]
    // Prüft fremdsprachige Ja-Wörter auch auf der ersten Stufe für die fünf aufgeführten Sprachen.
    [InlineData("de", "oui")]
    [InlineData("en", "oui")]
    [InlineData("fr", "ja")]
    [InlineData("es", "ja")]
    [InlineData("it", "oui")]
    public void EinFremdesJa_BehaeltDieSitzungssprucheNicht(string sessionIsoCode, string transcript)
    {
        Assert.False(SessionEntryAnswerReader.AcceptsTheSessionLanguage(transcript, sessionIsoCode));
    }

    [Theory]
    // Feste zustimmende Wendungen wie Ja, kein Problem dürfen nicht an der Verneinung scheitern.
    [InlineData("Ja, kein Problem")]
    [InlineData("Ja, keine Frage")]
    [InlineData("Ja, nicht schlecht")]
    public void EineZustimmungMitEinerFremdenVerneinungDarin_BleibtZustimmung(string transcript)
    {
        Assert.True(
            SessionEntryAnswerReader.AcceptsTheSessionLanguage(transcript, "de"),
            $"'{transcript}' wurde als Ablehnung gelesen");
    }

    [Theory]
    // Das Endonym der Sitzungssprache nimmt deren erste Frage an.
    [InlineData("de", "Deutsch")]
    [InlineData("en", "English")]
    [InlineData("fr", "français")]
    [InlineData("es", "español")]
    [InlineData("it", "italiano")]
    public void DerNameDerSitzungssprache_BehaeltSie(string sessionIsoCode, string transcript)
    {
        Assert.True(SessionEntryAnswerReader.AcceptsTheSessionLanguage(transcript, sessionIsoCode));
    }

    [Fact]
    public async Task ErMussNichtsSagenKoennen_AusserSeinemEigenenJa()
    {
        // Eine italienische Bitte auf der deutschen Stufe verhindert die spätere Annahme nicht.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: ["Vorrei l'italiano.", null, null, null, "italiano"]);

        Assert.Equal(SessionEntryOutcome.ContinueIn("it"), await harness.Turn.DecideLanguageAsync("de", Deadline()));
    }

    [Fact]
    public async Task EinItalienischesJaAufDieDeutscheFrage_BehaeltNichtDeutsch()
    {
        // Ein italienisches Ja nimmt die deutsche Frage nicht an; später kann Italiano Italienisch wählen.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: ["Sì.", null, null, null, "Italiano."]);

        Assert.Equal(SessionEntryOutcome.ContinueIn("it"), await harness.Turn.DecideLanguageAsync("de", Deadline()));
        Assert.Equal(["it"], harness.Persisted);
    }

    [Fact]
    public async Task EinDeutschesJaAufDieItalienischeFrage_WaehltNichtsMehr()
    {
        // Ein deutsches Ja nimmt die italienische Frage nicht an; das passende Endonym bleibt eine Alternative.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: [null, null, null, null, "ja"]);

        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), await harness.Turn.DecideLanguageAsync("de", Deadline()));
        Assert.Empty(harness.Persisted);
    }

    [Fact]
    public async Task DasGeteilteJaAufDerItalienischenFrage_WaehltNichts()
    {
        // Auch ein akzentuiertes italienisches Ja bleibt ohne Endonym mehrdeutig und wählt hier nichts.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: [null, null, null, null, "sì"]);

        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), await harness.Turn.DecideLanguageAsync("de", Deadline()));
        Assert.Empty(harness.Persisted);
    }

    [Fact]
    public async Task DasGeteilteJaAufDerSpanischenSprosse_TraegtDenItalienerNichtDavon()
    {
        // Sí darf Spanisch nicht auswählen; die folgende Antwort italiano wählt Italienisch.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: [null, null, null, "Sí", "italiano"]);

        Assert.Equal(SessionEntryOutcome.ContinueIn("it"), await harness.Turn.DecideLanguageAsync("de", Deadline()));
    }

    [Theory]
    // Endonym mit und ohne abschließende Interpunktion.
    [InlineData("Italiano.")]
    [InlineData("italiano")]
    // Endonym mit vorangestelltem Artikel.
    [InlineData("l'italiano")]
    public async Task DenNamenDerAngebotenenSpracheZuWiederholen_IstZustimmung(string transcript)
    {
        // Das passende, nicht verneinte Endonym gilt als Annahme der angebotenen Sprache.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: [null, null, null, null, transcript]);

        Assert.Equal(SessionEntryOutcome.ContinueIn("it"), await harness.Turn.DecideLanguageAsync("de", Deadline()));
    }

    [Fact]
    public async Task EinAbgelehnterNameImGanzenSatz_IstKeineZustimmung()
    {
        // Eine ausdrückliche Ablehnung des Endonyms verhindert die Annahme auch im ganzen Satz.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: [null, null, null, null, "non voglio italiano"]);

        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), await harness.Turn.DecideLanguageAsync("de", Deadline()));
        Assert.Empty(harness.Persisted);
    }

    [Fact]
    public async Task DerNameEinerAnderenSprache_IstKeineZustimmung()
    {
        // Wer auf die Frage nach Italienisch mit dem Namen Französisch antwortet, stimmt nicht zu.
        // Die Prüfung muss den angebotenen Namen von anderen Sprachnamen unterscheiden.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: [null, null, null, null, "Français"]);

        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), await harness.Turn.DecideLanguageAsync("de", Deadline()));
        Assert.Empty(harness.Persisted);
    }

    private static CancellationToken Deadline()
        => new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;
}
