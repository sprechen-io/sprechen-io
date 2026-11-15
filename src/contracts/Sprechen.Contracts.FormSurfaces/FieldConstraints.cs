namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Bündelt die für eine Formularoberfläche abgeleiteten Validierungsregeln,
/// damit die Darstellung den Vertrag nicht erneut auswerten muss.
/// </summary>
internal sealed record FieldConstraints(
    bool Required,
    string? Pattern,
    int? MinLength,
    int? MaxLength,
    string? Min,
    string? Max,
    IReadOnlyList<string>? OneOfValues)
{
    public static readonly FieldConstraints Empty = new(false, null, null, null, null, null, null);
}
