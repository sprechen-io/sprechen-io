using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class DigitsWorkflowCommandParserTests
{
    [Fact]
    public void TryParse_WhenGermanDeleteLastDigitUtterance_ReturnsDeleteLastDigitCommand()
    {
        var parsed = DigitsWorkflowCommandParser.TryParse(
            "letzte Ziffer löschen",
            "de",
            PolitenessLevel.Formal,
            out var commandId);

        Assert.True(parsed);
        Assert.Equal(DigitsWorkflowCommandIds.DeleteLastDigit, commandId);
    }

    [Theory]
    [InlineData("letzte Ziffer löschen")]   // Schreibweise mit Umlaut.
    [InlineData("letzte ziffer loeschen")]  // ASCII-Umlautumschrift.
    [InlineData("letzte ziffer loschen")]   // Schreibweise nach Entfernen des Umlautzeichens.
    public void TryParse_AcceptsEitherGermanAsciiConvention(string transcript)
    {
        // Die normale Zerlegung macht aus „löschen“ bereits „loschen“, nicht aber aus „loeschen“.
        // Der zusätzliche Abgleich beider Seiten muss alle drei Schreibweisen demselben Befehl zuordnen.
        var parsed = DigitsWorkflowCommandParser.TryParse(
            transcript,
            "de",
            PolitenessLevel.Formal,
            out var commandId);

        Assert.True(parsed);
        Assert.Equal(DigitsWorkflowCommandIds.DeleteLastDigit, commandId);
    }

    [Fact]
    public void TryParse_WhenEnglishDeleteLastBlockUtterance_ReturnsDeleteLastBlockCommand()
    {
        var parsed = DigitsWorkflowCommandParser.TryParse(
            "delete last block",
            "en",
            PolitenessLevel.Formal,
            out var commandId);

        Assert.True(parsed);
        Assert.Equal(DigitsWorkflowCommandIds.DeleteLastBlock, commandId);
    }

    [Fact]
    public void TryParse_WhenUtteranceHasPolitePadding_ReturnsRepeatLastBlockCommand()
    {
        var parsed = DigitsWorkflowCommandParser.TryParse(
            "bitte block wiederholen bitte",
            "de",
            PolitenessLevel.Formal,
            out var commandId);

        Assert.True(parsed);
        Assert.Equal(DigitsWorkflowCommandIds.RepeatLastBlock, commandId);
    }

    [Fact]
    public void TryParse_WhenGermanRedictateBlockUtterance_ReturnsRedictateLastBlockCommand()
    {
        var parsed = DigitsWorkflowCommandParser.TryParse(
            "block neu diktieren",
            "de",
            PolitenessLevel.Formal,
            out var commandId);

        Assert.True(parsed);
        Assert.Equal(DigitsWorkflowCommandIds.RedictateLastBlock, commandId);
    }

    [Fact]
    public void TryParse_WhenTranscriptContainsAdditionalDomainText_ReturnsFalse()
    {
        var parsed = DigitsWorkflowCommandParser.TryParse(
            "meine eingabe ist delete last block now",
            "en",
            PolitenessLevel.Formal,
            out var commandId);

        Assert.False(parsed);
        Assert.Equal(string.Empty, commandId);
    }
}

