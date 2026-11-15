using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Datumsbeispiele der fünf unterstützten Sprachen durch erneutes Parsen.
/// Jedes Beispiel muss zum erwarteten Datum führen, damit kein unzulässiges Format angeleitet wird.
/// </summary>
public sealed class FormDateEntryExamplePromptTests
{
    private static readonly string[] SupportedLanguages = [.. LanguageDescriptors.AllIsoCodes];

    private static FormTemporalTranscriptParser CreateParser()
        => new(() => new DateOnly(2026, 5, 19));

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void DateEntryExamplePhrase_ExistsForEveryLanguage(string iso)
    {
        var phrase = FormDateEntryExamplePromptBuilder.GetDateEntryExamplePhrase(iso);
        Assert.False(string.IsNullOrWhiteSpace(phrase));
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void DateEntryExamplePhrase_RoundTripsToTheSameAcceptedDate(string iso)
    {
        var phrase = FormDateEntryExamplePromptBuilder.GetDateEntryExamplePhrase(iso)!;


        var outcome = CreateParser().ParseDate(iso, phrase);

        // Alle Beispiele bezeichnen den 8. Januar 1980.
        Assert.True(outcome.Success, $"Example for '{iso}' did not parse: '{phrase}'.");
        Assert.Equal("1980-01-08", outcome.CanonicalValue);
    }

    [Theory]
    [InlineData("de", "Zum Beispiel")]
    [InlineData("en", "For example")]
    [InlineData("fr", "Par exemple")]
    [InlineData("es", "Por ejemplo")]
    [InlineData("it", "Per esempio")]
    public void DateEntryExamplePrompt_ComposesLocalizedFramingPlusPhrase(string iso, string expectedLead)
    {
        var prompt = FormDateEntryExamplePromptBuilder.GetDateEntryExamplePrompt(
            CreateContract(),
            iso,
            PolitenessLevel.Formal);

        var phrase = FormDateEntryExamplePromptBuilder.GetDateEntryExamplePhrase(iso)!;

        Assert.NotNull(prompt);
        Assert.StartsWith(expectedLead, prompt, StringComparison.Ordinal);
        Assert.Contains(phrase, prompt!, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", prompt, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void DateEntryExampleAndReadback_AgreeOnTheYearForm(string iso)
    {
        // Beispiel und Rücklesetext müssen für 1980 dieselbe vom Jahresrenderer erzeugte Form enthalten.
        var yearForm = FormSpokenYearRenderer.RenderSpokenYear(1980, iso);

        var example = FormDateEntryExamplePromptBuilder.GetDateEntryExamplePhrase(iso)!;
        var readback = FormTemporalSpokenValueResolver.ResolveSpokenValue(ValueTypeKinds.Date, "1980-01-08", iso);

        Assert.Contains(yearForm, example, StringComparison.Ordinal);
        Assert.Contains(yearForm, readback, StringComparison.Ordinal);
    }

    [Fact]
    public void EverySupportedLanguage_IsCovered()
    {
        foreach (var iso in SupportedLanguages)
        {
            Assert.False(
                string.IsNullOrWhiteSpace(FormDateEntryExamplePromptBuilder.GetDateEntryExamplePhrase(iso)),
                $"Missing date example for '{iso}'.");
        }
    }

    private static FormContract CreateContract()
        => new()
        {
            SupportedLanguages = ["de", "en", "fr", "es", "it"],
            DefaultLanguage = "de"
        };
}
