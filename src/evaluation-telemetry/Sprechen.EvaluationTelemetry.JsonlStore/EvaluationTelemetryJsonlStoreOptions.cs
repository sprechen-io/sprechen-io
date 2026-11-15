namespace Sprechen.EvaluationTelemetry.JsonlStore;

/// <summary>
/// Konfiguriert Verzeichnis und Aktivierung des JSONL-Speichers.
/// Das Standardverzeichnis trennt die aktuelle Speicherform von älteren Datenbeständen.
/// </summary>
public sealed class EvaluationTelemetryJsonlStoreOptions
{
    public const string SectionName = "EvaluationTelemetry:JsonlStore";

    public string DataDirectory { get; set; } = "/data/generation-3";

    public bool IsEnabled { get; set; } = true;
}
