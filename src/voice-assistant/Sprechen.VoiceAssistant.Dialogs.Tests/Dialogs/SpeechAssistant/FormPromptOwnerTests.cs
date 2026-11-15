using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormPromptOwnerTests
{
    [Fact]
    public void InputFeedbackPromptBuilder_UsesModelPromptWhenAvailable()
    {
        var contract = CreatePromptContract(
            FormDialogPromptIds.VoiceInputForbiddenWithFallback,
            "Modell sagt: {{silentInputFallback}}.");

        var result = FormInputFeedbackPromptBuilder.GetVoiceInputForbiddenPrompt(
            contract,
            "de",
            PolitenessLevel.Formal,
            "Tastatur");

        Assert.Equal("Modell sagt: Tastatur.", result);
    }

    [Fact]
    public void ConfirmationPromptBuilder_InterpolatesConfirmationPrefix()
    {
        var contract = CreatePromptContract(
            FormDialogPromptIds.ReparableConfirmation,
            "{{confirmationPrefix}} aus Modell.");

        var result = FormConfirmationPromptBuilder.GetReparableConfirmationPrompt(
            contract,
            "de",
            "Name",
            "Max",
            includesValue: true,
            PolitenessLevel.Formal);

        Assert.Equal("Name ist Max aus Modell.", result);
    }

    [Fact]
    public void OptionsPromptBuilder_UsesNoOptionsPromptForEmptyItems()
    {
        var contract = CreatePromptContract(FormDialogPromptIds.OptionsNone, "Keine Modelloptionen.");

        var result = FormOptionsPromptBuilder.FormatOptionsPrompt(
            contract,
            "de",
            PolitenessLevel.Formal,
            []);

        Assert.Equal("Keine Modelloptionen.", result);
    }

    [Fact]
    public void ConstraintViolationPromptFormatter_CombinesMessageAndRemediation()
    {
        var violation = new FormConstraintViolation(
            "field1",
            "constraint1",
            SeverityLevels.Error,
            ConstraintKinds.Required,
            "Feld ist erforderlich.",
            "Bitte eingeben.",
            ConstraintScopes.Field,
            null);

        var result = FormConstraintViolationPromptFormatter.FormatViolationPrompt(
            CreatePromptContract(FormDialogPromptIds.OptionsNone, "Keine Optionen."),
            "de",
            PolitenessLevel.Formal,
            violation,
            fieldLabel: null,
            understoodValue: null,
            includesValue: false);

        Assert.Equal("Feld ist erforderlich. Bitte eingeben.", result);
    }

    private static FormContract CreatePromptContract(string promptId, string deText)
    {
        return new FormContract
        {
            SupportedLanguages = ["de", "en", "fr", "es", "it"],
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
}
