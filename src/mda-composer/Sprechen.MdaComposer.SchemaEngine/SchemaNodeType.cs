namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Unterscheidet Schemaknotentypen für die gemeinsame Verarbeitung durch Inspektion und Validierung.
/// </summary>
public enum SchemaNodeType
{
    String,
    Number,
    Integer,
    Boolean,
    Object,
    Array,
    Null,
    OneOf,
    AnyOf,
    AllOf,
    Unknown
}
