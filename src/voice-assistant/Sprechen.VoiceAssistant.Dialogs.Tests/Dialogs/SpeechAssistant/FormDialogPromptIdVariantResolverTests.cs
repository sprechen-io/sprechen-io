using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormDialogPromptIdVariantResolverTests
{
    [Theory]
    [InlineData(PolitenessLevel.Formal, "prompt.formal")]
    [InlineData(PolitenessLevel.Informal, "prompt.informal")]
    public void ResolvePolitenessVariantPromptId_ReturnsExpectedSuffix(
        PolitenessLevel politenessLevel,
        string expected)
    {
        var result = FormDialogPromptIdVariantResolver.ResolvePolitenessVariantPromptId("prompt", politenessLevel);

        Assert.Equal(expected, result);
    }
}
