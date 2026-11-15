using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormEditing;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormDialogCqrsStateAdapterTests
{
    [Fact]
    public async Task EditableState_SetValue_UpdatesInterpreterValueStore()
    {
        var interpreter = new FormInterpreterRuntimeOwner(CreateContract(), requestedIsoCode: "de", ImmutableDictionary<string, string>.Empty);
        var adapter = new FormDialogCqrsStateAdapter(interpreter.CqrsProjection);
        var editableState = (IEditableFormState)adapter;

        Assert.True(editableState.IsFieldKnown(FieldId.From("field1")));
        Assert.True(editableState.TryGetVariableId(FieldId.From("field1"), out var variableId));
        Assert.Equal("value1", variableId.Value);

        editableState.SetValue(VariableId.From("value1"), "Max");

        Assert.Equal("Max", editableState.GetCurrentValue(VariableId.From("value1")));
        Assert.Equal("Max", (await interpreter.GetValuesSnapshotAsync())["value1"]);
    }

    [Fact]
    public void NavigationState_ReflectsInterpreterFocusAndFieldGroups()
    {
        var interpreter = new FormInterpreterRuntimeOwner(CreateContract(), requestedIsoCode: "de", ImmutableDictionary<string, string>.Empty);
        var adapter = new FormDialogCqrsStateAdapter(interpreter.CqrsProjection);
        var navigationState = (INavigableFormState)adapter;
        var fieldGroupState = (IFieldGroupNavigableFormState)adapter;

        Assert.Equal("field1", navigationState.CurrentFocusedFieldId);
        Assert.True(navigationState.TryGetFieldIndex(FieldId.From("field1"), out var firstFieldIndex));
        Assert.Equal(0, firstFieldIndex);
        Assert.True(fieldGroupState.IsFieldGroupKnown(FieldGroupId.From("section2")));
        Assert.True(fieldGroupState.IsFieldGroupVisible(FieldGroupId.From("section2")));
        Assert.Equal(["field2"], fieldGroupState.GetFieldsInFieldGroup(FieldGroupId.From("section2")).ToArray());

        navigationState.SetCurrentFocus(FieldId.From("field2"));

        Assert.Equal("field2", fieldGroupState.CurrentFocusedFieldId);
        Assert.Equal("section2", fieldGroupState.GetCurrentFieldGroupId());
    }

    [Fact]
    public void ReadModels_ProjectCurrentStatusAndSummary()
    {
        var initialValues = ImmutableDictionary.Create<string, string>()
            .Add("value1", "Alice");
        var interpreter = new FormInterpreterRuntimeOwner(CreateContract(), requestedIsoCode: "de", initialValues);
        var adapter = new FormDialogCqrsStateAdapter(interpreter.CqrsProjection);
        var statusState = (IReadableFormStatus)adapter;
        var summaryState = (IReadableFormSummary)adapter;

        Assert.Equal("field1", statusState.CurrentFieldId);
        Assert.Equal("root", statusState.CurrentFieldGroupId);
        Assert.Equal(1, statusState.CurrentFieldIndex);
        Assert.Equal(1, statusState.CurrentFieldGroupIndex);
        Assert.Equal(2, statusState.TotalFields);
        Assert.Equal(2, statusState.TotalFieldGroups);
        Assert.Equal(0, statusState.OpenRequiredCount);
        Assert.Equal(0, statusState.ValidationErrorCount);

        Assert.Equal(["field1", "field2"], summaryState.OrderedFieldIds.ToArray());
        Assert.Equal("Erstes Feld", summaryState.GetFieldLabel("field1"));
        Assert.Equal("Alice", summaryState.GetFieldValue("field1"));
        Assert.Null(summaryState.GetFieldValue("field2"));
    }

    private static FormContract CreateContract()
    {
        var stringValueType = JsonDocument.Parse("{\"kind\":\"string\"}").RootElement;

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "test-app",
            FormId = FormId.From("test-form"),
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("root"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("value1"),
                    Name = "value1",
                    ValueType = stringValueType
                },
                new VariableDefinition
                {
                    VariableId = VariableId.From("value2"),
                    Name = "value2",
                    ValueType = stringValueType
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("field1"),
                    VariableId = VariableId.From("value1"),
                    Label = new LocalizedText { ["de"] = "Erstes Feld" },
                    Editability = FieldEditability.UserEditable
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("field2"),
                    VariableId = VariableId.From("value2"),
                    Label = new LocalizedText { ["de"] = "Zweites Feld" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("root"),
                    Title = new LocalizedText { ["de"] = "Root" },
                    Members =
                    [
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("field1") },
                        new FieldGroupMember { Kind = MemberKinds.FieldGroup, FieldGroupId = FieldGroupId.From("section2") }
                    ]
                },
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("section2"),
                    Title = new LocalizedText { ["de"] = "Unterbereich" },
                    Members =
                    [
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("field2") }
                    ]
                }
            ]
        };
    }
}


