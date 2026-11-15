namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Überträgt eine verletzte Formularbedingung samt Geltungsbereich und Abhilfe.
/// </summary>
public sealed record FormConstraintViolation(
    string FieldId,
    string ConstraintId,
    string Severity,
    string Code,
    string Message,
    string? Remediation,
    string Scope,
    string? FieldGroupId);



