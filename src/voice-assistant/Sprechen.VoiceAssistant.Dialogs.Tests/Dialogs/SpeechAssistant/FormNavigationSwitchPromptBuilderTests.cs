using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormNavigationSwitchPromptBuilderTests
{
    [Fact]
    public void GetAlreadyOnFormPrompt_WithContractPrompt_InterpolatesFormName()
    {
        var contract = CreatePromptContract(
            FormDialogPromptIds.AlreadyOnForm,
            "{{formName}} kommt aus dem Modell.");

        var result = FormNavigationSwitchPromptBuilder.GetAlreadyOnFormPrompt(
            contract,
            "de",
            "Kontaktformular",
            PolitenessLevel.Formal);

        Assert.Equal("Kontaktformular kommt aus dem Modell.", result);
    }

    [Fact]
    public void GetPrerequisitesNotMetPrompt_WithMissingItems_InterpolatesFallbackList()
    {
        var result = FormNavigationSwitchPromptBuilder.GetPrerequisitesNotMetPrompt(
            "de",
            "Antrag",
            ["Name", "", "Adresse"],
            PolitenessLevel.Formal);

        Assert.Contains("Antrag", result);
        Assert.Contains("Name, Adresse", result);
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
