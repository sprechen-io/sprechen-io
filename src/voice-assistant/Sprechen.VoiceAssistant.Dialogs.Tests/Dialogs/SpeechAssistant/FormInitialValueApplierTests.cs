using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormInitialValueApplierTests
{
    [Fact]
    public void ApplyMissingInitialValues_AppliesOnlyUnsetAndEncodableValues()
    {
        var variablesById = new[]
        {
            new VariableDefinition
            {
                VariableId = VariableId.From("name"),
                Name = "name",
                ValueType = JsonDocument.Parse("{\"kind\":\"string\"}").RootElement,
                InitialValue = JsonDocument.Parse("\"Alice\"").RootElement
            },
            new VariableDefinition
            {
                VariableId = VariableId.From("city"),
                Name = "city",
                ValueType = JsonDocument.Parse("{\"kind\":\"string\"}").RootElement,
                InitialValue = JsonDocument.Parse("\"Berlin\"").RootElement
            },
            new VariableDefinition
            {
                VariableId = VariableId.From("profile"),
                Name = "profile",
                ValueType = JsonDocument.Parse("{\"kind\":\"string\"}").RootElement,
                InitialValue = JsonDocument.Parse("{\"nested\":true}").RootElement
            }
        }.ToImmutableDictionary(variable => VariableId.From(variable.VariableId));

        var store = FormMutableStateStoreTestFactory.Create();
        store.SetVariableValue("city", "Paris");

        FormInitialValueApplier.ApplyMissingInitialValues(variablesById, store);

        Assert.Equal("Alice", store.GetVariableValue("name"));
        Assert.Equal("Paris", store.GetVariableValue("city"));
        Assert.Null(store.GetVariableValue("profile"));
    }
}


