using System.Text.Json;
using Sprechen.Mda.Contracts.V1.RuntimeArtifacts;

namespace Sprechen.Mda.Contracts.Tests;

/// <summary>
/// Prüft die Auflistung eingebetteter Anwendungsartefakte, aus denen der Laufzeitkatalog
/// zusammengesetzt wird: apps-Dateien unter dem logischen Präfix
/// <c>RuntimeArtifacts.apps.&lt;dir&gt;.*</c> müssen als Unterverzeichnisse und JSON-Dateien auffindbar sein.
/// </summary>
public sealed class MdaEmbeddedRuntimeArtifactsAppsResolutionTests
{
    [Fact]
    public void EnumerateSubdirectories_Apps_ReturnsEveryDistributionFolderOrdinal()
    {
        var distributions = MdaEmbeddedRuntimeArtifacts.EnumerateSubdirectories("apps");

        Assert.Equal(
            new[]
            {
                "sample-complex",
                "sample-simple",
                "umfrage"
            },
            distributions);
    }

    [Fact]
    public void EnumerateJsonFileNames_AppsCategoryDirectory_ResolvesEmbeddedFiles()
    {
        var formContracts = MdaEmbeddedRuntimeArtifacts.EnumerateJsonFileNames("apps/sample-simple/form-contracts");
        var fragment = MdaEmbeddedRuntimeArtifacts.ReadAllBytes("apps/sample-simple/application-distribution.reference.json");

        Assert.Equal(new[] { "form-contract.simple-two-groups.reference.json" }, formContracts);
        Assert.NotEmpty(fragment);
    }

    [Fact]
    public void SpellingFirstField_KeepsItsAuthoredInputModeOrder_ThroughTheEmbeddedPath()
    {
        // Die Reihenfolge bestimmt den anfänglichen Eingabemodus. Der Test liest
        // deshalb die tatsächlich eingebettete Felddefinition statt eines im Test
        // aufgebauten Vertrags und erkennt so abweichende Artefaktwerte.
        var bytes = MdaEmbeddedRuntimeArtifacts.ReadAllBytes(
            "apps/sample-complex/field-definitions/field-definitions.complex-multilingual.reference.json");
        using var document = JsonDocument.Parse(bytes);
        var ibanField = document.RootElement.GetProperty("definitions").EnumerateArray().Single(
            definition => definition.GetProperty("fieldDefinitionId").GetString()
                == "sample.complex.full-coverage.ibanField");

        Assert.Equal(
            new[] { "spelling", "keyboard", "voice" },
            ibanField.GetProperty("preferredInputModes").EnumerateArray().Select(mode => mode.GetString()));
    }
}
