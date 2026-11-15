using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormPromptCompositionTests
{
    private readonly FormPromptComposition _composition = FormPromptComposition.Default;

    [Fact]
    public void ResolveFallbackPrompt_AppliesTemplateParameters()
    {
        var prompt = _composition.ResolveFallbackPrompt(
            "de",
            FormDialogPromptIds.SummaryValueLine,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.FieldLabel] = " Name ",
                [FormPromptTemplateParameterNames.FieldValue] = " Max "
            });

        Assert.Equal("Name ist Max", prompt);
    }

    [Fact]
    public void ResolvePromptOrFallback_UsesContractPromptBeforeFallback()
    {
        var contract = CreatePromptContract(FormDialogPromptIds.SummaryValueLine, "{{fieldLabel}} => {{fieldValue}}");

        var prompt = _composition.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.SummaryValueLine,
            "de",
            PolitenessLevel.Formal,
            fallback: "fallback",
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.FieldLabel] = "Name",
                [FormPromptTemplateParameterNames.FieldValue] = "Max"
            });

        Assert.Equal("Name => Max", prompt);
    }

    [Fact]
    public void ResolveOrFallback_UsesFallbackPromptWhenContractIsMissing()
    {
        var prompt = _composition.ResolveOrFallback(
            contract: null,
            FormDialogPromptIds.StatusSummary,
            FormDialogPromptIds.NoErrors,
            "en",
            PolitenessLevel.Formal);

        Assert.Contains("no errors", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void JoinPromptItems_TrimsAndSkipsEmptyItems()
    {
        var prompt = _composition.JoinPromptItems([" first ", "", "  ", "second"]);

        Assert.Equal("first second", prompt);
    }

    [Theory]
    [InlineData("DE", "de")]
    [InlineData("unknown", "en")]
    [InlineData("", "en")]
    public void NormalizeIsoCode_ReturnsSupportedTwoLetterCodeOrEnglish(string isoCode, string expected)
    {
        var normalized = _composition.NormalizeIsoCode(isoCode);

        Assert.Equal(expected, normalized);
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
