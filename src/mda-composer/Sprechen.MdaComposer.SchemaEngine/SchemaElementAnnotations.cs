namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Bündelt gelesene Beschreibungen, Standardwerte und Einschränkungen eines Schemaknotens für Anzeige und Verarbeitung.
/// </summary>
internal sealed class SchemaElementAnnotations
{
    public string? Title { get; init; }

    public string? Description { get; init; }

    public string? Pattern { get; init; }

    public string? Format { get; init; }

    public string? ConstValue { get; init; }

    public object? DefaultValue { get; init; }

    public double? Minimum { get; init; }

    public double? Maximum { get; init; }

    public int? MinLength { get; init; }

    public int? MaxLength { get; init; }

    public int? MinItems { get; init; }

    public int? MaxItems { get; init; }

    public int? MinProperties { get; init; }

    public IReadOnlyList<string>? EnumValues { get; init; }

    public bool? UnevaluatedProperties { get; init; }

    public bool? UniqueItems { get; init; }
}
