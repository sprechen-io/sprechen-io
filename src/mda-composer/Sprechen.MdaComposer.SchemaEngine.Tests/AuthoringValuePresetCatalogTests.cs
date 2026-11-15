namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class AuthoringValuePresetCatalogTests
{
    [Fact]
    public void AddPreset_ScopesValuesByRootSchemaAndJsonPointer()
    {
        var catalog = new AuthoringValuePresetCatalog();

        catalog.AddPreset("form-contract.schema.json", "/title", "Titelwerte", ["A", "B"]);
        catalog.AddPreset("form-contract.schema.json", "/description", "Beschreibungen", ["C"]);
        catalog.AddPreset("voice-interface.schema.json", "/title", "Andere Titel", ["D"]);

        var scopedPresets = catalog.GetPresets("form-contract.schema.json", "/title");

        Assert.Single(scopedPresets);
        Assert.Equal(["A", "B"], scopedPresets[0].Values);
    }

    [Fact]
    public void ReplaceAll_ReplacesExistingPresets()
    {
        var catalog = new AuthoringValuePresetCatalog();
        catalog.AddPreset("form-contract.schema.json", "/title", "Titelwerte", ["A"]);

        catalog.ReplaceAll(
        [
            new AuthoringValuePreset("voice-interface.schema.json", "/name", "Namen", ["X", "Y"])
        ]);

        Assert.Empty(catalog.GetPresets("form-contract.schema.json", "/title"));
        Assert.Single(catalog.GetPresets("voice-interface.schema.json", "/name"));
    }
}
