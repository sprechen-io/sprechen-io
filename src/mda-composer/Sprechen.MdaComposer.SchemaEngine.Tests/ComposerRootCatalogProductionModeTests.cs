using Sprechen.MdaComposer.SchemaEngine;

namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class ComposerRootCatalogProductionModeTests
{
    [Fact]
    public void GetForOptions_null_returns_full_catalog()
    {
        var schemas = ComposerRootCatalog.GetForOptions(options: null);

        Assert.Equal(ComposerRootCatalog.All.Count, schemas.Count);
    }

    [Fact]
    public void GetForOptions_default_StudyMode_returns_full_catalog()
    {
        var options = new ComposerOptions();

        var schemas = ComposerRootCatalog.GetForOptions(options);

        Assert.Equal(ComposerRootCatalog.All.Count, schemas.Count);
    }

    [Fact]
    public void GetForOptions_ProductionMode_excludes_Evaluationsmodell_area()
    {
        var options = new ComposerOptions { IncludeEvaluationSchemas = false };

        var schemas = ComposerRootCatalog.GetForOptions(options);

        Assert.DoesNotContain(
            schemas,
            schema => schema.ModelArea.Equals("Evaluationsmodell", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(schemas, schema => schema.FileName == "form-contract.schema.json");
        Assert.DoesNotContain(schemas, schema => schema.FileName == "telemetry-events.schema.json");
        Assert.DoesNotContain(schemas, schema => schema.FileName == "telemetry-fragments.schema.json");
        Assert.DoesNotContain(schemas, schema => schema.FileName == "canonical-transition-projection.schema.json");
        Assert.DoesNotContain(schemas, schema => schema.FileName == "task-end-projection.schema.json");
    }

    [Fact]
    public void FindByFileName_ProductionMode_does_not_resolve_telemetry_schemas()
    {
        var options = new ComposerOptions { IncludeEvaluationSchemas = false };

        Assert.Null(ComposerRootCatalog.FindByFileName("telemetry-events.schema.json", options));
        Assert.Null(ComposerRootCatalog.FindByFileName("telemetry-fragments.schema.json", options));
        Assert.Null(ComposerRootCatalog.FindByFileName("canonical-transition-projection.schema.json", options));
        Assert.Null(ComposerRootCatalog.FindByFileName("task-end-projection.schema.json", options));
        Assert.NotNull(ComposerRootCatalog.FindByFileName("form-contract.schema.json", options));
    }

    [Fact]
    public void MatchReferenceContract_ProductionMode_does_not_resolve_telemetry_refs()
    {
        var options = new ComposerOptions { IncludeEvaluationSchemas = false };

        Assert.Null(ComposerRootCatalog.MatchReferenceContract("telemetry-events.reference.json", options));
        Assert.Null(ComposerRootCatalog.MatchReferenceContract("telemetry-fragments.reference.json", options));
        Assert.Null(ComposerRootCatalog.MatchReferenceContract("canonical-transition-projection.reference.json", options));
        Assert.Null(ComposerRootCatalog.MatchReferenceContract("task-end-projection.reference.json", options));
    }

    [Fact]
    public void FindByFileName_StudyMode_still_resolves_telemetry_schemas()
    {
        var options = new ComposerOptions { IncludeEvaluationSchemas = true };

        Assert.NotNull(ComposerRootCatalog.FindByFileName("telemetry-events.schema.json", options));
        Assert.NotNull(ComposerRootCatalog.FindByFileName("telemetry-fragments.schema.json", options));
        Assert.NotNull(ComposerRootCatalog.FindByFileName("canonical-transition-projection.schema.json", options));
        Assert.NotNull(ComposerRootCatalog.FindByFileName("task-end-projection.schema.json", options));
    }
}
