namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Beschreibt die zulässigen Werte einer Schema-Aufzählung, damit die Oberfläche sie zur Auswahl anbieten kann.
/// </summary>
public sealed record EnumDefinition(
    string Name,
    IReadOnlyList<string> Values,
    string? SchemaFile = null,
    string? PropertyPath = null,
    string? Description = null);
