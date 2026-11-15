namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class InMemorySchemaTextProvider : ISchemaTextProvider
{
    private readonly IReadOnlyDictionary<string, string> _schemaTextsByFileName;

    public InMemorySchemaTextProvider(IReadOnlyDictionary<string, string> schemaTextsByFileName)
    {
        _schemaTextsByFileName = schemaTextsByFileName;
    }

    public IReadOnlyDictionary<string, string> GetSchemaTexts() => _schemaTextsByFileName;
}
