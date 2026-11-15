using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormPrivacy;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormFocus;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormFocus;

public sealed class SetFocusedFieldInputModeCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_NoFocusedField_ReturnsFalse()
    {
        var store = FormMutableStateStoreTestFactory.Create();
        var handler = CreateHandler(store);

        var result = await handler.HandleAsync(new SetFocusedFieldInputModeCommand("voice"));

        Assert.False(result);
    }

    [Fact]
    public async Task HandleAsync_FocusedFieldAndKnownMode_StoresOverride()
    {
        var store = FormMutableStateStoreTestFactory.Create();
        store.SetFocusedField(FieldId.From("field1"));
        var handler = CreateHandler(store);

        var result = await handler.HandleAsync(new SetFocusedFieldInputModeCommand("voice"));

        Assert.True(result);
        Assert.True(store.FieldInputModeOverrides.ContainsKey(FieldId.From("field1")));
        Assert.Equal("voice", store.FieldInputModeOverrides[FieldId.From("field1")]);
    }

    [Fact]
    public async Task HandleAsync_UnknownMode_ReturnsFalse()
    {
        var store = FormMutableStateStoreTestFactory.Create();
        store.SetFocusedField(FieldId.From("field1"));
        var handler = CreateHandler(store);

        var result = await handler.HandleAsync(new SetFocusedFieldInputModeCommand("totally-not-a-mode"));

        Assert.False(result);
        Assert.False(store.FieldInputModeOverrides.ContainsKey(FieldId.From("field1")));
    }

    [Fact]
    public async Task HandleAsync_ModeUnsupportedForFieldType_ReturnsFalseAndPreservesOverride()
    {
        // Für ein boolesches Feld wird der Buchstabiermodus abgelehnt.
        // Die zuvor eingestellte Tastatureingabe bleibt erhalten.
        var store = FormMutableStateStoreTestFactory.Create();
        store.SetFocusedField(FieldId.From("field1"));
        store.SetFieldInputModeOverride(FieldId.From("field1"), InputModes.Keyboard);
        var handler = CreateHandler(store, """{"kind":"boolean"}""");

        var result = await handler.HandleAsync(new SetFocusedFieldInputModeCommand("spelling"));

        Assert.False(result);
        Assert.Equal("keyboard", store.FieldInputModeOverrides[FieldId.From("field1")]);
    }

    private static SetFocusedFieldInputModeCommandHandler CreateHandler(
        FormMutableStateStore store,
        string valueTypeJson = """{"kind":"string"}""")
        => new(
            store,
            CreateDefinitionState(valueTypeJson),
            new FormFieldInputModeAvailabilityPolicy(
                store,
                CreateDefinitionState(valueTypeJson).EffectivePoliciesByVariableId));

    private static FormDialogDefinitionState CreateDefinitionState(string valueTypeJson)
    {
        var fieldsById = ImmutableDictionary.CreateRange(
            [
                KeyValuePair.Create(
                    FieldId.From("field1"),
                    new FieldDefinition
                    {
                        FieldId = FieldId.From("field1"),
                        VariableId = VariableId.From("variable1")
                    })
            ]);
        var fieldGroupsById = ImmutableDictionary.CreateRange(
            [
                KeyValuePair.Create(
                    FieldGroupId.From("group1"),
                    new FieldGroupDefinition
                    {
                        FieldGroupId = FieldGroupId.From("group1")
                    })
            ]);
        var variablesById = ImmutableDictionary.CreateRange(
            [
                KeyValuePair.Create(
                    VariableId.From("variable1"),
                    new VariableDefinition
                    {
                        VariableId = VariableId.From("variable1"),
                        Name = "variable1",
                        ValueType = System.Text.Json.JsonDocument.Parse(valueTypeJson).RootElement
                    })
            ]);

        return new FormDialogDefinitionState
        {
            Contract = new FormContract(),
            PolitenessLevel = PolitenessLevel.Formal,
            RequestedIsoCode = "de",
            LocalizationResolver = new FormLocalizationResolver(
                "de",
                defaultLanguage: "de",
                supportedLanguages: ["de"],
                strictLocalization: false,
                politenessLevel: PolitenessLevel.Formal,
                variablesById: variablesById,
                fieldGroupsById: fieldGroupsById),
            VariablesById = variablesById,
            FieldsById = fieldsById,
            FieldGroupsById = fieldGroupsById,
            OrderedFieldGroupIds = ["group1"],
            FieldGroupIndexById = ImmutableDictionary.CreateRange(
                [KeyValuePair.Create(FieldGroupId.From("group1"), 0)]),
            ParentFieldGroupById = ImmutableDictionary.CreateRange<FieldGroupId, string?>(
                [KeyValuePair.Create<FieldGroupId, string?>(FieldGroupId.From("group1"), null)]),
            OrderedFieldIds = ["field1"],
            FieldIndexById = ImmutableDictionary.CreateRange(
                [KeyValuePair.Create(FieldId.From("field1"), 0)]),
            FieldToFieldGroupId = ImmutableDictionary.CreateRange(
                [KeyValuePair.Create(FieldId.From("field1"), "group1")]),
            FieldToVariableId = ImmutableDictionary.CreateRange(
                [KeyValuePair.Create(FieldId.From("field1"), "variable1")]),
            EffectivePoliciesByVariableId = FormPrivacyTestFixture.ResolvePolicies(variablesById.Values),
            Constraints = []
        };
    }
}
