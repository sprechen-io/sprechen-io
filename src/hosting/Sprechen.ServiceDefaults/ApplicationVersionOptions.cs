namespace Sprechen.ServiceDefaults;

/// <summary>
/// Ermöglicht, Anzeigeversion und Quellenrevision per Konfiguration
/// gegenüber den Assembly-Metadaten vorzugeben.
/// </summary>
public sealed class ApplicationVersionOptions
{
    public const string SectionName = "Application";

    public string Version { get; set; } = string.Empty;

    public string SourceRevision { get; set; } = string.Empty;
}
