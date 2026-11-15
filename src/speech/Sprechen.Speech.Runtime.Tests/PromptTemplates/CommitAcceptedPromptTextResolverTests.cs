using Sprechen.Speech.ModelRuntime.PromptTemplates;

namespace Sprechen.Speech.Runtime.Tests.PromptTemplates;

public sealed class CommitAcceptedPromptTextResolverTests
{
    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void ResolvesCommitAcceptedPrompt_PerLanguage(string isoCode)
    {
        var ack = FormDialogFlowPromptTextResolver.GetCommitAcceptedPrompt(isoCode);

        Assert.False(string.IsNullOrWhiteSpace(ack));
    }
}
