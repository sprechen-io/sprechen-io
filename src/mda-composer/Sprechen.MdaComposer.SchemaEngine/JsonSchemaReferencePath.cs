namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Hält Dateiname und optionales Fragment einer Schemareferenz für die getrennte Auflösung beider Teile.
/// </summary>
internal sealed record JsonSchemaReferencePath(string File, string? Fragment);
