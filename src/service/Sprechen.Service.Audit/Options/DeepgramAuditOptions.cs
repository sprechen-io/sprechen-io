using System.ComponentModel.DataAnnotations;

namespace Sprechen.Service.Audit.Options;

/// <summary>
/// Konfiguration der dateibasierten Protokollierung von Deepgram-Aufrufen.
/// </summary>
public sealed class DeepgramAuditOptions
{
    public const string SectionName = "DeepgramAudit";

    /// <summary>
    /// Aktiviert die Aufzeichnung. Bei false wird die wirkungslose Implementierung verwendet. Standardwert ist true.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Verzeichnis für tägliche Dateien namens deepgram-audit-{yyyy-MM-dd}.jsonl.
    /// Relative Pfade werden vom aktuellen Arbeitsverzeichnis des Prozesses aus aufgelöst.
    /// Das Verzeichnis muss für den Dienstprozess beschreibbar sein.
    /// </summary>
    [Required]
    public string JsonlDirectory { get; init; } = "studydata/deepgram-audit";
}
