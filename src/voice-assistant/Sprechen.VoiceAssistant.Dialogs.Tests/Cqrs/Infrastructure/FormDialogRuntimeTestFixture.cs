using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.Infrastructure;

/// <summary>
/// Erzeugt <see cref="FormInterpreterRuntimeOwner"/> mit einem kleinen Formularvertrag
/// für die Tests der CQRS-Handler.
/// </summary>
internal static class FormDialogRuntimeTestFixture
{
    public static FormInterpreterRuntimeOwner CreateInterpreter(
        IReadOnlyDictionary<string, string>? initialValuesByVariableId = null)
    {
        return new FormInterpreterRuntimeOwner(
            CreateContract(),
            requestedIsoCode: "de",
            initialValuesByVariableId ?? ImmutableDictionary<string, string>.Empty);
    }

    public static FormContract CreateContract()
    {
        var stringValueType = JsonDocument.Parse("{\"kind\":\"string\"}").RootElement;

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("form"),
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "name",
                    ValueType = stringValueType
                },
                new VariableDefinition
                {
                    VariableId = VariableId.From("city"),
                    Name = "city",
                    ValueType = stringValueType
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("nameField"),
                    VariableId = VariableId.From("name"),
                    Label = new LocalizedText { ["de"] = "Name" },
                    Editability = FieldEditability.UserEditable
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("cityField"),
                    VariableId = VariableId.From("city"),
                    Label = new LocalizedText { ["de"] = "Stadt" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Hauptbereich" },
                    Members =
                    [
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("nameField") },
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("cityField") }
                    ]
                }
            ]
        };
    }
}
