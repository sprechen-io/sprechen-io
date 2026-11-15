namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft französische, spanische und italienische Zahlwörter beim Erfassen von Ziffern.
/// <see cref="RemovedHomophones_DoNotDigitize"/> prüft außerdem, dass die angegebenen
/// französischen Wörter nicht als Ziffern interpretiert werden.
/// </summary>
public sealed class FormDigitCaptureLanguageFallbackTests
{
    private readonly IFormTranscriptParser _parser =
        new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver());


    [Theory]
    [InlineData("fr", "zero un deux trois quatre cinq six sept huit neuf", "0123456789")]
    [InlineData("es", "cero uno dos tres cuatro cinco seis siete ocho nueve", "0123456789")]
    [InlineData("it", "zero uno due tre quattro cinque sei sette otto nove", "0123456789")]
    public void DigitWords_CoverZeroToNine_PerLanguage(string isoCode, string transcript, string expected)
    {
        var outcome = _parser.ParseDigits(isoCode, transcript);

        Assert.True(outcome.Success);
        Assert.Equal(expected, outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("fr", "une", "1")]
    [InlineData("es", "un", "1")]
    [InlineData("it", "un", "1")]
    [InlineData("it", "uno", "1")]
    public void DigitWords_AcceptCanonicalOneVariants(string isoCode, string transcript, string expected)
    {
        var outcome = _parser.ParseDigits(isoCode, transcript);

        Assert.True(outcome.Success);
        Assert.Equal(expected, outcome.CanonicalValue);
    }

    // Diese französischen Wörter dürfen nicht als Zahlwörter angenommen werden.
    // Der Parsertest prüft ihre Ablehnung, keine erneute Messung von Erkennungsfehlern.
    [Theory]
    [InlineData("mythe")]
    [InlineData("sac")]
    [InlineData("sacs")]
    [InlineData("cette")]
    public void RemovedHomophones_DoNotDigitize(string word)
    {
        var outcome = _parser.ParseDigits("fr", word);

        Assert.False(outcome.Success);
    }

    // Das italienische zusammengesetzte Zahlwort liefert allein keine Ziffernfolge.
    // Die Ablehnung wird als NotRecognized gemeldet; der Dialog zur erneuten Eingabe ist hier nicht enthalten.
    [Fact]
    public void Italian_AgglutinatedNumberWord_IsRejectedForReAsk()
    {
        var outcome = _parser.ParseDigits("it", "novecentottanta");

        Assert.False(outcome.Success);
        Assert.NotNull(outcome.Failure);
        Assert.Equal(FormTranscriptParseFailureCategory.NotRecognized, outcome.Failure!.Category);
    }

    [Fact]
    public void Italian_IsolatedUno_MapsToOne()
    {
        var outcome = _parser.ParseDigits("it", "uno");

        Assert.True(outcome.Success);
        Assert.Equal("1", outcome.CanonicalValue);
    }

    // Zusätzliche nicht als Ziffer erkannte Wörter lassen die enthaltenen Ziffern bestehen.
    // Die Beispiele prüfen den Parser, nicht die Häufigkeit solcher Erkennungsergebnisse.
    [Theory]
    [InlineData("fr", "oui 8", "8")]
    [InlineData("fr", "le 3", "3")]
    [InlineData("fr", "0 litre", "0")]
    [InlineData("fr", "le deux et trois", "23")]
    [InlineData("es", "el 5", "5")]
    [InlineData("it", "ecco 7", "7")]
    public void FillerAroundDigits_IsTolerated_DigitsKept(string isoCode, string transcript, string expected)
    {
        var outcome = _parser.ParseDigits(isoCode, transcript);

        Assert.True(outcome.Success);
        Assert.Equal(expected, outcome.CanonicalValue);
    }

    // Ohne erkannte Ziffer entsteht kein Wert, auch wenn unbekannte Wörter übersprungen werden.
    [Theory]
    [InlineData("fr", "oui")]
    [InlineData("fr", "le litre")]
    [InlineData("es", "vale pues")]
    [InlineData("it", "novecentottanta")]
    public void PureFiller_WithoutAnyDigit_IsNotRecognized(string isoCode, string transcript)
    {
        var outcome = _parser.ParseDigits(isoCode, transcript);

        Assert.False(outcome.Success);
        Assert.NotNull(outcome.Failure);
        Assert.Equal(FormTranscriptParseFailureCategory.NotRecognized, outcome.Failure!.Category);
    }


    [Theory]
    [InlineData("de", "eins zwei drei", "123")]
    [InlineData("de", "zwo acht", "28")]
    [InlineData("en", "one two three", "123")]
    [InlineData("en", "nine eight seven", "987")]
    public void DeEn_DigitCapture_IsUnchanged(string isoCode, string transcript, string expected)
    {
        var outcome = _parser.ParseDigits(isoCode, transcript);

        Assert.True(outcome.Success);
        Assert.Equal(expected, outcome.CanonicalValue);
    }
}
