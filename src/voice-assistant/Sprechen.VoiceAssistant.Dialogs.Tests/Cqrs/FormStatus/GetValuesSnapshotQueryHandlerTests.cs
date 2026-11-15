using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormStatus;

public sealed class GetValuesSnapshotQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_EmptyStore_ReturnsEmptyDictionary()
    {
        var store = FormMutableStateStoreTestFactory.Create();
        var handler = new GetValuesSnapshotQueryHandler(store, CreateVisibilityPolicy(store));

        var result = await handler.HandleAsync(new GetValuesSnapshotQuery());

        Assert.Empty(result);
    }

    [Fact]
    public async Task HandleAsync_NonFieldBoundVariables_AreRetained()
    {
        var store = FormMutableStateStoreTestFactory.Create();
        store.SetVariableValue("var1", "value1");
        store.SetVariableValue("var2", "value2");
        var handler = new GetValuesSnapshotQueryHandler(store, CreateVisibilityPolicy(store));

        var result = await handler.HandleAsync(new GetValuesSnapshotQuery());

        Assert.Equal(2, result.Count);
        Assert.Equal("value1", result["var1"]);
        Assert.Equal("value2", result["var2"]);
    }

    [Fact]
    public async Task HandleAsync_SnapshotIsIsolatedFromSubsequentMutations()
    {
        var store = FormMutableStateStoreTestFactory.Create();
        store.SetVariableValue("var1", "value1");
        var handler = new GetValuesSnapshotQueryHandler(store, CreateVisibilityPolicy(store));

        var snapshot = await handler.HandleAsync(new GetValuesSnapshotQuery());
        store.SetVariableValue("var1", "mutated");

        Assert.Equal("value1", snapshot["var1"]);
    }

    [Fact]
    public async Task HandleAsync_HiddenFieldValue_IsFilteredOut()
    {
        var store = FormMutableStateStoreTestFactory.Create();
        store.SetVariableValue("visibleVar", "keep");
        store.SetVariableValue("hiddenVar", "drop");
        store.SetFieldVisible(FieldId.From("visibleField"), true);
        store.SetFieldVisible(FieldId.From("hiddenField"), false);
        var visibility = CreateVisibilityPolicy(
            store,
            ("visibleField", "visibleVar", "group"),
            ("hiddenField", "hiddenVar", "group"));
        var handler = new GetValuesSnapshotQueryHandler(store, visibility);

        var result = await handler.HandleAsync(new GetValuesSnapshotQuery());

        Assert.True(result.ContainsKey("visibleVar"));
        Assert.False(result.ContainsKey("hiddenVar"));
    }

    [Fact]
    public async Task HandleAsync_VariableWithAtLeastOneVisibleField_IsRetained()
    {
        var store = FormMutableStateStoreTestFactory.Create();
        store.SetVariableValue("sharedVar", "keep");
        store.SetFieldVisible(FieldId.From("fieldA"), false);
        store.SetFieldVisible(FieldId.From("fieldB"), true);
        var visibility = CreateVisibilityPolicy(
            store,
            ("fieldA", "sharedVar", "group"),
            ("fieldB", "sharedVar", "group"));
        var handler = new GetValuesSnapshotQueryHandler(store, visibility);

        var result = await handler.HandleAsync(new GetValuesSnapshotQuery());

        Assert.True(result.ContainsKey("sharedVar"));
    }

    [Fact]
    public async Task HandleAsync_VariableWithAllFieldsHidden_IsFilteredOut()
    {
        var store = FormMutableStateStoreTestFactory.Create();
        store.SetVariableValue("sharedVar", "drop");
        store.SetFieldVisible(FieldId.From("fieldA"), false);
        store.SetFieldVisible(FieldId.From("fieldB"), false);
        var visibility = CreateVisibilityPolicy(
            store,
            ("fieldA", "sharedVar", "group"),
            ("fieldB", "sharedVar", "group"));
        var handler = new GetValuesSnapshotQueryHandler(store, visibility);

        var result = await handler.HandleAsync(new GetValuesSnapshotQuery());

        Assert.False(result.ContainsKey("sharedVar"));
    }

    private static FormFieldVisibilityPolicy CreateVisibilityPolicy(
        FormMutableStateStore store,
        params (string FieldId, string VariableId, string FieldGroupId)[] fields)
    {
        var fieldToFieldGroup = fields.ToImmutableDictionary(f => FieldId.From(f.FieldId), f => f.FieldGroupId);
        var fieldsById = fields.ToImmutableDictionary(
            f => FieldId.From(f.FieldId),
            f => new FieldDefinition { FieldId = FieldId.From(f.FieldId), VariableId = VariableId.From(f.VariableId) });
        var parentFieldGroup = fields
            .Select(f => f.FieldGroupId)
            .Distinct(StringComparer.Ordinal)
            .ToImmutableDictionary(g => FieldGroupId.From(g), _ => (string?)null);

        return new FormFieldVisibilityPolicy(
            store.FieldGroupVisibleById,
            store.FieldVisibleById,
            store.FieldEnabledById,
            store.FieldReadOnlyById,
            parentFieldGroup,
            fieldToFieldGroup,
            fieldsById);
    }
}
