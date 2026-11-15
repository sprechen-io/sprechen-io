namespace Sprechen.ServiceDefaults;

/// <summary>
/// Definiert die JSON-Antwort des Versionsendpunkts getrennt von den intern
/// aufgelösten Versionsdaten.
/// </summary>
internal sealed record ApplicationVersionResponse(
    string DisplayVersion,
    string InformationalVersion,
    string AssemblyVersion,
    string? SourceRevision,
    string? ShortSourceRevision);
