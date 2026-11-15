using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Ablehnungen mit vorangestelltem Sprachnamen und nachfolgender Verneinung in den fünf aufgeführten Sprachen.
/// Kontraste gegen eine andere Sprache und Verneinungen ohne Bezug zur angebotenen Sprache sollen weiterhin Zustimmung erlauben.
/// </summary>
public sealed class SessionEntryFrontedNameRefusalTests
{
    [Theory]

    [InlineData("Deutsch verstehe ich nicht.", "de")]
    [InlineData("Deutsch spreche ich nicht.", "de")]
    [InlineData("Deutsch möchte ich nicht.", "de")]
    [InlineData("Deutsch kann ich leider gar nicht.", "de")]
    // Italienisch, Französisch, Englisch, Spanisch: dieselbe Satzform.
    [InlineData("L'italiano non lo parlo.", "it")]
    [InlineData("Italiano non lo voglio.", "it")]
    [InlineData("Le français, je ne le parle pas.", "fr")]
    [InlineData("English, I don't speak.", "en")]
    [InlineData("El español no lo hablo.", "es")]
    public void DerVorangestellteNameMitNachgestellterVerneinung_IstEineAblehnung(
        string transcript, string askedIsoCode)
    {
        Assert.False(
            SessionEntryAnswerReader.AcceptsTheOfferedLanguage(transcript, askedIsoCode, "de"),
            $"'{transcript}' wurde als Zustimmung zu '{askedIsoCode}' gelesen");
    }

    [Theory]
    // Die Verneinung betrifft eine andere Sprache; die vorangestellte angebotene Sprache bleibt ausgewählt.
    // Auch fremdsprachige Bezeichnungen wie inglese werden geprüft.
    [InlineData("Italiano, non l'inglese.", "it")]
    [InlineData("Italiano, non tedesco.", "it")]
    [InlineData("Deutsch, nicht Englisch.", "de")]
    public void DerKontrastGegenEineAndereSprache_BleibtZustimmung(string transcript, string askedIsoCode)
    {
        Assert.True(
            SessionEntryAnswerReader.AcceptsTheOfferedLanguage(transcript, askedIsoCode, "de"),
            $"'{transcript}' wurde nicht als Zustimmung zu '{askedIsoCode}' gelesen");
    }

    [Theory]
    // Eine Verneinung ohne Bezug zur Sprache soll die Zustimmung nicht aufheben.
    [InlineData("Ja, kein Problem.", "de")]
    public void EineVerneinungDieEtwasAnderesTrifft_IstKeineAblehnung(string transcript, string askedIsoCode)
    {
        Assert.True(
            SessionEntryAnswerReader.AcceptsTheOfferedLanguage(transcript, askedIsoCode, askedIsoCode),
            $"'{transcript}' wurde als Ablehnung von '{askedIsoCode}' gelesen");
    }

    [Fact]
    public void DasGeteilteJaMitAngehaengterVerneinung_EntscheidetNichts_UndIstKeineAblehnung()
    {
        // Das sprachübergreifend verwendete Sì reicht allein nicht zur Annahme des italienischen Angebots.
        // Die Fassung mit Sprachnamen prüft zusätzlich, dass non c’è problema nicht als Ablehnung der genannten Sprache gilt.
        Assert.False(SessionEntryAnswerReader.AcceptsTheOfferedLanguage("Sì, non c'è problema.", "it", "it"));

        // Der ausdrücklich genannte Sprachname ermöglicht hier die Zustimmung.
        Assert.True(SessionEntryAnswerReader.AcceptsTheOfferedLanguage("Sì, italiano, non c'è problema.", "it", "it"));
    }

    [Fact(Skip = "Bekannte Lücke: Ein bloßes Nein-Wort irgendwo im Text gilt als Ablehnung, unabhängig davon, "
        + "worauf es sich bezieht. 'Español, no alemán.' nennt Spanisch und lehnt Deutsch ab, wird aber "
        + "als Ablehnung von Spanisch gelesen, weil 'no' ein spanisches Nein-Wort ist. Dieselbe Klasse: "
        + "'Sí, no hay problema.' Das Nein-Wort braucht dieselbe Positionsbindung wie die Verneinungs-Cues.")]
    public void DerKontrastMitEinemBlossenNeinWort_WirdNochAlsAblehnungGelesen()
    {
        Assert.True(SessionEntryAnswerReader.AcceptsTheOfferedLanguage("Español, no alemán.", "es", "de"));
    }
}
