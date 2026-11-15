using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Sprechen.Contracts.Services.Loading;
using Xunit;

namespace Sprechen.Contracts.Tests.Services.Loading;

/// <summary>
/// Prüft Reihenfolge und Eindeutigkeit der Anwendungen im zusammengesetzten Katalog sowie die Maskierung von Nicht-ASCII-Zeichen.
/// </summary>
public sealed class FormsCatalogComposerTests
{
    private const string NeutralCatalog = """{"catalogVersion":"1.0.0","applications":[]}""";

    [Fact]
    public void Compose_AppendsEveryDistributionsApplications_InTheGivenOrder()
    {
        var composed = new FormsCatalogComposer().Compose(
            Encoding.UTF8.GetBytes(NeutralCatalog),
            [Distribution("b.dist", "b.one", "b.two"), Distribution("a.dist", "a.one")]);

        Assert.Equal(["b.one", "b.two", "a.one"], ApplicationIds(composed));
    }

    [Fact]
    public void Compose_WhenAnApplicationIdCollidesAcrossDistributions_ThrowsNamingTheDistribution()
    {
        var composer = new FormsCatalogComposer();

        var ex = Assert.Throws<InvalidOperationException>(
            () => composer.Compose(
                Encoding.UTF8.GetBytes(NeutralCatalog),
                [Distribution("first.dist", "shared.app"), Distribution("second.dist", "shared.app")]));

        Assert.Equal(
            "Forms-catalog composition produced a duplicate applicationId 'shared.app' (distribution 'second.dist').",
            ex.Message);
    }

    [Fact]
    public void Compose_WhenTheNeutralCatalogAlreadyDeclaresTheApplicationId_Throws()
    {
        var composer = new FormsCatalogComposer();

        var ex = Assert.Throws<InvalidOperationException>(
            () => composer.Compose(
                Encoding.UTF8.GetBytes("""{"applications":[{"applicationId":"shared.app"}]}"""),
                [Distribution("first.dist", "shared.app")]));

        Assert.Contains("duplicate applicationId 'shared.app'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_WhenTheNeutralCatalogHasNoApplicationsArray_Throws()
    {
        var composer = new FormsCatalogComposer();

        var ex = Assert.Throws<InvalidOperationException>(
            () => composer.Compose(Encoding.UTF8.GetBytes("""{"catalogVersion":"1.0.0"}"""), []));

        Assert.Contains("Missing or non-array 'applications' (neutral forms catalog)", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_KeepsNonAsciiTextEscapedInTheServedBytes()
    {
        var distribution = new ApplicationDistributionFragment(
            "umlaut.dist",
            (JsonArray)JsonNode.Parse("""[{"applicationId":"umlaut.app","displayName":{"de":"Grüße"}}]""")!);

        var composed = new FormsCatalogComposer().Compose(Encoding.UTF8.GetBytes(NeutralCatalog), [distribution]);

        Assert.DoesNotContain("ü", Encoding.UTF8.GetString(composed), StringComparison.Ordinal);
        Assert.Contains("\\u00FC", Encoding.UTF8.GetString(composed), StringComparison.OrdinalIgnoreCase);
    }

    private static ApplicationDistributionFragment Distribution(string distributionId, params string[] applicationIds)
    {
        var applications = new JsonArray();
        foreach (var applicationId in applicationIds)
        {
            applications.Add(new JsonObject { ["applicationId"] = applicationId });
        }

        return new ApplicationDistributionFragment(distributionId, applications);
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
