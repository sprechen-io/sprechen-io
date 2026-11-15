namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Beschreibt einen Schemaknoten mit Typ, Eigenschaften und Einschränkungen als gemeinsame Struktur für Inspektion und Editor.
/// </summary>
public sealed record SchemaNode
{
    public required string Name { get; init; }
    public required SchemaNodeType Type { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public bool IsRequired { get; init; }
    public IReadOnlyList<string>? EnumValues { get; init; }
    public IReadOnlyList<SchemaNode>? Properties { get; init; }
    public SchemaNode? Items { get; init; }
    public SchemaNode? AdditionalProperties { get; init; }
    public object? DefaultValue { get; init; }
    public double? Minimum { get; init; }
    public double? Maximum { get; init; }
    public int? MinLength { get; init; }
    public int? MaxLength { get; init; }
    public string? Pattern { get; init; }
    public string? ConstValue { get; init; }
    public string? SchemaId { get; init; }
    public string? Format { get; init; }
    public int? MinItems { get; init; }
    public int? MaxItems { get; init; }
    public int? MinProperties { get; init; }
    public IReadOnlyList<SchemaNode>? OneOf { get; init; }
    public IReadOnlyList<SchemaNode>? AnyOf { get; init; }
    public IReadOnlyList<SchemaNode>? AllOf { get; init; }
    public SchemaNode? If { get; init; }
    public SchemaNode? Then { get; init; }
    public SchemaNode? Else { get; init; }
    public bool? UnevaluatedProperties { get; init; }
    public SchemaNode? PropertyNames { get; init; }
    public SchemaNode? Not { get; init; }
    public bool? UniqueItems { get; init; }
    public IReadOnlyDictionary<string, SchemaNode>? Definitions { get; init; }
    public bool IsMap { get; init; }
}
