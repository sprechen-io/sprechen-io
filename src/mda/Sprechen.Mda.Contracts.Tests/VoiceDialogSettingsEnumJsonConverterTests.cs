using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Mda.Contracts.Tests;

public sealed class VoiceDialogSettingsEnumJsonConverterTests
{
    [Theory]
    [InlineData(PolitenessLevel.Formal, "\"formal\"")]
    [InlineData(PolitenessLevel.Informal, "\"informal\"")]
    public void PolitenessLevel_Serialize_WritesExpectedWireValue(PolitenessLevel value, string expectedJson)
    {
        var json = JsonSerializer.Serialize(value);

        Assert.Equal(expectedJson, json);
    }

    [Theory]
    [InlineData("\"formal\"", PolitenessLevel.Formal)]
    [InlineData("\"informal\"", PolitenessLevel.Informal)]
    public void PolitenessLevel_Deserialize_ReadsExpectedWireValue(string json, PolitenessLevel expected)
    {
        var value = JsonSerializer.Deserialize<PolitenessLevel>(json);

        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData(ProgressCalculationBasis.AllFields, "\"allFields\"")]
    [InlineData(ProgressCalculationBasis.RequiredFieldsOnly, "\"requiredFieldsOnly\"")]
    [InlineData(ProgressCalculationBasis.FieldGroupBased, "\"fieldGroupBased\"")]
    public void ProgressCalculationBasis_Serialize_WritesExpectedWireValue(ProgressCalculationBasis value, string expectedJson)
    {
        var json = JsonSerializer.Serialize(value);

        Assert.Equal(expectedJson, json);
    }

    [Theory]
    [InlineData("\"allFields\"", ProgressCalculationBasis.AllFields)]
    [InlineData("\"requiredFieldsOnly\"", ProgressCalculationBasis.RequiredFieldsOnly)]
    [InlineData("\"fieldGroupBased\"", ProgressCalculationBasis.FieldGroupBased)]
    public void ProgressCalculationBasis_Deserialize_ReadsExpectedWireValue(string json, ProgressCalculationBasis expected)
    {
        var value = JsonSerializer.Deserialize<ProgressCalculationBasis>(json);

        Assert.Equal(expected, value);
    }
}
