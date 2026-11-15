using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormDialogStateSnapshotStoreTests
{
    [Fact]
    public void Take_CreatesDefensiveCopies()
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal) { ["name"] = "Alice" };
        var fieldGroups = new Dictionary<FieldGroupId, bool> { [FieldGroupId.From("s1")] = true };
        var fieldsVisible = new Dictionary<FieldId, bool> { [FieldId.From("f1")] = true };
        var fieldsEnabled = new Dictionary<FieldId, bool> { [FieldId.From("f1")] = true };
        var fieldsReadOnly = new Dictionary<FieldId, bool> { [FieldId.From("f1")] = false };
        var inputModeOverrides = new Dictionary<FieldId, string> { [FieldId.From("f1")] = "voice" };

        var snapshot = FormDialogStateSnapshotStore.Take(
            values,
            fieldGroups,
            fieldsVisible,
            fieldsEnabled,
            fieldsReadOnly,
            inputModeOverrides,
            focusedFieldId: "f1");

        values["name"] = "Bob";
        fieldGroups[FieldGroupId.From("s1")] = false;
        fieldsVisible[FieldId.From("f1")] = false;
        fieldsEnabled[FieldId.From("f1")] = false;
        fieldsReadOnly[FieldId.From("f1")] = true;
        inputModeOverrides[FieldId.From("f1")] = "spelling";

        Assert.Equal("Alice", snapshot.ValuesByVariableId["name"]);
        Assert.True(snapshot.FieldGroupVisibleById[FieldGroupId.From("s1")]);
        Assert.True(snapshot.FieldVisibleById[FieldId.From("f1")]);
        Assert.True(snapshot.FieldEnabledById[FieldId.From("f1")]);
        Assert.False(snapshot.FieldReadOnlyById[FieldId.From("f1")]);
        Assert.Equal("voice", snapshot.FieldInputModeOverrides[FieldId.From("f1")]);
        Assert.Equal("f1", snapshot.FocusedFieldId);
    }

    [Fact]
    public void RestoreSnapshot_OverwritesStateAndSetsFocus()
    {
        var snapshot = new FormDialogStateSnapshot(
            ValuesByVariableId: new Dictionary<string, string>(StringComparer.Ordinal) { ["name"] = "Alice" },
            FieldGroupVisibleById: new Dictionary<FieldGroupId, bool> { [FieldGroupId.From("s1")] = true },
            FieldVisibleById: new Dictionary<FieldId, bool> { [FieldId.From("f1")] = true },
            FieldEnabledById: new Dictionary<FieldId, bool> { [FieldId.From("f1")] = true },
            FieldReadOnlyById: new Dictionary<FieldId, bool> { [FieldId.From("f1")] = false },
            FieldInputModeOverrides: new Dictionary<FieldId, string> { [FieldId.From("f1")] = "voice" },
            FocusedFieldId: "f1");

        var store = FormMutableStateStoreTestFactory.Create();
        store.SetVariableValue("other", "stale");
        store.SetFieldInputModeOverride(FieldId.From("f2"), "digits");

        store.RestoreSnapshot(snapshot);

        Assert.Equal(FieldId.From("f1"), store.FocusedFieldId);
        Assert.Single(store.ValuesByVariableId);
        Assert.Equal("Alice", store.ValuesByVariableId["name"]);
        Assert.Single(store.FieldGroupVisibleById);
        Assert.True(store.FieldGroupVisibleById[FieldGroupId.From("s1")]);
        Assert.Single(store.FieldVisibleById);
        Assert.True(store.FieldVisibleById[FieldId.From("f1")]);
        Assert.Single(store.FieldEnabledById);
        Assert.True(store.FieldEnabledById[FieldId.From("f1")]);
        Assert.Single(store.FieldReadOnlyById);
        Assert.False(store.FieldReadOnlyById[FieldId.From("f1")]);
        Assert.Single(store.FieldInputModeOverrides);
        Assert.Equal("voice", store.FieldInputModeOverrides[FieldId.From("f1")]);
    }
}
