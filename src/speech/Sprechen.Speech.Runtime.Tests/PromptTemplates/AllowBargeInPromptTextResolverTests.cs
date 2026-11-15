using Sprechen.Speech.ModelRuntime.PromptTemplates;

namespace Sprechen.Speech.Runtime.Tests.PromptTemplates;

public sealed class AllowBargeInPromptTextResolverTests
{
    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void ResolvesEnabledDisabledAndAlreadyVariants_PerLanguage(string isoCode)
    {
        var enabled = AllowBargeInPromptTextResolver.GetEnabledPrompt(isoCode);
        var disabled = AllowBargeInPromptTextResolver.GetDisabledPrompt(isoCode);
        var alreadyEnabled = AllowBargeInPromptTextResolver.GetAlreadyEnabledPrompt(isoCode);
        var alreadyDisabled = AllowBargeInPromptTextResolver.GetAlreadyDisabledPrompt(isoCode);

        Assert.False(string.IsNullOrWhiteSpace(enabled));
        Assert.False(string.IsNullOrWhiteSpace(disabled));
        Assert.False(string.IsNullOrWhiteSpace(alreadyEnabled));
        Assert.False(string.IsNullOrWhiteSpace(alreadyDisabled));

        Assert.NotEqual(enabled, disabled);
        Assert.NotEqual(alreadyEnabled, alreadyDisabled);
        Assert.NotEqual(enabled, alreadyEnabled);
        Assert.NotEqual(disabled, alreadyDisabled);
    }
}
