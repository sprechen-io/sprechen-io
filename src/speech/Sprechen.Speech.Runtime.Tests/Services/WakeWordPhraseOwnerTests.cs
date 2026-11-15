
namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class WakeWordPhraseOwnerTests
{
    private static readonly WakeWordInventory VariantInventory = new(
        ["Okay"],
        ["Ok", "OK"]);
    private static readonly WakeWordInventory AlexInventory = new(
        ["Okay Alex", "Hey Alex"],
        ["Ok Alex"]);
    private static readonly WakeWordActivationFormCodec ActivationForms = new();
    private static readonly WakeWordCommandTextExtractor CommandTextExtractor = new();

    public static IEnumerable<object[]> ModeledActivationOnlySequenceCases()
    {
        var isoCodes = new[] { "de", "en", "fr", "es", "it" };
        var sequenceKinds = new[] { "single", "canonical-pair", "recognition-pair" };

        foreach (var isoCode in isoCodes)
        {
            foreach (var sequenceKind in sequenceKinds)
            {
                yield return [isoCode, sequenceKind];
            }
        }
    }

    [Fact]
    public void ContainsWakeWord_WhenTranscriptContainsModeledActivationForm_ReturnsTrue()
    {
        var result = CommandTextExtractor.ContainsWakeWord("Okay", ["Okay"]);

        Assert.True(result);
    }

    [Fact]
    public void ContainsWakeWord_WhenTranscriptUsesDifferentCasingAndPunctuation_ReturnsTrue()
    {
        var result = CommandTextExtractor.ContainsWakeWord("OKAY!", ["okay"]);

        Assert.True(result);
    }

    [Fact]
    public void ContainsWakeWord_WhenTranscriptDoesNotContainActivationForm_ReturnsFalse()
    {
        var result = CommandTextExtractor.ContainsWakeWord("Luma", ["Okay"]);

        Assert.False(result);
    }

    [Fact]
    public void NormalizeWakeWordActivationForms_DeduplicatesAndTrims()
    {
        var activationForms = ActivationForms.Normalize(["Okay", " okay ", "OKAY"]);

        Assert.Single(activationForms);
        Assert.Equal("Okay", activationForms[0], StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseWakeWordActivationForms_WhenDelimited_ReturnsNormalizedActivationForms()
    {
        var activationForms = ActivationForms.Parse("Okay, Nova; Echo");

        Assert.Equal(3, activationForms.Length);
        Assert.Contains("Okay", activationForms);
        Assert.Contains("Nova", activationForms);
        Assert.Contains("Echo", activationForms);
    }

    [Fact]
    public void TryExtractCommandText_WhenTranscriptContainsWakeWordAndCommand_ReturnsNormalizedCommandText()
    {
        var success = CommandTextExtractor.TryExtractCommandText(
            "Okay, bitte Hilfe vorlesen!",
            ["Okay"],
            out var commandText);

        Assert.True(success);
        Assert.Equal("bitte hilfe vorlesen", commandText);
    }

    [Fact]
    public void TryExtractCommandText_WhenTranscriptContainsWakeWordOnly_ReturnsEmptyCommandText()
    {
        var success = CommandTextExtractor.TryExtractCommandText(
            "Okay",
            ["Okay"],
            out var commandText);

        Assert.True(success);
        Assert.Equal(string.Empty, commandText);
    }

    [Fact]
    public void ContainsLeadingWakeWord_WhenWakeWordStartsTranscript_ReturnsTrue()
    {
        var result = CommandTextExtractor.ContainsLeadingWakeWord("Okay bitte Hilfe vorlesen", ["Okay"]);

        Assert.True(result);
    }

    [Fact]
    public void ContainsLeadingWakeWord_WhenWakeWordAppearsLaterInTranscript_ReturnsFalse()
    {
        var result = CommandTextExtractor.ContainsLeadingWakeWord("ich sagte vorher okay", ["Okay"]);

        Assert.False(result);
    }

    [Fact]
    public void TryExtractLeadingCommandText_WhenWakeWordAppearsOnlyLater_ReturnsFalse()
    {
        var success = CommandTextExtractor.TryExtractLeadingCommandText(
            "zuerst etwas anderes dann okay bitte hilfe vorlesen",
            ["Okay"],
            out var commandText);

        Assert.False(success);
        Assert.Equal(string.Empty, commandText);
    }

    [Theory]
    [InlineData("Okay")]
    [InlineData("Ok")]
    [InlineData("OK")]
    public void TryExtractLeadingCommandText_WhenRecognitionOnlyActivationIsModeled_ReturnsTrue(string transcript)
    {
        var success = CommandTextExtractor.TryExtractLeadingCommandText(
            transcript,
            VariantInventory,
            out var commandText);

        Assert.True(success);
        Assert.Equal(string.Empty, commandText);
    }

    [Theory]
    [InlineData("Okay Hilfe", "hilfe")]
    [InlineData("Ok bitte starten", "bitte starten")]
    [InlineData("OK formular offnen", "formular offnen")]
    public void TryExtractLeadingCommandText_WhenActivationFormHasInlineCommand_ReturnsNormalizedCommandText(
        string transcript,
        string expectedCommandText)
    {
        var success = CommandTextExtractor.TryExtractLeadingCommandText(
            transcript,
            VariantInventory,
            out var commandText);

        Assert.True(success);
        Assert.Equal(expectedCommandText, commandText);
    }

    [Theory]
    [InlineData("Okay Alpha Beta", "alpha beta")]
    [InlineData("OK Hilfe", "hilfe")]
    public void TryExtractLeadingCommandText_WhenTranscriptUsesRecognizedOrInlineActivationForms_ReturnsExpectedCommand(
        string transcript,
        string expectedCommandText)
    {
        var success = CommandTextExtractor.TryExtractLeadingCommandText(
            transcript,
            VariantInventory,
            out var commandText);

        Assert.True(success);
        Assert.Equal(expectedCommandText, commandText);
    }

    [Theory]
    [InlineData("Okay Alex. Okay Alex.", "")]
    [InlineData("Hey Alex. Okay Alex.", "")]
    [InlineData("Okay Alex. Ok Alex.", "")]
    [InlineData("Ok Alex. Hey Alex.", "")]
    [InlineData("Hey Alex. Okay Alex. Ok Alex.", "")]
    [InlineData("oK aLeX?! HEY ALEX... Hilfe", "hilfe")]
    [InlineData("Hey Alex. Okay Alex. Hilfe", "hilfe")]
    [InlineData("Okay Alex wiederholen", "wiederholen")]
    [InlineData("Okay Alex suche nach Okay Alex", "suche nach okay alex")]
    [InlineData("Okay Alex bitte Okay Alex", "bitte okay alex")]
    public void TryExtractLeadingCommandText_WhenActivationSequenceEnds_PreservesCompleteRemainder(
        string transcript,
        string expectedCommandText)
    {
        var success = CommandTextExtractor.TryExtractLeadingCommandText(
            transcript,
            AlexInventory,
            out var commandText);

        Assert.True(success);
        Assert.Equal(expectedCommandText, commandText);
    }

    [Theory]
    [MemberData(nameof(ModeledActivationOnlySequenceCases))]
    public void TryExtractLeadingCommandText_WhenModeledActivationSequenceHasNoCommand_ReturnsEmptyRemainder(
        string isoCode,
        string sequenceKind)
    {
        var activationForms = VoiceInterfaceModelCatalog.WakeWord.GetActivationFormsForLanguage(isoCode);
        var recognitionOnlyActivationForms = VoiceInterfaceModelCatalog.WakeWord
            .GetRecognitionOnlyActivationFormsForLanguage(isoCode);
        Assert.True(activationForms.Count >= 2);
        Assert.NotEmpty(recognitionOnlyActivationForms);
        var inventory = new WakeWordInventory(activationForms, recognitionOnlyActivationForms);
        var transcript = sequenceKind switch
        {
            "single" => activationForms[0],
            "canonical-pair" => $"{activationForms[0]}. {activationForms[1]}",
            "recognition-pair" => $"{activationForms[0]}. {recognitionOnlyActivationForms[0]}",
            _ => throw new InvalidOperationException($"Unknown activation sequence kind '{sequenceKind}'.")
        };

        var success = CommandTextExtractor.TryExtractLeadingCommandText(
            transcript,
            inventory,
            out var commandText);

        Assert.True(success);
        Assert.Equal(string.Empty, commandText);
    }

    [Theory]
    [InlineData("de", "Hilfe", "hilfe")]
    [InlineData("en", "Help", "help")]
    [InlineData("fr", "Aide", "aide")]
    [InlineData("es", "Ayuda", "ayuda")]
    [InlineData("it", "Aiuto", "aiuto")]
    public void TryExtractLeadingCommandText_WhenModeledActivationRepeats_PreservesLocalizedCommand(
        string isoCode,
        string localizedCommand,
        string expectedCommandText)
    {
        var activationForms = VoiceInterfaceModelCatalog.WakeWord.GetActivationFormsForLanguage(isoCode);
        var recognitionOnlyActivationForms = VoiceInterfaceModelCatalog.WakeWord
            .GetRecognitionOnlyActivationFormsForLanguage(isoCode);
        Assert.True(activationForms.Count >= 2);
        var inventory = new WakeWordInventory(activationForms, recognitionOnlyActivationForms);
        var transcript = $"{activationForms[0]}. {activationForms[1]}. {localizedCommand}";

        var success = CommandTextExtractor.TryExtractLeadingCommandText(
            transcript,
            inventory,
            out var commandText);

        Assert.True(success);
        Assert.Equal(expectedCommandText, commandText);
    }

    [Theory]
    [InlineData("zuerst Hey Alex Okay Alex Hilfe", "hilfe")]
    [InlineData("zuerst Hey Alex bitte Okay Alex", "bitte okay alex")]
    public void TryExtractCommandText_WhenFirstActivationSequenceEnds_PreservesCompleteRemainder(
        string transcript,
        string expectedCommandText)
    {
        var success = CommandTextExtractor.TryExtractCommandText(
            transcript,
            AlexInventory,
            out var commandText);

        Assert.True(success);
        Assert.Equal(expectedCommandText, commandText);
    }

    [Theory]
    [InlineData("Hallo")]
    [InlineData("Luma")]
    [InlineData("Luma Okay")]
    public void TryExtractLeadingCommandText_WhenTranscriptDoesNotMatchModeledVariants_ReturnsFalse(string transcript)
    {
        var success = CommandTextExtractor.TryExtractLeadingCommandText(
            transcript,
            VariantInventory,
            out var commandText);

        Assert.False(success);
        Assert.Equal(string.Empty, commandText);
    }
}
