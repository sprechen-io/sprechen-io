using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormsCatalogIntentMatcherTests
{
    private static readonly FormsCatalog Catalog = FormsCatalogIntentTestCatalog.Create();
    private readonly IFormsCatalogIntentMatcher _matcher = new FormsCatalogIntentMatcher();

    [Theory]
    [InlineData("de", "suche nach formular", "formular")]
    [InlineData("en", "search for service", "service")]
    public void TryExtractSearchQuery_ReturnsQuery(string isoCode, string input, string expected)
    {
        var result = _matcher.TryExtractSearchQuery(Catalog, input, isoCode, PolitenessLevel.Formal);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("de", "beschreibe leistung", "leistung")]
    [InlineData("en", "describe demo", "demo")]
    public void TryExtractDescribeTarget_ReturnsTarget(string isoCode, string input, string expected)
    {
        var result = _matcher.TryExtractDescribeTarget(Catalog, input, isoCode, PolitenessLevel.Formal);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("de", "3", 3)]
    [InlineData("de", "nummer 2", 2)]
    [InlineData("de", "zweite", 2)]
    [InlineData("en", "second", 2)]
    public void TryExtractNumber_ParsesExpectedNumber(string isoCode, string input, int expected)
    {
        var result = _matcher.TryExtractNumber(Catalog, input, isoCode, PolitenessLevel.Formal);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryExtractNumber_UnknownWord_ReturnsNull()
    {
        var result = _matcher.TryExtractNumber(Catalog, "unbekannt", "de", PolitenessLevel.Formal);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("de", "sprache wechseln")]
    [InlineData("en", "change language")]
    [InlineData("fr", "changer de langue")]
    [InlineData("es", "cambiar idioma")]
    [InlineData("it", "cambia lingua")]
    public void IsChangeLanguage_ReturnsTrueForSupportedLanguages(string isoCode, string input)
    {
        var result = _matcher.IsChangeLanguage(Catalog, input, isoCode, PolitenessLevel.Formal);

        Assert.True(result);
    }

    [Theory]
    [InlineData("de", "sprache wechseln englisch", "englisch")]
    [InlineData("en", "change language german", "german")]
    public void TryExtractChangeLanguageTarget_ReturnsTarget(string isoCode, string input, string expected)
    {
        var result = _matcher.TryExtractChangeLanguageTarget(
            Catalog,
            input,
            isoCode,
            PolitenessLevel.Formal);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsList_WhenGermanAsciiTransliterationNeedsCollapse_MatchesNormalizedVariant()
    {
        var catalog = CreateCatalogWithListUtterances("uebersicht");

        var result = _matcher.IsList(
            catalog,
            TextNormalizer.NormalizeForPhraseMatching("ubersicht"),
            "de",
            PolitenessLevel.Formal);

        Assert.True(result);
    }

    [Fact]
    public void IsList_WhenUtteranceContainsLegitimateOe_DoesNotMatchCollapsedVariant()
    {
        var catalog = CreateCatalogWithListUtterances("boeing");

        var result = _matcher.IsList(
            catalog,
            TextNormalizer.NormalizeForPhraseMatching("boing"),
            "de",
            PolitenessLevel.Formal);

        Assert.False(result);
    }

    [Fact]
    public void IsList_WhenUtteranceContainsLegitimateUeSequence_DoesNotMatchCollapsedVariant()
    {
        var catalog = CreateCatalogWithListUtterances("queue");

        var result = _matcher.IsList(
            catalog,
            TextNormalizer.NormalizeForPhraseMatching("qu"),
            "de",
            PolitenessLevel.Formal);

        Assert.False(result);
    }

    private static FormsCatalog CreateCatalogWithListUtterances(params string[] utterances)
    {
        var catalog = FormsCatalogIntentTestCatalog.Create();
        catalog.SelectionDialog = new ApplicationSelectionDialog
        {
            SchemaVersion = "1",
            ModelVersion = "1",
            SelectionDialogId = "default.selectionDialog",
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            Intents =
            [
                .. utterances.Select(utterance => new CatalogVoiceIntent
                {
                    IntentId = CatalogVoiceIntentId.List,
                    MatchMode = CatalogVoiceIntentMatchMode.Phrase,
                    Utterances =
                    [
                        new LocalizedText
                        {
                            ["de"] = utterance
                        }
                    ]
                })
            ]
        };

        return catalog;
    }
}
