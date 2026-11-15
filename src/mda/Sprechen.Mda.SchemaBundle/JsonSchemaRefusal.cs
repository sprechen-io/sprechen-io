namespace Sprechen.Mda.SchemaBundle;

/// <summary>
/// Befund einer Schemaprüfung mit Dokumentposition, Schlüsselwort und Meldung.
/// </summary>
public sealed record JsonSchemaRefusal(string InstanceLocation, string Keyword, string Message);
