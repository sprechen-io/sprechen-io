namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class JsonSchemaExternalReferenceCollectorTests
{
    [Fact]
    public void Collect_ReturnsExternalSchemaFileNames_FromRelativeAndAbsoluteRefs()
    {
        var collector = new Sprechen.Mda.SchemaBundle.JsonSchemaReferenceFileNameCollector();

        var references = collector.Collect(
            """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$defs": {
                "local": { "type": "string" }
              },
              "properties": {
                "shared": { "$ref": "../shared/shared.schema.json#/$defs/name" },
                "remote": { "$ref": "https://example.test/schemas/remote.schema.json#/$defs/name" },
                "local": { "$ref": "#/$defs/local" },
                "ignored": { "$ref": "../shared/not-a-schema.json" }
              },
              "items": [
                { "$ref": "nested.schema.json" }
              ]
            }
            """);

        Assert.Equal(3, references.Count);
        Assert.Contains("shared.schema.json", references);
        Assert.Contains("remote.schema.json", references);
        Assert.Contains("nested.schema.json", references);
    }
}
