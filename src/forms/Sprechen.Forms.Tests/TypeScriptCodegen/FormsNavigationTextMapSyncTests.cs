using Sprechen.Forms.TypeScriptCodegen;

namespace Sprechen.Forms.Tests.TypeScriptCodegen;

/// <summary>
/// Prüft die wiederholbare Erzeugung der Navigationstexte und die ausgegebenen Schlüssel.
/// </summary>
public sealed class FormsNavigationTextMapSyncTests
{

    [Fact]
    public void Generator_IsDeterministic_AndCoversEveryEmittedKeyPerLanguage()
    {
        var first = FormsNavigationTextMapGenerator.GenerateText();
        var second = FormsNavigationTextMapGenerator.GenerateText();

        Assert.Equal(first, second);

        foreach (var language in new[] { "de", "en", "fr", "es", "it" })
        {
            Assert.Contains($"\n    {language}: {{\n", first, StringComparison.Ordinal);
        }

        foreach (var tsKey in new[]
                 {
                     "cancelConfirm", "cancelTitle", "cancelledText", "pagerNavLabel",
                     "pagerNext", "pagerNextGroupTemplate", "pagerPrevious", "pagerPreviousGroupTemplate",
                     "pagerStepTemplate", "prerequisitesTitle",
                     "selectionLink", "sharedImportPrefix", "successNextSteps", "successSubmittedText",
                     "successTitle", "validationInvalidFormatFallback", "validationInvalidValueFallback",
                     "validationRequiredFallback", "validationSummaryTitle",
                 })
        {
            Assert.Contains($"| \"{tsKey}\"", first, StringComparison.Ordinal);
        }

        Assert.Contains("var formNavigationUiLanguageFallback: UiLanguage = \"de\";", first, StringComparison.Ordinal);
        Assert.Contains("var formNavigationUiLanguagePrefixOrder: readonly UiLanguage[] = [\"en\", \"fr\", \"es\", \"it\"];", first, StringComparison.Ordinal);
    }
}
