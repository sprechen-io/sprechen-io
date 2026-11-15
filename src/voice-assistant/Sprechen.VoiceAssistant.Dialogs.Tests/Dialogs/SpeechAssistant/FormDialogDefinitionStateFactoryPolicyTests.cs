using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormDialogDefinitionStateFactoryPolicyTests
{
    [Fact]
    public void Create_WhenPrivacyIsUnknown_ThrowsTypedPolicyException()
    {
        var contract = CreateContract(PrivacyClass.Unknown);

        var exception = Assert.Throws<FormContractPolicyInvalidException>(
            () => FormDialogDefinitionStateFactory.Create(contract, "de", PolitenessLevel.Formal));

        Assert.Equal(EffectiveSensitiveDataPolicyFailure.UnknownPrivacyClass, exception.Failure);
        Assert.Equal(FormContractPolicyInvalidException.ErrorCode, "FORM_CONTRACT_POLICY_INVALID");
    }

    [Fact]
    public void Create_WhenPrivacyIsValid_StoresResolvedPolicyBeforeConsumers()
    {
        var contract = CreateContract(PrivacyClass.Sensitive);

        var state = FormDialogDefinitionStateFactory.Create(contract, "de", PolitenessLevel.Formal);

        var policy = state.RequireEffectivePolicy(VariableId.From("name"));
        Assert.Equal(PrivacyClasses.Sensitive, policy.PrivacyClass);
        Assert.True(policy.RequiresReadbackOptIn);
        Assert.True(policy.OffersSilentInput);
    }

    private static FormContract CreateContract(PrivacyClass privacyClass)
        => new()
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("form"),
            DisplayName = new LocalizedText { ["de"] = "Form" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            EntryFieldGroupId = FieldGroupId.From("main"),
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "name",
                    PrivacyClass = privacyClass,
                    ValueType = JsonDocument.Parse("""{"kind":"string"}""").RootElement
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("name"),
                    VariableId = VariableId.From("name"),
                    Label = new LocalizedText { ["de"] = "Name" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("name") }
                    ]
                }
            ]
        };
}
