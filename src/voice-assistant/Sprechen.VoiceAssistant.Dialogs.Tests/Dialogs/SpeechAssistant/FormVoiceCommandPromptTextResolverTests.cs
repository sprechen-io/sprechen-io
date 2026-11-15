using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandPromptTextResolverTests
{
    private readonly FormVoiceCommandPromptTextResolver _resolver = new();

    [Fact]
    public void Resolve_WhenPromptIdsMatchModelPrompts_ReturnsTrimmedLocalizedTexts()
    {
        var contract = CreateContract();

        var prompts = _resolver.Resolve(
            contract,
            ImmutableArray.Create("prompt.main", " ", "PROMPT.SECOND", "missing"),
            "de",
            new VoiceDialogSettings());

        Assert.Equal(["Erster Prompt", "Zweiter Prompt"], prompts.ToArray());
    }

    [Fact]
    public void Resolve_WhenContractHasNoVoicePrompts_ReturnsEmpty()
    {
        var contract = CreateContract();
        contract.Projections = null;

        var prompts = _resolver.Resolve(
            contract,
            ImmutableArray.Create("prompt.main"),
            "de",
            new VoiceDialogSettings());

        Assert.Empty(prompts);
    }

    private static FormContract CreateContract()
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Formular" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
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
                                PromptId = "prompt.main",
                                Text = new LocalizedText { ["de"] = " Erster Prompt " }
                            },
                            new VoicePrompt
                            {
                                PromptId = "prompt.second",
                                Text = new LocalizedText { ["de"] = "Zweiter Prompt" }
                            }
                        ]
                    }
                }
            }
        };
    }
}
