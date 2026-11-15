namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Bestimmt, welche Eigenschaften wegen Pflichtstatus, konstantem Wert oder Standardwert initialisiert werden.
/// </summary>
internal sealed class SchemaNodeDefaultPropertySelector
{
    public bool ShouldInitialize(SchemaNode property)
    {
        return property.IsRequired || property.ConstValue is not null || property.DefaultValue is not null;
    }
}
