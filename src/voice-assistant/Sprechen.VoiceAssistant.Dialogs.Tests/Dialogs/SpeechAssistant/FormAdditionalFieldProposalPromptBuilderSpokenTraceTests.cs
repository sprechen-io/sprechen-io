using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft erzeugte Vorschlags- und Rückmeldungstexte in fünf Sprachen.
/// Die erwarteten Feldbezeichnungen und Werte müssen im fertigen Text enthalten sein;
/// das bloße Vorhandensein einer Katalogkennung würde dafür nicht genügen.
/// </summary>
public sealed class FormAdditionalFieldProposalPromptBuilderSpokenTraceTests
{
    [Theory]
    [InlineData("de", "Stadt")]
    [InlineData("en", "City")]
    [InlineData("fr", "Ville")]
    [InlineData("es", "Ciudad")]
    [InlineData("it", "Città")]
    public void SpokenTraces_ForFiveLanguages_SubstituteFieldAndValueWithoutTemplateHoles(
        string languageCode,
        string fieldLabel)
    {
        var contract = CreateMultilingualContract();
        const string fieldValue = "Berlin";

        var offer = FormAdditionalFieldProposalPromptBuilder.GetProposalPrompt(
            contract, languageCode, fieldLabel, fieldValue, PolitenessLevel.Formal);
        var applied = FormAdditionalFieldProposalPromptBuilder.GetAppliedPrompt(
            contract, languageCode, fieldLabel, fieldValue, PolitenessLevel.Formal);
        var unavailable = FormAdditionalFieldProposalPromptBuilder.GetUnavailablePrompt(
            contract, languageCode, fieldLabel, fieldValue, PolitenessLevel.Formal);
        var invalid = FormAdditionalFieldProposalPromptBuilder.GetInvalidPrompt(
            contract, languageCode, fieldLabel, fieldValue, PolitenessLevel.Formal);
        var skipped = FormAdditionalFieldProposalPromptBuilder.GetSkippedPrompt(
            contract, languageCode, fieldLabel, fieldValue, PolitenessLevel.Formal);
        var unchanged = FormAdditionalFieldProposalPromptBuilder.GetUnchangedAssurance(
            contract, languageCode, PolitenessLevel.Formal);
        var reask = FormAdditionalFieldProposalPromptBuilder.GetProposalReaskPrompt(
            contract, languageCode, fieldLabel, fieldValue, ConfirmationUndecidedReason.Silence, PolitenessLevel.Formal);

        foreach (var spoken in new[] { offer, applied, unavailable, invalid, skipped, reask })
        {
            Assert.False(string.IsNullOrWhiteSpace(spoken));
            Assert.Contains(fieldLabel, spoken, StringComparison.Ordinal);
        }

        foreach (var spoken in new[] { offer, applied, invalid, reask })
        {
            Assert.Contains(fieldValue, spoken, StringComparison.Ordinal);
        }

        Assert.DoesNotContain(fieldValue, unavailable, StringComparison.Ordinal);
        Assert.DoesNotContain(fieldValue, skipped, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(unchanged));
        Assert.DoesNotContain(fieldLabel, unchanged, StringComparison.Ordinal);
        Assert.DoesNotContain(fieldValue, unchanged, StringComparison.Ordinal);

        var offerInformal = FormAdditionalFieldProposalPromptBuilder.GetProposalPrompt(
            contract, languageCode, fieldLabel, fieldValue, PolitenessLevel.Informal);
        Assert.Contains(fieldLabel, offerInformal, StringComparison.Ordinal);
        Assert.Contains(fieldValue, offerInformal, StringComparison.Ordinal);
    }

    private static FormContract CreateMultilingualContract()
    {
        var stringValueType = JsonDocument.Parse("{\"kind\":\"string\"}").RootElement;
        var labels = new LocalizedText
        {
            ["de"] = "Stadt",
            ["en"] = "City",
            ["fr"] = "Ville",
            ["es"] = "Ciudad",
            ["it"] = "Città"
        };

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("additionalField"),
            SupportedLanguages = ["de", "en", "fr", "es", "it"],
            DefaultLanguage = "de",
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition { VariableId = VariableId.From("city"), Name = "city", ValueType = stringValueType }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("cityField"),
                    VariableId = VariableId.From("city"),
                    Label = labels,
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = labels,
                    Members =
                    [
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("cityField") }
                    ]
                }
            ]
        };
    }
}
