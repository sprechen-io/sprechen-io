namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Ziffernwörter und Großschreibmarker beim Buchstabieren
/// für die fünf aufgeführten Sprachen.
/// </summary>
public sealed class SpellingLanguageCoverageTests
{
    private readonly IFormTranscriptParser _parser =
        new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver());

    [Theory]
    [InlineData("de", "null eins zwei drei vier fünf sechs sieben acht neun", "0123456789")]
    [InlineData("en", "zero one two three four five six seven eight nine", "0123456789")]
    [InlineData("fr", "zero un deux trois quatre cinq six sept huit neuf", "0123456789")]
    [InlineData("es", "cero uno dos tres cuatro cinco seis siete ocho nueve", "0123456789")]
    [InlineData("it", "zero uno due tre quattro cinque sei sette otto nove", "0123456789")]
    public void SpokenDigits_ResolveInEverySupportedLanguage(string isoCode, string spoken, string expected)
    {
        var outcome = _parser.ParseSpelling(isoCode, spoken);

        Assert.True(outcome.Success, $"'{spoken}' did not parse in {isoCode}.");
        Assert.Equal(expected, outcome.CanonicalValue);
    }

    [Theory]
    // In diesen deutschen und englischen Beispielen steht der Marker vor dem Buchstaben.
    [InlineData("de", "großes p null null sieben", "P007")]
    [InlineData("en", "capital p zero zero seven", "P007")]
    // Die französischen, spanischen und italienischen Beispiele prüfen den nachgestellten Marker.
    [InlineData("fr", "p majuscule zero zero sept", "P007")]
    [InlineData("es", "p mayúscula cero cero siete", "P007")]
    [InlineData("it", "p maiuscola zero zero sette", "P007")]
    public void UpperCaseMarker_BindsToItsLetterInEitherOrder(string isoCode, string spoken, string expected)
    {
        var outcome = _parser.ParseSpelling(isoCode, spoken);

        Assert.True(outcome.Success, $"'{spoken}' did not parse in {isoCode}.");
        Assert.Equal(expected, outcome.CanonicalValue);
    }

    [Theory]
    // Auch Akzentschreibweisen müssen zur gleichen Zeichenfolge ausgewertet werden.
    [InlineData("fr", "p majuscule zéro zéro sept", "P007")]
    [InlineData("es", "p mayúscula cero cero siete", "P007")]
    [InlineData("it", "p maiuscola zero zero sette", "P007")]
    public void SpokenDigits_WithTheirDiacritics_StillResolve(string isoCode, string spoken, string expected)
    {
        var outcome = _parser.ParseSpelling(isoCode, spoken);

        Assert.True(outcome.Success, $"'{spoken}' did not parse in {isoCode}.");
        Assert.Equal(expected, outcome.CanonicalValue);
    }

    [Fact]
    public void SpokenDigits_OfAnotherLanguage_DoNotLeakIntoThisOne()
    {
        // Das italienische Ziffernwort sei darf im deutschen Kontext nicht zur Ziffer 6 werden.
        var outcome = _parser.ParseSpelling("de", "sei");

        Assert.True(outcome.Success);
        Assert.NotEqual("6", outcome.CanonicalValue);
    }
}
