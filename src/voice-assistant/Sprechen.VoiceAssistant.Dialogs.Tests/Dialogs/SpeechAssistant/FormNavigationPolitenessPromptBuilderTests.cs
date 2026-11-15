using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormNavigationPolitenessPromptBuilderTests
{
    [Fact]
    public void GetUnsupportedPrompt_WithContractPrompt_InterpolatesSupportedOptions()
    {
        var contract = CreatePromptContract(
            FormDialogPromptIds.ChangePolitenessUnsupported,
            "Modelloptionen: {{items}}.");

        var result = FormNavigationPolitenessPromptBuilder.GetUnsupportedPrompt(
            contract,
            "de",
            "formal, informell",
            PolitenessLevel.Formal);

        Assert.Equal("Modelloptionen: formal, informell.", result);
    }

    [Fact]
    public void GetAlreadySelectedPrompt_FallbackInterpolatesPolitenessLabel()
    {
        var result = FormNavigationPolitenessPromptBuilder.GetAlreadySelectedPrompt("de", "formell");

        Assert.Contains("formell", result);
        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    private static FormContract CreatePromptContract(string promptId, string deText)
        => new()
        {
            SupportedLanguages = ["de", "en"],
            DefaultLanguage = "de",
            Projections = new ContractProjections
            {
                Voice = new VoiceProjection
                {
                    Hints = new VoiceHintsDefinition
                    {
                        Prompts =
                        [
                            new VoicePrompt
                            {
                                PromptId = promptId,
                                Text = new LocalizedText
                                {
                                    ["de"] = deText
                                }
                            }
                        ]
                    }
                }
            }
        };
}
