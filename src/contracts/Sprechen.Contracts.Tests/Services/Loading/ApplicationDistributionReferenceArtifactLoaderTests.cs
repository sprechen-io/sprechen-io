using System.Text;
using Json.Schema;
using Sprechen.Contracts.Services.Loading;
using Sprechen.Mda.Contracts.V1.RuntimeArtifacts;
using Xunit;

namespace Sprechen.Contracts.Tests.Services.Loading;

/// <summary>
/// Prüft die Schemaprüfung vor dem Zusammenstellen des Katalogs, damit
/// fehlende oder ungültige Anwendungsdistributionen nicht unbemerkt ausgelassen werden.
/// </summary>
public sealed class ApplicationDistributionReferenceArtifactLoaderTests
{
    private const string SampleSimplePath = "apps/sample-simple/application-distribution.reference.json";

    [Fact]
    public void Load_WhenEveryAuthoredDistributionStandsAgainstItsDocument_ReadsThemInOrdinalFolderOrder()
    {
        var distributions = new ApplicationDistributionReferenceArtifactLoader(new MdaArtifactReader())
            .Load(DistributionSchema());

        Assert.Equal(
            ["sample.complex", "sample.simple", "umfrage"],
            distributions.Select(distribution => distribution.DistributionId));
        Assert.All(distributions, distribution => Assert.NotEmpty(distribution.Applications));
    }

    [Theory]
    [InlineData("""{"distributionId":"sample.simple","applications":[]}""", "minItems")]
    [InlineData("""{"applications":[{"applicationId":"sample.simple"}]}""", "distributionId")]
    [InlineData("""{"distributionId":"sample.simple","applications":[{"applicationId":"sample.simple"}],"note":"x"}""", "note")]
    public void Load_WhenADistributionCarriesWhatItsDocumentRefuses_FailsTheLoadNamingTheArtifact(
        string mutated,
        string expectedRefusal)
    {
        var loader = new ApplicationDistributionReferenceArtifactLoader(ReaderWith(SampleSimplePath, mutated));

        var ex = Assert.Throws<InvalidOperationException>(() => loader.Load(DistributionSchema()));

        Assert.Contains($"Schema validation failed ({SampleSimplePath})", ex.Message, StringComparison.Ordinal);
        Assert.Contains(expectedRefusal, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_WhenADistributionDocumentIsMissing_FailsTheLoad()
    {
        var loader = new ApplicationDistributionReferenceArtifactLoader(
            new MdaArtifactReader(
                path => path == SampleSimplePath
                    ? throw new InvalidOperationException($"Embedded runtime artifact '{path}' was not found.")
                    : MdaEmbeddedRuntimeArtifacts.ReadAllBytes(path),
                MdaEmbeddedRuntimeArtifacts.EnumerateJsonFileNames));

        var ex = Assert.Throws<InvalidOperationException>(() => loader.Load(DistributionSchema()));

        Assert.Contains(SampleSimplePath, ex.Message, StringComparison.Ordinal);
    }

    private static JsonSchema DistributionSchema()
    {
        return new MdaSchemaBundleFactory(new MdaArtifactReader()).Create().ApplicationDistributionSchema;
    }

    private static MdaArtifactReader ReaderWith(string path, string content)
    {
        return new MdaArtifactReader(
            requested => requested == path
                ? Encoding.UTF8.GetBytes(content)
                : MdaEmbeddedRuntimeArtifacts.ReadAllBytes(requested),
            MdaEmbeddedRuntimeArtifacts.EnumerateJsonFileNames);
    }
}
