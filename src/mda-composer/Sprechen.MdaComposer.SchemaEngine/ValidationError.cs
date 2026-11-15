namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Beschreibt eine Validierungsmeldung mit Dokumentpfad und Schweregrad für die Anzeige betroffener Stellen.
/// </summary>
public sealed record ValidationError(string Path, string Message, ValidationSeverity Severity = ValidationSeverity.Error);

/// <summary>
/// Unterscheidet Fehler, Warnungen und Hinweise für die Darstellung und Bewertung von Validierungsmeldungen.
/// </summary>
public enum ValidationSeverity
{
    Error,
    Warning,
    Info,
}
