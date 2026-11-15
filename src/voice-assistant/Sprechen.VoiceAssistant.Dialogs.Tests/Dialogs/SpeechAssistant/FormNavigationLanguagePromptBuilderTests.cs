using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormNavigationLanguagePromptBuilderTests
{
    [Fact]
    public void GetMissingArgumentPrompt_WithContractPrompt_InterpolatesSupportedLanguages()
    {
        var contract = CreatePromptContract(
            FormDialogPromptIds.ChangeLanguageMissingArgument,
            "Modellsprachen: {{items}}.");

        var result = FormNavigationLanguagePromptBuilder.GetMissingArgumentPrompt(
            contract,
            "de",
            "Deutsch, Englisch",
            PolitenessLevel.Formal);

        Assert.Equal("Modellsprachen: Deutsch, Englisch.", result);
    }

    [Fact]
    public void GetConfirmationPrompt_WithoutContractPrompt_UsesLanguageResolverFallback()
    {
        var result = FormNavigationLanguagePromptBuilder.GetConfirmationPrompt("de", "Deutsch");

        Assert.Contains("Deutsch", result);
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
