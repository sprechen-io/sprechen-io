namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Vergleicht die standardmäßigen Höflichkeitswörter in den Parseroptionen mit der sprachübergreifenden
/// Vereinigung des modellierten Lexikons. Die Prüfung verhindert unterschiedliche Wortmengen,
/// belegt aber nicht, wie jeder einzelne Verbraucher die Optionen verwendet.
/// </summary>
public sealed class FormCommandPolitePaddingLexiconAlignmentTests
{
    [Fact]
    public void ParserOptionsDefaultPoliteModifierTokens_SetEquals_LexiconUnion()
    {
        var index = new FormTranscriptLexiconIndex(EmbeddedFormTranscriptLexiconProvider.Instance.GetLexicon());
        var lexiconUnion = index.GetCommandPolitePaddingTokens();

        var optionsTokens = new FormGlobalCommandParserOptions().PoliteModifierTokens;

        Assert.Equal(
            lexiconUnion.OrderBy(token => token, StringComparer.Ordinal),
            optionsTokens.OrderBy(token => token, StringComparer.Ordinal));
    }

    [Fact]
    public void ParserOptionsDefaultPoliteModifierTokens_IsTheLanguageAgnosticUnion()
    {
        var optionsTokens = new FormGlobalCommandParserOptions().PoliteModifierTokens;

        // Die gemeinsame Liste enthält deutsche und englische Höflichkeitswörter; sie ist nicht nach Sitzungssprache getrennt.
        Assert.Equal(8, optionsTokens.Count);
        Assert.Contains("bitte", optionsTokens);
        Assert.Contains("kurz", optionsTokens);
        Assert.Contains("please", optionsTokens);
        Assert.Contains("kindly", optionsTokens);
    }
}
