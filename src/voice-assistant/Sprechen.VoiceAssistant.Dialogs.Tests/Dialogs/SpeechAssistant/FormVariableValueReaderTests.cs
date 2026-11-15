using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVariableValueReaderTests
{
    [Fact]
    public void TryGetValueTypeKind_ReturnsConfiguredKind()
    {
        var variables = CreateVariablesById(
            ("postalCode", "{\"kind\":\"string\"}"),
            ("tags", "{\"kind\":\"set\"}"));

        var kind = FormVariableValueReader.TryGetValueTypeKind(variables, "tags");

        Assert.Equal(ValueTypeKinds.Set, kind);
    }

    [Fact]
    public void TryGetValue_ForSet_ReturnsParsedSet()
    {
        var variables = CreateVariablesById(("tags", "{\"kind\":\"set\"}"));
        var values = ImmutableDictionary.Create<string, string>(StringComparer.Ordinal)
            .Add("tags", "one|two");

        var success = FormVariableValueReader.TryGetValue(variables, values, "tags", out var value);

        Assert.True(success);
        Assert.Equal(FormValueKind.Set, value.Kind);
        Assert.Equal(2, value.Set.Length);
        Assert.Equal("one", value.Set[0]);
        Assert.Equal("two", value.Set[1]);
    }

    [Fact]
    public void IsVariableValueSet_ForSetWithoutItems_ReturnsFalse()
    {
        var variables = CreateVariablesById(("tags", "{\"kind\":\"set\"}"));
        var values = ImmutableDictionary.Create<string, string>(StringComparer.Ordinal)
            .Add("tags", "|");

        var isSet = FormVariableValueReader.IsVariableValueSet(variables, values, "tags");

        Assert.False(isSet);
    }

    private static ImmutableDictionary<VariableId, VariableDefinition> CreateVariablesById(params (string VariableId, string ValueTypeJson)[] items)
    {
        var definitions = items
            .Select(item => new VariableDefinition
            {
                VariableId = VariableId.From(item.VariableId),
                Name = item.VariableId,
                ValueType = JsonDocument.Parse(item.ValueTypeJson).RootElement
            })
            .ToArray();

        return definitions.ToImmutableDictionary(item => VariableId.From(item.VariableId));
    }
}


