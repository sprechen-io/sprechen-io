using Sprechen.Mda.SchemaBundle;

namespace Sprechen.Mda.Contracts.Tests.Schema;

/// <summary>
/// Prüft die gemeinsame Zuordnung von Artefaktdateinamen zu Schema-Dateinamen
/// für Composer und Artefaktprüfungen, einschließlich Varianten und Mehrdeutigkeit.
/// </summary>
public sealed class MdaArtifactSchemaFileNameResolverTests
{
    private static readonly string[] Documents =
    [
        "form-contract.schema.json",
        "forms-catalog.schema.json",
        "wake-word-catalog.schema.json",
        "wake-word-detector-catalog.schema.json"
    ];

    [Theory]
    [InlineData("form-contract.reference.json", "form-contract.schema.json")]
    [InlineData("form-contract.kontaktaenderung.reference.json", "form-contract.schema.json")]
    [InlineData("form-contract.voice-complex.example.json", "form-contract.schema.json")]
    [InlineData("form-contract", "form-contract.schema.json")]
    public void ArtifactNamedAfterADocument_NamesThatDocument(string artifactFileName, string expected)
    {
        var named = new MdaArtifactSchemaFileNameResolver().Resolve(artifactFileName, Documents);

        Assert.Equal(expected, named);
    }

    [Fact]
    public void DocumentWhoseNameIsContinuedWithoutADot_DoesNotAnswerForThatArtifact()
    {
        var named = new MdaArtifactSchemaFileNameResolver()
            .Resolve("wake-word-detector-catalog.reference.json", Documents);

        Assert.Equal("wake-word-detector-catalog.schema.json", named);
    }

    [Fact]
    public void ArtifactTwoDocumentsBothName_IsRefused()
    {
        string[] documents = ["form-contract.schema.json", "form-contract.study.schema.json"];

        var refusal = Assert.Throws<InvalidOperationException>(
            () => new MdaArtifactSchemaFileNameResolver().Resolve("form-contract.study.reference.json", documents));

        Assert.Contains("decided nowhere", refusal.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("form-contracts.reference.json")]
    [InlineData("telemetry-events.reference.json")]
    public void ArtifactThatOnlyResemblesADocumentName_NamesNone(string artifactFileName)
    {
        var named = new MdaArtifactSchemaFileNameResolver().Resolve(artifactFileName, Documents);

        Assert.Null(named);
    }
}
