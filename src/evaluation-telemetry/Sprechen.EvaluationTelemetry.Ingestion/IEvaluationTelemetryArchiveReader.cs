namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Liefert einen lesbaren Stand des Archivs, damit der Host
/// die gespeicherten Dateien nicht selbst öffnen muss.
/// </summary>
public interface IEvaluationTelemetryArchiveReader
{
    Task<EvaluationTelemetryArchiveSnapshot> CreateSnapshotAsync(
        CancellationToken cancellationToken = default);
}
