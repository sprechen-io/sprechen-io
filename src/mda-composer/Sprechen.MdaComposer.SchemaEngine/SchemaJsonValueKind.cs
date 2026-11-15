namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Die JSON-Arten des Composers, damit Klassifikation und Anzeige dieselben Namen verwenden.
/// </summary>
internal enum SchemaJsonValueKind
{
    Object,
    Array,
    Boolean,
    Integer,
    Number,
    String,
    Null,
    Unknown
}
