namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Bündelt Gültigkeit, Fehler und Zusammenfassung einer Schemaprüfung für Sitzung und Oberfläche.
/// </summary>
public sealed record CanonicalValidationResult(
    bool IsValid,
    IReadOnlyList<CanonicalValidationIssue> Issues,
    string Summary);
