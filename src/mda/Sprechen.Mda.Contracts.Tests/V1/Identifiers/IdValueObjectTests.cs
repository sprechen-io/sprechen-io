using System.Collections.Generic;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Xunit;

namespace Sprechen.Mda.Contracts.Tests.V1.Identifiers;

/// <summary>
/// Prüft die typisierten Kennungen für Felder, Variablen, Feldgruppen, Befehle und Formulare:
/// Wertegleichheit, ungültige Leerwerte sowie JSON-Darstellung als Zeichenkette.
/// Für FieldId wird zusätzlich die implizite Umwandlung in string geprüft.
/// </summary>
public sealed class IdValueObjectTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void From_RejectsNullOrWhitespace(string? raw)
    {
        Assert.ThrowsAny<System.ArgumentException>(() => FieldId.From(raw!));
        Assert.ThrowsAny<System.ArgumentException>(() => VariableId.From(raw!));
        Assert.ThrowsAny<System.ArgumentException>(() => FieldGroupId.From(raw!));
        Assert.ThrowsAny<System.ArgumentException>(() => CommandId.From(raw!));
        Assert.ThrowsAny<System.ArgumentException>(() => FormId.From(raw!));
    }

    [Fact]
    public void From_PreservesValue_AndImplicitToStringReturnsIt()
    {
        var field = FieldId.From("firstName");

        Assert.Equal("firstName", field.Value);
        Assert.Equal("firstName", field.ToString());

        // Die implizite Umwandlung erlaubt FieldId auch als Schlüssel eines String-Dictionarys.
        string asString = field;
        Assert.Equal("firstName", asString);
        var byKey = new Dictionary<string, int>(System.StringComparer.Ordinal) { ["firstName"] = 7 };
        Assert.Equal(7, byKey[field]);
    }

    [Fact]
    public void Equality_IsByValue()
    {
        Assert.Equal(FieldId.From("a"), FieldId.From("a"));
        Assert.NotEqual(FieldId.From("a"), FieldId.From("b"));
        Assert.Equal(VariableId.From("v"), VariableId.From("v"));
        Assert.Equal(FieldGroupId.From("g"), FieldGroupId.From("g"));
        Assert.NotEqual(FieldGroupId.From("g"), FieldGroupId.From("h"));
        Assert.Equal(CommandId.From("next"), CommandId.From("next"));
        Assert.NotEqual(CommandId.From("next"), CommandId.From("back"));
        Assert.Equal(FormId.From("form"), FormId.From("form"));
        Assert.NotEqual(FormId.From("form"), FormId.From("other"));
    }

    [Fact]
    public void Default_Value_Throws()
    {
        Assert.Throws<System.InvalidOperationException>(() => default(FieldId).Value);
        Assert.Throws<System.InvalidOperationException>(() => default(VariableId).Value);
        Assert.Throws<System.InvalidOperationException>(() => default(FieldGroupId).Value);
        Assert.Throws<System.InvalidOperationException>(() => default(CommandId).Value);
        Assert.Throws<System.InvalidOperationException>(() => default(FormId).Value);
    }

    [Fact]
    public void Json_RoundTripsAsBareString()
    {
        Assert.Equal("\"firstName\"", JsonSerializer.Serialize(FieldId.From("firstName")));
        Assert.Equal(FieldId.From("firstName"), JsonSerializer.Deserialize<FieldId>("\"firstName\""));

        Assert.Equal("\"givenName\"", JsonSerializer.Serialize(VariableId.From("givenName")));
        Assert.Equal(VariableId.From("givenName"), JsonSerializer.Deserialize<VariableId>("\"givenName\""));

        Assert.Equal("\"identity\"", JsonSerializer.Serialize(FieldGroupId.From("identity")));
        Assert.Equal(FieldGroupId.From("identity"), JsonSerializer.Deserialize<FieldGroupId>("\"identity\""));

        Assert.Equal("\"next\"", JsonSerializer.Serialize(CommandId.From("next")));
        Assert.Equal(CommandId.From("next"), JsonSerializer.Deserialize<CommandId>("\"next\""));

        Assert.Equal("\"contact\"", JsonSerializer.Serialize(FormId.From("contact")));
        Assert.Equal(FormId.From("contact"), JsonSerializer.Deserialize<FormId>("\"contact\""));
    }

    [Fact]
    public void Json_InsideObject_SerializesIdsAsBareStrings()
    {
        var payload = new IdCarrier(
            FieldId.From("firstName"),
            VariableId.From("givenName"),
            FieldGroupId.From("identity"),
            CommandId.From("next"),
            FormId.From("contact"));

        var json = JsonSerializer.Serialize(payload);

        Assert.Equal(
            "{\"FieldId\":\"firstName\",\"VariableId\":\"givenName\",\"FieldGroupId\":\"identity\",\"CommandId\":\"next\",\"FormId\":\"contact\"}",
            json);

        var round = JsonSerializer.Deserialize<IdCarrier>(json);
        Assert.Equal(payload, round);
    }

    [Fact]
    public void Json_Read_RejectsNonStringAndEmpty()
    {
        Assert.ThrowsAny<JsonException>(() => JsonSerializer.Deserialize<FieldId>("123"));
        Assert.ThrowsAny<JsonException>(() => JsonSerializer.Deserialize<FieldId>("\"\""));
        Assert.ThrowsAny<JsonException>(() => JsonSerializer.Deserialize<FieldGroupId>("123"));
        Assert.ThrowsAny<JsonException>(() => JsonSerializer.Deserialize<FieldGroupId>("\"\""));
        Assert.ThrowsAny<JsonException>(() => JsonSerializer.Deserialize<CommandId>("123"));
        Assert.ThrowsAny<JsonException>(() => JsonSerializer.Deserialize<CommandId>("\"\""));
        Assert.ThrowsAny<JsonException>(() => JsonSerializer.Deserialize<FormId>("123"));
        Assert.ThrowsAny<JsonException>(() => JsonSerializer.Deserialize<FormId>("\"\""));
    }
}
