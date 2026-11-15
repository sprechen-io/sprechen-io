using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormInitialRuntimeValueLoaderTests
{
    [Fact]
    public void ApplyKnownValues_WhenKeyIsExact_WritesTheValue()
    {
        var loader = new FormInitialRuntimeValueLoader();
        var store = FormMutableStateStoreTestFactory.Create();
        var variablesById = CreateKnownVariables();

        loader.ApplyKnownValues(
            variablesById,
            store,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["known"] = "accepted" });

        Assert.Equal("accepted", store.GetVariableValue("known"));
        Assert.Equal(1L, store.StateRevision);
    }

    [Theory]
    [InlineData(" known ")]
    [InlineData("unknown")]
    public void ApplyKnownValues_WhenKeyIsNotExact_FailsBeforeMutation(string variableId)
    {
        var loader = new FormInitialRuntimeValueLoader();
        var store = FormMutableStateStoreTestFactory.Create();
        var variablesById = CreateKnownVariables();

        Assert.Throws<InvalidOperationException>(() => loader.ApplyKnownValues(
            variablesById,
            store,
            new Dictionary<string, string>(StringComparer.Ordinal) { [variableId] = "ignored" }));
        Assert.Null(store.GetVariableValue("known"));
        Assert.Equal(0L, store.StateRevision);
    }

    [Fact]
    public void ApplyKnownValues_WhenKeyIsBlank_FailsBeforeMutation()
    {
        var loader = new FormInitialRuntimeValueLoader();
        var store = FormMutableStateStoreTestFactory.Create();

        Assert.Throws<ArgumentException>(() => loader.ApplyKnownValues(
            CreateKnownVariables(),
            store,
            new Dictionary<string, string>(StringComparer.Ordinal) { [" "] = "blank" }));
        Assert.Equal(0L, store.StateRevision);
    }

    private static ImmutableDictionary<VariableId, VariableDefinition> CreateKnownVariables()
        => ImmutableDictionary.CreateRange(
            [
                KeyValuePair.Create(
                    VariableId.From("known"),
                    new VariableDefinition
                    {
                        VariableId = VariableId.From("known"),
                        Name = "known"
                    })
            ]);
}
