using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Mda.Contracts.Tests;

public sealed class ConfirmationPolicyJsonConverterTests
{
    [Theory]
    [InlineData(ConfirmationPolicy.Always, "\"always\"")]
    [InlineData(ConfirmationPolicy.Ambiguous, "\"ambiguous\"")]
    [InlineData(ConfirmationPolicy.Never, "\"never\"")]
    public void Serialize_WritesExpectedWireValue(ConfirmationPolicy value, string expectedJson)
    {
        var json = JsonSerializer.Serialize(value);

        Assert.Equal(expectedJson, json);
    }

    [Theory]
    [InlineData("\"always\"", ConfirmationPolicy.Always)]
    [InlineData("\"ambiguous\"", ConfirmationPolicy.Ambiguous)]
    [InlineData("\"never\"", ConfirmationPolicy.Never)]
    public void Deserialize_ReadsExpectedWireValue(string json, ConfirmationPolicy expected)
    {
        var value = JsonSerializer.Deserialize<ConfirmationPolicy>(json);

        Assert.Equal(expected, value);
    }
}

