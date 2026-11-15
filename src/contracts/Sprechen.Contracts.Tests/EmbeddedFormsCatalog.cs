using Sprechen.Contracts.Services.Loading;

namespace Sprechen.Contracts.Tests;

/// <summary>
/// Erzeugt Testkataloge mit dem regulären Ladeverfahren, damit die Tests
/// dieselben Artefakte wie der Vertragsdienst verwenden.
/// </summary>
internal static class EmbeddedFormsCatalog
{
    private static readonly Lazy<byte[]> Composed = new(Compose);

    public static byte[] ComposedBytes()
    {
        return Composed.Value;
    }

    private static byte[] Compose()
    {
        var artifactReader = new MdaArtifactReader();
        var schemas = new MdaSchemaBundleFactory(artifactReader).Create();

        return new FormsCatalogReferenceArtifactLoader(artifactReader, new MdaReferenceJsonModelReader())
            .Load(schemas.FormsCatalogSchema, schemas.ApplicationDistributionSchema)
            .Artifact
            .Content;
    }
}
