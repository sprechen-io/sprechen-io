namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Erkennt ausgewählte Längen-, Zahlen-, Muster-, Format- und Arrayvorgaben für deren Anzeige am Schemaknoten.
/// </summary>
public static class SchemaConstraintExtensions
{
    public static bool HasConstraints(this SchemaNode schema) =>
        schema.MinLength is not null || schema.MaxLength is not null
        || schema.Minimum is not null || schema.Maximum is not null
        || schema.Pattern is not null || schema.Format is not null
        || schema.MinItems is not null || schema.MaxItems is not null;
}
