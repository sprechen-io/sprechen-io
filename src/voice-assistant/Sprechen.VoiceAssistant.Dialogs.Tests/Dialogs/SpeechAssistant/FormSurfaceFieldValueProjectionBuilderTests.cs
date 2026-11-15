using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormSurfaceFieldValueProjectionBuilderTests
{
    [Fact]
    public async Task GetSurfaceFieldValues_PublicKeepsValue_PersonalPresence_SecretOmitted()
    {
        var contract = CreateContract();
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            "de",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["publicName"] = "Ada",
                ["personalMail"] = "ada@example.com",
                ["sensitiveIban"] = "DE89370400440532013000",
                ["emptyPersonal"] = "",
                ["secretPin"] = "1234"
            });

        var surface = await interpreter.GetSurfaceFieldValuesAsync();

        Assert.Equal(FormSurfaceFieldValueKind.PublicValue, surface["publicName"].Kind);
        Assert.Equal("Ada", surface["publicName"].Value);
        Assert.Equal(FormSurfaceFieldValueKind.StoredEntryPresence, surface["personalMail"].Kind);
        Assert.Null(surface["personalMail"].Value);
        Assert.Equal(FormSurfaceFieldValueKind.StoredEntryPresence, surface["sensitiveIban"].Kind);
        Assert.Null(surface["sensitiveIban"].Value);
        Assert.Equal(FormSurfaceFieldValueKind.StoredEntryPresence, surface["emptyPersonal"].Kind);
        Assert.Null(surface["emptyPersonal"].Value);
        Assert.False(surface.ContainsKey("secretPin"));
    }

    private static FormContract CreateContract()
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
                Variable("publicName", PrivacyClass.Public),
                Variable("personalMail", PrivacyClass.Personal),
                Variable("sensitiveIban", PrivacyClass.Sensitive),
                Variable("emptyPersonal", PrivacyClass.Personal),
                Variable("secretPin", PrivacyClass.Secret)
            ],
            Fields =
            [
                Field("publicName"),
                Field("personalMail"),
                Field("sensitiveIban"),
                Field("emptyPersonal"),
                Field("secretPin")
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("publicName") },
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("personalMail") },
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("sensitiveIban") },
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("emptyPersonal") },
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("secretPin") }
                    ]
                }
            ]
        };

    private static VariableDefinition Variable(string id, PrivacyClass privacyClass)
        => new()
        {
            VariableId = VariableId.From(id),
            Name = id,
            PrivacyClass = privacyClass,
            ValueType = JsonDocument.Parse("""{"kind":"string"}""").RootElement
        };

    private static FieldDefinition Field(string id)
        => new()
        {
            FieldId = FieldId.From(id),
            VariableId = VariableId.From(id),
            Label = new LocalizedText { ["de"] = id },
            Editability = FieldEditability.UserEditable
        };
}
