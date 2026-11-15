using System.Text;
using Json.Schema;
using Sprechen.Contracts.Services.Loading;
using Sprechen.Mda.Contracts.V1.RuntimeArtifacts;
using Xunit;

namespace Sprechen.Contracts.Tests.Services.Loading;

public sealed class MdaReferenceArtifactLoaderTests
{
    [Fact]
    public void Load_WhenEmbeddedReferenceArtifactsArePresent_LoadsTypedArtifactFamilies()
    {
        var schemas = new MdaSchemaBundleFactory(new MdaArtifactReader()).Create();

        var references = new MdaReferenceArtifactLoader(new MdaArtifactReader()).Load(schemas);

        Assert.NotEmpty(references.FieldDefinitions.Artifacts);
        Assert.NotEmpty(references.VariableDefinitions.Artifacts);
        // Kein eingebettetes Formular referenziert ein eigenes Dialogprofil; deshalb ist der Profilbestand leer.
        Assert.Empty(references.FormVoiceProfiles.Artifacts);
        Assert.NotEmpty(references.SelectionDialogs.Artifacts);
        Assert.NotEmpty(references.SelectionDialogProfiles.Artifacts);
        Assert.NotEmpty(references.FormContracts.Artifacts);
        Assert.Equal("sample.application-selection-dialog", references.FormsCatalogDocument.SelectionDialog.SelectionDialogId);
    }

    [Fact]
    public void Load_WhenReferenceArtifactDirectoryIsEmpty_Throws()
    {
        var schemas = new MdaSchemaBundleFactory(new MdaArtifactReader()).Create();
        var reader = new MdaArtifactReader(
            MdaEmbeddedRuntimeArtifacts.ReadAllBytes,
            directoryPath => directoryPath.Contains("field-definitions", StringComparison.Ordinal)
                ? []
                : MdaEmbeddedRuntimeArtifacts.EnumerateJsonFileNames(directoryPath));

        var ex = Assert.Throws<InvalidOperationException>(
            () => new MdaReferenceArtifactLoader(reader).Load(schemas));

        Assert.Contains("does not contain JSON artifacts", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_WhenReferenceArtifactJsonIsMalformed_Throws()
    {
        var schemas = new MdaSchemaBundleFactory(new MdaArtifactReader()).Create();
        var reader = new MdaArtifactReader(
            path => path == $"{MdaContractsArtifactPaths.FormContractsDirectoryPath}/broken.reference.json"
                ? Encoding.UTF8.GetBytes("{not-json")
                : MdaEmbeddedRuntimeArtifacts.ReadAllBytes(path),
            directoryPath => directoryPath == MdaContractsArtifactPaths.FormContractsDirectoryPath
                ? ["broken.reference.json"]
                : MdaEmbeddedRuntimeArtifacts.EnumerateJsonFileNames(directoryPath));

        var ex = Assert.Throws<InvalidOperationException>(
            () => new MdaReferenceArtifactLoader(reader).Load(schemas));

        Assert.Contains("Invalid JSON artifact", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ArtifactSetLoader_WhenDuplicateKeysAreRead_Throws()
    {
        var reader = new MdaArtifactReader(
            _ => Encoding.UTF8.GetBytes("{}"),
            directoryPath => directoryPath == "refs" ? ["one.reference.json", "two.reference.json"] : []);
        var setLoader = new MdaReferenceArtifactSetLoader(reader, new MdaReferenceJsonModelReader());

        var ex = Assert.Throws<InvalidOperationException>(
            () => setLoader.Load<string, Dictionary<string, string>>(
                "refs",
                JsonSchema.FromText("""{"type":"object"}"""),
                _ => "same",
                key => $"Duplicate key '{key}'."));

        Assert.Contains("Duplicate key 'same'.", ex.Message, StringComparison.Ordinal);
    }
}
