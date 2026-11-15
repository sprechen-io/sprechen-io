namespace Sprechen.EvaluationTelemetry.Api;

/// <summary>
/// Konfiguriert Freigabe und Leseschlüssel für den Archivzugriff.
/// </summary>
public sealed class EvaluationTelemetryArchiveOptions
{
    public const string SectionName = "EvaluationTelemetry:Archive";

    public bool IsEnabled { get; set; }

    public string ReadKey { get; set; } = string.Empty;
}
