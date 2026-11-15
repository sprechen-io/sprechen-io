namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Beschreibt einen Validierungsfehler mit Dokumentposition, damit die betroffene Stelle angezeigt werden kann.
/// </summary>
public sealed record CanonicalValidationIssue(string InstanceLocation, string Message);
