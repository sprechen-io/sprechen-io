namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Stellt Schemainhalte nach Dateinamen bereit, damit die Validierung unabhängig von ihrer Ladequelle arbeitet.
/// </summary>
public interface ISchemaTextProvider
{
    IReadOnlyDictionary<string, string> GetSchemaTexts();
}
