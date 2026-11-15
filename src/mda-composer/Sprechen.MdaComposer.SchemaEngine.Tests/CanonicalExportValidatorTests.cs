namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class CanonicalExportValidatorTests
{
    [Fact]
    public void Validate_Rejects_Instance_ThatViolates_RootRequiredProperty()
    {
        var validator = new CanonicalExportValidator();
        var schemaTexts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["sample.schema.json"] =
                """
                {
                  "$schema": "https://json-schema.org/draft/2020-12/schema",
                  "$id": "https://Sprechen/schemas/sample.schema.json",
                  "type": "object",
                  "required": ["title"],
                  "properties": {
                    "title": { "type": "string" }
                  }
                }
                """
        };

        var result = validator.Validate("sample.schema.json", "{}", schemaTexts);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Issues);
    }

    [Fact]
    public void Validate_WhenSchemaContentChangesWithSameLength_RebuildsSchemaBundle()
    {
        var validator = new CanonicalExportValidator();
        var stringSchema = CreateSingleTypeSchema("string");
        var numberSchema = CreateSingleTypeSchema("number");
        Assert.Equal(stringSchema.Length, numberSchema.Length);

        var firstResult = validator.Validate(
            "sample.schema.json",
            "\"value\"",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["sample.schema.json"] = stringSchema,
            });
        var secondResult = validator.Validate(
            "sample.schema.json",
            "\"value\"",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["sample.schema.json"] = numberSchema,
            });

        Assert.True(firstResult.IsValid);
        Assert.False(secondResult.IsValid);
    }

    private static string CreateSingleTypeSchema(string type)
    {
        return $$"""
        {
          "$schema": "https://json-schema.org/draft/2020-12/schema",
          "$id": "https://Sprechen/schemas/sample.schema.json",
          "type": "{{type}}"
        }
        """;
    }
}
