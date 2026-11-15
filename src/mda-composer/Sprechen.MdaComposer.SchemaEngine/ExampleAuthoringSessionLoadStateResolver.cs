namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Bestimmt aus Dokumentquelle und Validierungsergebnis den Ladezustand für die Statusanzeige.
/// </summary>
internal sealed class ExampleAuthoringSessionLoadStateResolver
{
    public ExampleSessionLoadState ResolveUnchecked(ExampleSessionDocumentSource documentSource)
    {
        return documentSource == ExampleSessionDocumentSource.SchemaDefault
            ? ExampleSessionLoadState.SchemaDraft
            : ExampleSessionLoadState.LoadedUnchecked;
    }

    public ExampleSessionLoadState ResolveCanonical(
        ExampleSessionDocumentSource documentSource,
        CanonicalValidationResult canonicalValidation)
    {
        if (documentSource == ExampleSessionDocumentSource.SchemaDefault)
        {
            return ExampleSessionLoadState.SchemaDraft;
        }

        return canonicalValidation.IsValid
            ? ExampleSessionLoadState.LoadedValid
            : ExampleSessionLoadState.LoadedInvalidButParsable;
    }
}
