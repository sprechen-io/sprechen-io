namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class ExampleAuthoringSessionLoadStateResolverTests
{
    [Fact]
    public void ResolveUnchecked_ForSchemaDefault_ReturnsSchemaDraft()
    {
        var resolver = new ExampleAuthoringSessionLoadStateResolver();

        var loadState = resolver.ResolveUnchecked(ExampleSessionDocumentSource.SchemaDefault);

        Assert.Equal(ExampleSessionLoadState.SchemaDraft, loadState);
    }

    [Fact]
    public void ResolveUnchecked_ForImportedDocument_ReturnsLoadedUnchecked()
    {
        var resolver = new ExampleAuthoringSessionLoadStateResolver();

        var loadState = resolver.ResolveUnchecked(ExampleSessionDocumentSource.JsonImport);

        Assert.Equal(ExampleSessionLoadState.LoadedUnchecked, loadState);
    }

    [Theory]
    [InlineData(true, ExampleSessionLoadState.LoadedValid)]
    [InlineData(false, ExampleSessionLoadState.LoadedInvalidButParsable)]
    public void ResolveCanonical_ForImportedDocument_FollowsValidationResult(
        bool isValid,
        ExampleSessionLoadState expectedLoadState)
    {
        var resolver = new ExampleAuthoringSessionLoadStateResolver();

        var loadState = resolver.ResolveCanonical(
            ExampleSessionDocumentSource.JsonImport,
            new CanonicalValidationResult(isValid, [], "summary"));

        Assert.Equal(expectedLoadState, loadState);
    }
}
