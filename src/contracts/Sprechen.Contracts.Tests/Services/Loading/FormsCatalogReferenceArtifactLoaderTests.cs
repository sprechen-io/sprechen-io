using System.Text;
using System.Text.Json;
using Sprechen.Contracts.Services.Loading;
using Sprechen.Mda.Contracts.V1.RuntimeArtifacts;
using Xunit;

namespace Sprechen.Contracts.Tests.Services.Loading;

/// <summary>
/// Prüft, dass der Katalog alle Anwendungen der Distributionen enthält und ungültige Distributionen den Ladevorgang abbrechen.
/// </summary>
public sealed class FormsCatalogReferenceArtifactLoaderTests
{
    private const string SampleSimplePath = "apps/sample-simple/application-distribution.reference.json";

    [Fact]
    public void Load_WhenEveryDistributionStandsAgainstItsDocument_ServesEveryDeclaredApplication()
    {
        var schemas = new MdaSchemaBundleFactory(new MdaArtifactReader()).Create();

        var loaded = new FormsCatalogReferenceArtifactLoader(new MdaArtifactReader(), new MdaReferenceJsonModelReader())
            .Load(schemas.FormsCatalogSchema, schemas.ApplicationDistributionSchema);

        Assert.Equal(["sample.complex", "sample.simple", "study.umfrage"], ApplicationIds(loaded.Artifact.Content));
    }

    [Fact]
    public void Load_WhenADistributionDeclaresNoApplication_FailsInsteadOfDroppingIt()
    {
        var schemas = new MdaSchemaBundleFactory(new MdaArtifactReader()).Create();
        var reader = new MdaArtifactReader(
            path => path == SampleSimplePath
                ? Encoding.UTF8.GetBytes("""{"distributionId":"sample.simple","applications":[]}""")
                : MdaEmbeddedRuntimeArtifacts.ReadAllBytes(path),
            MdaEmbeddedRuntimeArtifacts.EnumerateJsonFileNames);

        var ex = Assert.Throws<InvalidOperationException>(
            () => new FormsCatalogReferenceArtifactLoader(reader, new MdaReferenceJsonModelReader())
                .Load(schemas.FormsCatalogSchema, schemas.ApplicationDistributionSchema));

        Assert.Contains($"Schema validation failed ({SampleSimplePath})", ex.Message, StringComparison.Ordinal);
    }

    private static string?[] ApplicationIds(byte[] composedCatalog)
    {
        using var document = JsonDocument.Parse(composedCatalog);
        return document.RootElement.GetProperty("applications")
            .EnumerateArray()
            .Select(application => application.GetProperty("applicationId").GetString())
            .ToArray();
    }
}
