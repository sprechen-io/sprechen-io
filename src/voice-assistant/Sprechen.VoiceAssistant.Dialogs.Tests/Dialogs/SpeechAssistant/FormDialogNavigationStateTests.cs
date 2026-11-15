using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormPrivacy;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormDialogNavigationStateTests
{
    [Fact]
    public void NavigatorState_UsesDefinitionAndMutableFocusState()
    {
        var mutableState = FormMutableStateStoreTestFactory.Create();
        var state = new FormDialogNavigationState(CreateDefinitionState(), mutableState);
        var navigatorState = (IFormNavigatorState)state;

        Assert.True(navigatorState.IsKnownField(FieldId.From("field1")));
        Assert.True(navigatorState.IsKnownFieldGroup(FieldGroupId.From("group1")));
        Assert.Equal("group1", navigatorState.GetFieldGroupId(FieldId.From("field1")));
        Assert.True(navigatorState.TryGetFieldIndex(FieldId.From("field1"), out var index));
        Assert.Equal(0, index);

        navigatorState.SetCurrentFocus(FieldId.From("field1"));

        Assert.Equal("field1", mutableState.FocusedFieldId?.Value);
        Assert.Equal("field1", navigatorState.CurrentFocusedFieldId?.Value);
    }

    [Fact]
    public void NavigatorState_FailsFastWhenServiceBackedMethodRunsBeforeBinding()
    {
        var state = new FormDialogNavigationState(CreateDefinitionState(), FormMutableStateStoreTestFactory.Create());
        var navigatorState = (IFormNavigatorState)state;

        var exception = Assert.Throws<InvalidOperationException>(() => navigatorState.IsFieldInputAvailable(FieldId.From("field1")));

        Assert.Contains("not bound", exception.Message, StringComparison.Ordinal);
    }

    private static FormDialogDefinitionState CreateDefinitionState()
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
                        Name = "variable1"
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
