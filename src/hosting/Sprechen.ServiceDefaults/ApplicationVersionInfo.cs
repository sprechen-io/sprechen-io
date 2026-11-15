namespace Sprechen.ServiceDefaults;

/// <summary>
/// Enthält den aufgelösten Versionsstand einer Host-Anwendung als gemeinsame
/// Datenquelle für Versionsendpunkt und Antwortheader.
/// </summary>
public sealed record ApplicationVersionInfo(
    string DisplayVersion,
    string InformationalVersion,
    string AssemblyVersion,
    string? SourceRevision,
    string? ShortSourceRevision);
