using System.Text.Json;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Liest Beschreibungen, Standardwerte und Einschränkungen aus JSON-Schemas für die weitere Verarbeitung im Composer.
/// </summary>
internal sealed class SchemaElementAnnotationsReader
{
    private readonly SchemaElementDefaultValueReader _defaultValueReader = new();
    private readonly SchemaElementEnumValueReader _enumValueReader = new();
    private readonly SchemaElementPrimitivePropertyReader _primitiveReader = new();

    public SchemaElementAnnotations Read(JsonElement element)
    {
        return new SchemaElementAnnotations
        {
            Title = _primitiveReader.ReadString(element, "title"),
            Description = _primitiveReader.ReadString(element, "description"),
            Pattern = _primitiveReader.ReadString(element, "pattern"),
            Format = _primitiveReader.ReadString(element, "format"),
            ConstValue = _primitiveReader.ReadElementText(element, "const"),
            DefaultValue = _defaultValueReader.Read(element),
            Minimum = _primitiveReader.ReadDouble(element, "minimum"),
            Maximum = _primitiveReader.ReadDouble(element, "maximum"),
            MinLength = _primitiveReader.ReadInt32(element, "minLength"),
            MaxLength = _primitiveReader.ReadInt32(element, "maxLength"),
            MinItems = _primitiveReader.ReadInt32(element, "minItems"),
            MaxItems = _primitiveReader.ReadInt32(element, "maxItems"),
            MinProperties = _primitiveReader.ReadInt32(element, "minProperties"),
            EnumValues = _enumValueReader.Read(element),
            UnevaluatedProperties = _primitiveReader.ReadBoolean(element, "unevaluatedProperties"),
            UniqueItems = _primitiveReader.ReadBoolean(element, "uniqueItems"),
        };
    }
}
