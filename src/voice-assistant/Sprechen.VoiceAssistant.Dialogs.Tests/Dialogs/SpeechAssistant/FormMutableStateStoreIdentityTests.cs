using Sprechen.Mda.Contracts.V1.Identifiers;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormMutableStateStoreIdentityTests
{
    [Fact]
    public void Constructor_WhenAnyCanonicalSetIsMissing_Throws()
    {
        var variables = new HashSet<VariableId> { VariableId.From("name") };
        var fields = new HashSet<FieldId> { FieldId.From("name") };
        var groups = new HashSet<FieldGroupId> { FieldGroupId.From("main") };

        Assert.Throws<ArgumentNullException>(() => new FormMutableStateStore(null!, fields, groups));
        Assert.Throws<ArgumentNullException>(() => new FormMutableStateStore(variables, null!, groups));
        Assert.Throws<ArgumentNullException>(() => new FormMutableStateStore(variables, fields, null!));
    }

    [Fact]
    public void SetVariableValue_WhenCanonicalSetsAreBound_RejectsUnknownAndWhitespaceRetargetedKeys()
    {
        var store = CreateBoundStore();

        Assert.Throws<InvalidOperationException>(() => store.SetVariableValue("other", "x"));
        Assert.Throws<InvalidOperationException>(() => store.SetVariableValue(" name ", "x"));
        Assert.True(store.SetVariableValue("name", "Max"));
        Assert.Equal("Max", store.GetVariableValue("name"));
    }

    [Fact]
    public void RestoreSnapshot_WhenAnyMapContainsAnUnknownKey_LeavesStateUnchanged()
    {
        var store = CreateBoundStore();
        store.SetVariableValue("name", "Max");
        var revision = store.StateRevision;
        var snapshot = FormDialogStateSnapshotStore.Take(
            new Dictionary<string, string>(StringComparer.Ordinal) { ["other"] = "nope" },
            new Dictionary<FieldGroupId, bool>(),
            new Dictionary<FieldId, bool>(),
            new Dictionary<FieldId, bool>(),
            new Dictionary<FieldId, bool>(),
            new Dictionary<FieldId, string>(),
            focusedFieldId: null);

        Assert.Throws<InvalidOperationException>(() => store.RestoreSnapshot(snapshot));
        Assert.Equal("Max", store.GetVariableValue("name"));
        Assert.Equal(revision, store.StateRevision);
    }

    private static FormMutableStateStore CreateBoundStore()
    {
        return new FormMutableStateStore(
            new HashSet<VariableId> { VariableId.From("name") },
            new HashSet<FieldId> { FieldId.From("name") },
            new HashSet<FieldGroupId> { FieldGroupId.From("main") });
    }
}
