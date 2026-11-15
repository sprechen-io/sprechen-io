using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Variables;

namespace Sprechen.Mda.Contracts.Tests;

public sealed class VariableDefinitionsPreflightTests
{
    [Theory]
    [InlineData("""{"kind":"decimal","precision":5,"scale":2}""")]
    [InlineData("""{"kind":"dateTime"}""")]
    [InlineData("""{"kind":"object","properties":{}}""")]
    [InlineData("""{"kind":"list","itemType":{"kind":"string"}}""")]
    [InlineData("""{"kind":"custom","typeName":"domain-type"}""")]
    public void Validate_WhenValueTypeUsesPf09Extension_ReturnsFeatureNotSupported(string valueTypeJson)
    {
        var catalog = new VariableDefinitions
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            VariableDefinitionsId = "sample.shared.variables",
            SupportedLanguages = ["de"],
            StrictLocalization = true,
            Definitions =
            [
                new ReusableVariableDefinition
                {
                    VariableDefinitionId = "sampleVariable",
                    Name = "sampleVariable",
                    ValueType = JsonDocument.Parse(valueTypeJson).RootElement,
                    Description = new LocalizedText { ["de"] = "Beschreibung" }
                }
            ]
        };

        var result = VariableDefinitionsPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Contains(
            result.Errors,
            issue => issue.Code == PreflightCodes.ErrorFeatureNotSupported
                && string.Equals(issue.Path, "$.definitions[0].valueType", StringComparison.Ordinal));
    }
}
