using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Zusammensetzung der Feldansage aus einsbasierter Position, Bezeichnung
/// und optionalem Eingabehinweis anhand deutscher und englischer Beispiele.
/// </summary>
public sealed class FormFieldPositionPromptBuilderTests
{
    [Theory]
    [InlineData(1, 6)]
    [InlineData(3, 6)]
    [InlineData(6, 6)]
    public void GetFieldPositionPrompt_ComposesOneBasedPositionAndLabel(int index, int total)
    {
        var result = FormFieldPositionPromptBuilder.GetFieldPositionPrompt(
            CreateContract(),
            "de",
            PolitenessLevel.Formal,
            index,
            total,
            "Farbe",
            expectedInputHint: null);

        Assert.Equal($"Feld {index} von {total} heißt Farbe.", result);
        Assert.DoesNotContain("{{", result, StringComparison.Ordinal);
    }

    [Fact]
    public void GetFieldPositionPrompt_FirstFieldIsOneBased_NeverZero()
    {
        var result = FormFieldPositionPromptBuilder.GetFieldPositionPrompt(
            CreateContract(),
            "de",
            PolitenessLevel.Formal,
            index: 1,
            total: 6,
            "Vorname",
            expectedInputHint: null);

        Assert.StartsWith("Feld 1 von 6 heißt", result, StringComparison.Ordinal);
        Assert.DoesNotContain("Feld 0", result, StringComparison.Ordinal);
    }

    [Fact]
    public void GetFieldPositionPrompt_WhenHintAuthored_AppendsShortHint_AndSpeaksLabelOnce()
    {
        var result = FormFieldPositionPromptBuilder.GetFieldPositionPrompt(
            CreateContract(),
            "de",
            PolitenessLevel.Formal,
            index: 2,
            total: 6,
            "Farbe",
            expectedInputHint: "Bitte angeben.");

        Assert.Equal("Feld 2 von 6 heißt Farbe. Bitte angeben.", result);

        Assert.Equal(1, CountOccurrences(result, "Farbe"));
    }

    [Fact]
    public void GetFieldPositionPrompt_WhenNoHintAuthored_NoTrailingFiller()
    {
        var result = FormFieldPositionPromptBuilder.GetFieldPositionPrompt(
            CreateContract(),
            "de",
            PolitenessLevel.Formal,
            index: 2,
            total: 6,
            "Farbe",
            expectedInputHint: "   ");

        // Ein Hinweis aus Leerzeichen gilt als fehlend; der Text endet nach der Bezeichnung.
        Assert.Equal("Feld 2 von 6 heißt Farbe.", result);
    }

    [Fact]
    public void GetFieldPositionPrompt_ResolvesEnglishVariant()
    {
        var result = FormFieldPositionPromptBuilder.GetFieldPositionPrompt(
            CreateContract(),
            "en",
            PolitenessLevel.Formal,
            index: 2,
            total: 6,
            "Color",
            expectedInputHint: null);

        Assert.Equal("Field 2 of 6: Color.", result);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var position = 0;
        while ((position = haystack.IndexOf(needle, position, StringComparison.Ordinal)) >= 0)
        {
            count++;
            position += needle.Length;
        }

        return count;
    }

    private static FormContract CreateContract()
    {
        return new FormContract
        {
            SupportedLanguages = ["de", "en", "fr", "es", "it"],
            DefaultLanguage = "de"
        };
    }
}
