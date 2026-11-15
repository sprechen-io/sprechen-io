using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormMutableStateStoreTests
{
    [Fact]
    public void VisibilityAndEnablement_DefaultsMatchContractRuntimeExpectations()
    {
        var store = FormMutableStateStoreTestFactory.Create();

        Assert.True(store.IsFieldVisible(FieldId.From("f1")));
        Assert.True(store.IsFieldGroupVisible(FieldGroupId.From("s1")));
        Assert.True(store.IsFieldEnabled(FieldId.From("f1")));
        Assert.False(store.IsFieldReadOnly(FieldId.From("f1")));
    }

    [Fact]
    public void Setters_ReturnChangeFlagsAndPersistValues()
    {
        var store = FormMutableStateStoreTestFactory.Create();

        Assert.True(store.SetFieldVisible(FieldId.From("f1"), false));
        Assert.False(store.SetFieldVisible(FieldId.From("f1"), false));
        Assert.False(store.IsFieldVisible(FieldId.From("f1")));

        Assert.True(store.SetFieldGroupVisible(FieldGroupId.From("s1"), false));
        Assert.False(store.SetFieldGroupVisible(FieldGroupId.From("s1"), false));
        Assert.False(store.IsFieldGroupVisible(FieldGroupId.From("s1")));

        Assert.True(store.SetFieldEnabled(FieldId.From("f1"), false));
        Assert.False(store.SetFieldEnabled(FieldId.From("f1"), false));
        Assert.False(store.IsFieldEnabled(FieldId.From("f1")));

        Assert.True(store.SetFieldReadOnly(FieldId.From("f1"), true));
        Assert.False(store.SetFieldReadOnly(FieldId.From("f1"), true));
        Assert.True(store.IsFieldReadOnly(FieldId.From("f1")));
    }

    [Fact]
    public void VariableStorage_SupportsSetGetClearAndSnapshot()
    {
        var store = FormMutableStateStoreTestFactory.Create();

        Assert.True(store.SetVariableValue("name", "Alice"));
        Assert.False(store.SetVariableValue("name", "Alice"));
        Assert.Equal("Alice", store.GetVariableValue("name"));

        var snapshot = store.GetValuesSnapshot();
        Assert.Equal("Alice", snapshot["name"]);

        Assert.True(store.ClearVariableValue("name"));
        Assert.Null(store.GetVariableValue("name"));
        Assert.False(store.ClearVariableValue("name"));
    }

    [Fact]
    public void StateRevision_Initially_IsZero()
    {
        var store = FormMutableStateStoreTestFactory.Create();

        Assert.Equal(0L, store.StateRevision);
    }

    [Fact]
    public void StateRevision_WhenMutationIsEffective_IncrementsExactlyOnce()
    {
        (string Name, Action<FormMutableStateStore> Seed, Action<FormMutableStateStore> Mutate)[] cases =
        {
            ("set a new variable value", _ => { }, store => store.SetVariableValue("variable", "value")),
            ("change a variable value", store => store.SetVariableValue("variable", "before"), store => store.SetVariableValue("variable", "after")),
            ("clear an existing variable value", store => store.SetVariableValue("variable", "value"), store => store.ClearVariableValue("variable")),
            ("change field visibility", _ => { }, store => store.SetFieldVisible(FieldId.From("field"), visible: false)),
            ("change field-group visibility", _ => { }, store => store.SetFieldGroupVisible(FieldGroupId.From("group"), visible: false)),
            ("change field enabled state", _ => { }, store => store.SetFieldEnabled(FieldId.From("field"), enabled: false)),
            ("change field read-only state", _ => { }, store => store.SetFieldReadOnly(FieldId.From("field"), readOnly: true)),
            ("move the focus", _ => { }, store => store.SetFocusedField(FieldId.From("field"))),
            ("set an input-mode override", _ => { }, store => store.SetFieldInputModeOverride(FieldId.From("field"), InputModes.Keyboard)),
        };

        foreach (var (name, seed, mutate) in cases)
        {
            var store = FormMutableStateStoreTestFactory.Create();
            seed(store);
            var before = store.StateRevision;

            mutate(store);

            Assert.True(before + 1 == store.StateRevision, $"Case '{name}' must increment the revision exactly once.");
        }
    }

    [Fact]
    public void StateRevision_WhenMutationIsANoOp_DoesNotIncrement()
    {
        (string Name, Action<FormMutableStateStore> Seed, Action<FormMutableStateStore> Mutate)[] cases =
        {
            ("set the same variable value", store => store.SetVariableValue("variable", "value"), store => store.SetVariableValue("variable", "value")),
            ("clear a missing variable value", _ => { }, store => store.ClearVariableValue("missing")),
            ("reapply field visibility", store => store.SetFieldVisible(FieldId.From("field"), visible: false), store => store.SetFieldVisible(FieldId.From("field"), visible: false)),
            ("reapply field-group visibility", store => store.SetFieldGroupVisible(FieldGroupId.From("group"), visible: false), store => store.SetFieldGroupVisible(FieldGroupId.From("group"), visible: false)),
            ("reapply field enabled state", store => store.SetFieldEnabled(FieldId.From("field"), enabled: false), store => store.SetFieldEnabled(FieldId.From("field"), enabled: false)),
            ("reapply field read-only state", store => store.SetFieldReadOnly(FieldId.From("field"), readOnly: true), store => store.SetFieldReadOnly(FieldId.From("field"), readOnly: true)),
            ("refocus the same field", store => store.SetFocusedField(FieldId.From("field")), store => store.SetFocusedField(FieldId.From("field"))),
            ("reapply the same input-mode override", store => store.SetFieldInputModeOverride(FieldId.From("field"), InputModes.Keyboard), store => store.SetFieldInputModeOverride(FieldId.From("field"), InputModes.Keyboard)),
        };

        foreach (var (name, seed, mutate) in cases)
        {
            var store = FormMutableStateStoreTestFactory.Create();
            seed(store);
            var before = store.StateRevision;

            mutate(store);

            Assert.True(before == store.StateRevision, $"Case '{name}' is a no-op and must not increment the revision.");
        }
    }

    [Fact]
    public void StateRevision_AcrossAMutationSequence_IncreasesMonotonically()
    {
        var store = FormMutableStateStoreTestFactory.Create();
        var revisions = new List<long> { store.StateRevision };

        store.SetVariableValue("variable", "value");
        revisions.Add(store.StateRevision);
        store.SetFieldVisible(FieldId.From("field"), visible: false);
        revisions.Add(store.StateRevision);
        store.SetFocusedField(FieldId.From("field"));
        revisions.Add(store.StateRevision);
        store.SetFieldInputModeOverride(FieldId.From("field"), InputModes.Keyboard);
        revisions.Add(store.StateRevision);

        Assert.Equal([0L, 1L, 2L, 3L, 4L], revisions);
    }

    [Fact]
    public void RestoreSnapshot_WhenPayloadIsEqual_StillIncrementsOnce()
    {
        // Auch die Wiederherstellung identischer Werte erhöht die Revision, damit zuvor
        // auf diese Revision bezogene Freigaben nicht unverändert weitergelten.
        var store = FormMutableStateStoreTestFactory.Create();
        store.SetVariableValue("variable", "value");
        var snapshot = TakeSnapshotOf(store);
        var before = store.StateRevision;

        store.RestoreSnapshot(snapshot);

        Assert.Equal(before + 1, store.StateRevision);
    }

    [Fact]
    public void RestoreSnapshot_NeverRestoresAnEarlierRevision()
    {
        var store = FormMutableStateStoreTestFactory.Create();
        store.SetVariableValue("variable", "value");
        var snapshot = TakeSnapshotOf(store);

        store.RestoreSnapshot(snapshot);
        var afterFirstRestore = store.StateRevision;
        store.SetVariableValue("variable", "changed");
        store.RestoreSnapshot(snapshot);


        Assert.Equal(afterFirstRestore + 2, store.StateRevision);
    }

    private static FormDialogStateSnapshot TakeSnapshotOf(FormMutableStateStore store)
        => FormDialogStateSnapshotStore.Take(
            store.ValuesByVariableId,
            store.FieldGroupVisibleById,
            store.FieldVisibleById,
            store.FieldEnabledById,
            store.FieldReadOnlyById,
            store.FieldInputModeOverrides,
            store.FocusedFieldId?.Value);
}
