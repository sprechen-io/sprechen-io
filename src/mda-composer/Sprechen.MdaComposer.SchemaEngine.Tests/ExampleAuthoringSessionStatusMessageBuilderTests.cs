namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class ExampleAuthoringSessionStatusMessageBuilderTests
{
    [Fact]
    public void Build_WhenCanonicalValidationIsUnavailable_IncludesRepairAvailability()
    {
        var builder = new ExampleAuthoringSessionStatusMessageBuilder();

        var message = builder.Build(
            ExampleSessionDocumentSource.JsonImport,
            canonicalValidation: null,
            [CreateRepairAction()],
            canonicalValidationAvailable: false);

        Assert.Equal(
            "Dokument geladen, kanonische Prüfung noch nicht verfügbar. 1 Reparaturaktion verfügbar.",
            message);
    }

    [Fact]
    public void Build_WhenLoadedDocumentIsValid_ReturnsValidImportMessage()
    {
        var builder = new ExampleAuthoringSessionStatusMessageBuilder();

        var message = builder.Build(
            ExampleSessionDocumentSource.JsonImport,
            new CanonicalValidationResult(IsValid: true, Issues: [], Summary: "ok"),
            [],
            canonicalValidationAvailable: true);

        Assert.Equal("Geladenes Dokument ist gegen das Basisschema valide.", message);
    }

    [Fact]
    public void Build_WhenSchemaDraftIsInvalid_IncludesRepairAvailability()
    {
        var builder = new ExampleAuthoringSessionStatusMessageBuilder();

        var message = builder.Build(
            ExampleSessionDocumentSource.SchemaDefault,
            new CanonicalValidationResult(IsValid: false, Issues: [], Summary: "invalid"),
            [CreateRepairAction(), CreateRepairAction()],
            canonicalValidationAvailable: true);

        Assert.Equal(
            "Entwurf aus dem Basisschema ist noch nicht exportierbar. 2 Reparaturaktionen verfügbar.",
            message);
    }

    private static ExampleRepairAction CreateRepairAction()
    {
        return new ExampleRepairAction(
            "repair",
            ExampleRepairActionKind.InitializeMissingRequiredValue,
            "/title",
            "Repair title",
            "Repair description",
            new SchemaNode
            {
                Name = "title",
                Type = SchemaNodeType.String,
            });
    }
}
