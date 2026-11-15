using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Speech.Runtime.Tests.PromptTemplates;

/// <summary>
/// Prüft Einführung und Wiederholungshinweis in beiden Anredeformen für fünf Sprachen.
/// Die englischen Varianten sind im Modell gleich, die übrigen unterscheiden sich;
/// beide Formen nennen das Aktivierungswort.
/// </summary>
public sealed class FormDialogFlowPromptTextResolverPolitenessTests
{
    public static TheoryData<string, PolitenessLevel> SupportedLanguagesWithPoliteness()
    {
        var data = new TheoryData<string, PolitenessLevel>();
        foreach (var isoCode in new[] { "de", "en", "fr", "es", "it" })
        {
            data.Add(isoCode, PolitenessLevel.Formal);
            data.Add(isoCode, PolitenessLevel.Informal);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(SupportedLanguagesWithPoliteness))]
    public void GetSessionWakeWordIntroPrompt_ResolvesTheSelectedRegisterAndTeachesTheWakeWord(
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        var intro = FormDialogFlowPromptTextResolver.GetSessionWakeWordIntroPrompt(isoCode, politenessLevel);

        Assert.False(string.IsNullOrWhiteSpace(intro));
        Assert.Contains("Okay Alex", intro, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(SupportedLanguagesWithPoliteness))]
    public void GetSessionRepeatHintPrompt_ResolvesTheSelectedRegisterAndTeachesTheWakeAnchor(
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        var repeatHint = FormDialogFlowPromptTextResolver.GetSessionRepeatHintPrompt(isoCode, politenessLevel);

        Assert.False(string.IsNullOrWhiteSpace(repeatHint));
        Assert.Contains("Okay Alex", repeatHint, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void SessionPrompts_DifferBetweenRegisters_InEveryTuVousLanguage(string isoCode)
    {
        // Unterschiedliche Texte verhindern einen unbemerkten Rückfall auf die formale Vorlage.
        Assert.NotEqual(
            FormDialogFlowPromptTextResolver.GetSessionWakeWordIntroPrompt(isoCode, PolitenessLevel.Formal),
            FormDialogFlowPromptTextResolver.GetSessionWakeWordIntroPrompt(isoCode, PolitenessLevel.Informal));
        Assert.NotEqual(
            FormDialogFlowPromptTextResolver.GetSessionRepeatHintPrompt(isoCode, PolitenessLevel.Formal),
            FormDialogFlowPromptTextResolver.GetSessionRepeatHintPrompt(isoCode, PolitenessLevel.Informal));
    }

    [Fact]
    public void SessionPrompts_AreIdenticalAcrossRegisters_InEnglishByDesign()
    {
        // Das Modell verwendet für Englisch in beiden Anredeformen denselben Text.
        // Die einheitlichen Varianten ersparen dem Auflöser einen sprachspezifischen Sonderfall.
        Assert.Equal(
            FormDialogFlowPromptTextResolver.GetSessionWakeWordIntroPrompt("en", PolitenessLevel.Formal),
            FormDialogFlowPromptTextResolver.GetSessionWakeWordIntroPrompt("en", PolitenessLevel.Informal));
        Assert.Equal(
            FormDialogFlowPromptTextResolver.GetSessionRepeatHintPrompt("en", PolitenessLevel.Formal),
            FormDialogFlowPromptTextResolver.GetSessionRepeatHintPrompt("en", PolitenessLevel.Informal));
    }

    [Fact]
    public void SessionPrompts_FormalRegisterMatchesThePolitenessBlindResolution()
    {
        // Ohne Anredeparameter gilt die formale Variante.
        Assert.Equal(
            FormDialogFlowPromptTextResolver.GetSessionWakeWordIntroPrompt("de"),
            FormDialogFlowPromptTextResolver.GetSessionWakeWordIntroPrompt("de", PolitenessLevel.Formal));
        Assert.Equal(
            FormDialogFlowPromptTextResolver.GetSessionRepeatHintPrompt("de"),
            FormDialogFlowPromptTextResolver.GetSessionRepeatHintPrompt("de", PolitenessLevel.Formal));
    }

    [Theory]
    [InlineData("de", "sag ", "du")]
    [InlineData("fr", "dis ", "tu")]
    [InlineData("es", "di ", "has")]
    [InlineData("it", "di' ", "hai")]
    public void SessionPrompts_InformalVariantsSpeakTheInformalRegisterTokens(
        string isoCode,
        string informalImperative,
        string informalRepeatHintToken)
    {
        var intro = FormDialogFlowPromptTextResolver.GetSessionWakeWordIntroPrompt(isoCode, PolitenessLevel.Informal);
        var repeatHint = FormDialogFlowPromptTextResolver.GetSessionRepeatHintPrompt(isoCode, PolitenessLevel.Informal);

        Assert.Contains(informalImperative, intro, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(informalRepeatHintToken, repeatHint, StringComparison.OrdinalIgnoreCase);
    }
}
