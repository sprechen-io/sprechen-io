using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Mda.Contracts.Tests.Schema;

public sealed class PolitenessLevelEnumAlignmentTests
{
    public static IEnumerable<object[]> AllLevels()
        => Enum.GetValues<PolitenessLevel>().Select(level => new object[] { level });

    [Theory]
    [MemberData(nameof(AllLevels))]
    public void EnumValue_SerializesAsItsWireString_AndRoundTrips(PolitenessLevel level)
    {
        // Der Vertrag verwendet den kleingeschriebenen Enum-Namen als Übertragungswert.
        var wire = level.ToString().ToLowerInvariant();

        // Verwendet den am Enum registrierten JsonConverter ohne zusätzliche Testoptionen.
        Assert.Equal($"\"{wire}\"", JsonSerializer.Serialize(level));
        Assert.Equal(level, JsonSerializer.Deserialize<PolitenessLevel>($"\"{wire}\""));
    }

    [Theory]
    [InlineData("\"Formal\"")]   // Der PascalCase-Name des Enums ist kein zulässiger Übertragungswert.
    [InlineData("\"unknown\"")]
    [InlineData("\"\"")]
    [InlineData("123")]
    [InlineData("true")]
    public void Read_FailsClosed_OnNonStringOrUnknownValue(string json)
    {
        Assert.ThrowsAny<JsonException>(() => JsonSerializer.Deserialize<PolitenessLevel>(json));
    }
}
