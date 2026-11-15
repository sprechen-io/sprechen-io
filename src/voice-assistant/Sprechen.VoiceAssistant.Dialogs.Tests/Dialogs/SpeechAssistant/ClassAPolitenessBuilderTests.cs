using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die gewählte Anrede in Formularhinweisen aus dem allgemeinen Katalog.
/// Der Formularvertrag überschreibt nur einen anderen Hinweis, sodass die geprüften Texte
/// aus dem Katalog stammen. Formelle und informelle Anrede werden ausdrücklich vorgegeben.
/// </summary>
public sealed class ClassAPolitenessBuilderTests
{
    private static readonly FormContract NoOverrideContract = CreateContract();

    [Fact]
    public void CancelRefractory_HonoursTheSelectedRegister()
    {
        Assert.Equal(
            "Abbrechen wurde gerade angefragt. Bitte warte kurz.",
            FormConfirmationPromptBuilder.GetCancelRefractoryPrompt(NoOverrideContract, "de", PolitenessLevel.Informal));
        Assert.Equal(
            "Abbrechen wurde gerade angefragt. Bitte warten Sie kurz.",
            FormConfirmationPromptBuilder.GetCancelRefractoryPrompt(NoOverrideContract, "de", PolitenessLevel.Formal));
    }

    [Fact]
    public void ReparableConfirmation_HonoursTheSelectedRegister()
    {
        var informal = FormConfirmationPromptBuilder.GetReparableConfirmationPrompt(
            NoOverrideContract, "de", "Name", "Max", includesValue: true, PolitenessLevel.Informal);
        var formal = FormConfirmationPromptBuilder.GetReparableConfirmationPrompt(
            NoOverrideContract, "de", "Name", "Max", includesValue: true, PolitenessLevel.Formal);

        Assert.Contains("sag übernehmen", informal, StringComparison.Ordinal);
        Assert.Contains("sagen Sie übernehmen", formal, StringComparison.Ordinal);
    }

    [Fact]
    public void HelpCommandsPointer_HonoursTheSelectedRegister()
    {
        Assert.Equal(
            "Für alle Befehle sag: Okay Alex welche Befehle.",
            FormHelpCommandsPointerPromptBuilder.BuildPrompt(NoOverrideContract, "de", PolitenessLevel.Informal));
        Assert.Equal(
            "Für alle Befehle sagen Sie: Okay Alex welche Befehle.",
            FormHelpCommandsPointerPromptBuilder.BuildPrompt(NoOverrideContract, "de", PolitenessLevel.Formal));
    }

    [Fact]
    public void CommandAnchorHint_NullContract_HonoursTheSelectedRegister()
    {
        // Auch ohne Formularvertrag muss die gewählte Anrede im allgemeinen Katalog aufgelöst werden.
        Assert.Contains("sag zuerst", FormCommandAnchorHintPromptBuilder.BuildPrompt(null, "de", PolitenessLevel.Informal), StringComparison.Ordinal);
        Assert.Contains("sagen Sie zuerst", FormCommandAnchorHintPromptBuilder.BuildPrompt(null, "de", PolitenessLevel.Formal), StringComparison.Ordinal);
    }

    [Fact]
    public void DigitsContinueOrDone_HonoursTheSelectedRegister()
    {
        Assert.Equal(
            "Sag die n\u00e4chsten Ziffern, oder fertig.",
            FormDigitsPromptBuilder.GetDigitsContinueOrDonePrompt(NoOverrideContract, "de", PolitenessLevel.Informal));
        Assert.Equal(
            "Sagen Sie die n\u00e4chsten Ziffern, oder fertig.",
            FormDigitsPromptBuilder.GetDigitsContinueOrDonePrompt(NoOverrideContract, "de", PolitenessLevel.Formal));
    }

    [Fact]
    public void SpellingContinueOrDone_HonoursTheSelectedRegister()
    {
        Assert.Equal(
            "Sag die n\u00e4chsten Buchstaben, zur\u00fcck zum L\u00f6schen, oder fertig.",
            FormSpellingWorkflowPromptBuilder.GetSpellingContinueOrDonePrompt(NoOverrideContract, "de", PolitenessLevel.Informal));
        Assert.Equal(
            "Sagen Sie die n\u00e4chsten Buchstaben, zur\u00fcck zum L\u00f6schen, oder fertig.",
            FormSpellingWorkflowPromptBuilder.GetSpellingContinueOrDonePrompt(NoOverrideContract, "de", PolitenessLevel.Formal));
    }

    [Fact]
    public void SetSelectionHint_HonoursTheSelectedRegister()
    {
        Assert.Equal(
            "Wähle 1 bis 3 Optionen.",
            FormSetWorkflowPromptBuilder.GetSetSelectionHintPrompt(NoOverrideContract, "de", 1, 3, PolitenessLevel.Informal));
        Assert.Equal(
            "Wählen Sie 1 bis 3 Optionen.",
            FormSetWorkflowPromptBuilder.GetSetSelectionHintPrompt(NoOverrideContract, "de", 1, 3, PolitenessLevel.Formal));
    }

    [Theory]
    [InlineData("de", "sag ", "sagen Sie ")]
    [InlineData("fr", "dis ", "dites ")]
    public void UnrecognizedInputThird_HonoursTheSelectedRegister(string isoCode, string informalToken, string formalToken)
    {
        var informal = FormInputFeedbackPromptBuilder.GetUnrecognizedInputThirdPrompt(NoOverrideContract, isoCode, PolitenessLevel.Informal);
        var formal = FormInputFeedbackPromptBuilder.GetUnrecognizedInputThirdPrompt(NoOverrideContract, isoCode, PolitenessLevel.Formal);

        Assert.Contains(informalToken, informal, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(formalToken, formal, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(formal, informal);
    }

    [Fact]
    public void PromptWithoutAVariant_KeepsBaseWordingForBothRegisters()
    {
        // Ohne eigene Anredevarianten liefern beide Einstellungen denselben Grundtext.
        Assert.Equal(
            FormDigitsPromptBuilder.GetDigitsNoValuePrompt(NoOverrideContract, "de", PolitenessLevel.Formal),
            FormDigitsPromptBuilder.GetDigitsNoValuePrompt(NoOverrideContract, "de", PolitenessLevel.Informal));
    }

    private static FormContract CreateContract()
        => new()
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
                                // Diese andere Hinweiskennung lässt die geprüften Texte aus dem allgemeinen Katalog unverändert.
                                PromptId = "dialog.form.orientation",
                                Text = new LocalizedText { ["de"] = "Formular." }
                            }
                        ]
                    }
                }
            }
        };
}
