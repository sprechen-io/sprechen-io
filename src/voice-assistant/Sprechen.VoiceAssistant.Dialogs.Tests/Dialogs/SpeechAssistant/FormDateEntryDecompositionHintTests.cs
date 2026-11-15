using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft den Datumshinweis auf ein enthaltenes Beispiel, fehlende Platzhalter
/// und ausgewählte unerwünschte Wörter für Reihenfolge und Aktivierung.
/// Die Wortprüfungen ersetzen keine vollständige sprachliche Beurteilung des Hinweises.
/// </summary>
public sealed class FormDateEntryDecompositionHintTests
{
    private static readonly string[] SupportedLanguages = [.. LanguageDescriptors.AllIsoCodes];

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void DecompositionHint_ExistsForEveryLanguage_QuotesTheSingleExamplePhrase(string iso)
    {
        var hint = FormDateEntryExamplePromptBuilder.GetDateEntryDecompositionHintPrompt(
            CreateContract(),
            iso,
            PolitenessLevel.Formal);

        Assert.False(string.IsNullOrWhiteSpace(hint), $"Missing date decomposition hint for '{iso}'.");

        var examplePhrase = FormDateEntryExamplePromptBuilder.GetDateEntryExamplePhrase(iso)!;
        // Der Hinweis muss die Beispielphrase enthalten und darf keinen offenen Platzhalter führen.
        Assert.Contains(examplePhrase, hint!, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", hint, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("de", "zuerst", "dann")]
    [InlineData("en", "first", "then")]
    [InlineData("fr", "d'abord", "puis")]
    [InlineData("es", "primero", "luego")]
    [InlineData("it", "prima", "poi")]
    public void DecompositionHint_DoesNotPrescribeComponentOrder(string iso, params string[] orderWords)
    {
        var hint = FormDateEntryExamplePromptBuilder.GetDateEntryDecompositionHintPrompt(
            CreateContract(),
            iso,
            PolitenessLevel.Formal);
        Assert.False(string.IsNullOrWhiteSpace(hint), $"Missing date decomposition hint for '{iso}'.");

        foreach (var orderWord in orderWords)
        {
            Assert.DoesNotContain(orderWord, hint!, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void DecompositionHint_TeachesStructureOnly_AddsNoCommandOrWakeWordAnchor(string iso)
    {
        var hint = FormDateEntryExamplePromptBuilder.GetDateEntryDecompositionHintPrompt(
            CreateContract(),
            iso,
            PolitenessLevel.Formal)!;


        Assert.DoesNotContain("Alex", hint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EverySupportedLanguage_IsCovered()
    {
        foreach (var iso in SupportedLanguages)
        {
            Assert.False(
                string.IsNullOrWhiteSpace(
                    FormDateEntryExamplePromptBuilder.GetDateEntryDecompositionHintPrompt(
                        CreateContract(),
                        iso,
                        PolitenessLevel.Formal)),
                $"Missing date decomposition hint for '{iso}'.");
        }
    }

    private static FormContract CreateContract()
        => new()
        {
            SupportedLanguages = ["de", "en", "fr", "es", "it"],
            DefaultLanguage = "de"
        };
}
