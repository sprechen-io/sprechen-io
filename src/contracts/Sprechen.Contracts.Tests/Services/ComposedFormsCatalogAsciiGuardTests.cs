using System.Text.Json;
using Sprechen.Contracts.Services;
using Sprechen.Contracts.Services.Loading;
using Sprechen.Mda.Contracts.V1.Catalog;
using Xunit;

namespace Sprechen.Contracts.Tests.Services;

/// <summary>
/// Prüft die ASCII-Ausgabe und die enthaltenen Beispielanwendungen des
/// zusammengesetzten Katalogs, damit Kodierung und Zusammenstellung erhalten bleiben.
/// </summary>
public sealed class ComposedFormsCatalogAsciiGuardTests
{
    [Fact]
    public void ServedFormsCatalog_ContainsOnlyAsciiBytes()
    {
        var store = CreateStore();

        var content = store.FormsCatalog.Content;

        var firstNonAsciiOffset = Array.FindIndex(content, b => b > 0x7F);
        Assert.True(
            firstNonAsciiOffset < 0,
            $"Served forms catalog contains a non-ASCII byte at offset {firstNonAsciiOffset}.");
    }

    [Fact]
    public void ServedFormsCatalog_ComposesTheSampleDistributions()
    {
        var store = CreateStore();

        var catalog = JsonSerializer.Deserialize<FormsCatalogDocument>(
            store.FormsCatalog.Content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(catalog);
        Assert.Equal("5", catalog.SelectionDialog.ModelVersion);
        var simple = Assert.Single(catalog!.Applications, app => app.ApplicationId == "sample.simple");
        Assert.Equal("two-groups", Assert.Single(simple.Forms).FormId);
        var complex = Assert.Single(catalog.Applications, app => app.ApplicationId == "sample.complex");
        Assert.Equal("full-coverage", Assert.Single(complex.Forms).FormId);
    }

    private static MdaContractsStore CreateStore()
    {
        var artifactReader = new MdaArtifactReader();
        var schemas = new MdaSchemaBundleFactory(artifactReader).Create();
        var references = new MdaReferenceArtifactLoader(artifactReader).Load(schemas);
        var bundle = new MdaContractsBundleConsistencyValidator().Validate(schemas, references);

        return new MdaContractsStore(bundle);
    }
}
