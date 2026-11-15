namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Stellt Archivinhalt und Prüfsumme bereit und gibt den Stream beim Entsorgen frei.
/// </summary>
public sealed class EvaluationTelemetryArchiveSnapshot : IDisposable, IAsyncDisposable
{
    public EvaluationTelemetryArchiveSnapshot(
        Stream content,
        string sha256,
        int partitionCount,
        int eventCount,
        DateTimeOffset generatedAtUtc)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Sha256 = sha256 ?? throw new ArgumentNullException(nameof(sha256));
        PartitionCount = partitionCount;
        EventCount = eventCount;
        GeneratedAtUtc = generatedAtUtc;
    }

    public Stream Content { get; }

    public string Sha256 { get; }

    public int PartitionCount { get; }

    public int EventCount { get; }

    public DateTimeOffset GeneratedAtUtc { get; }

    public void Dispose() => Content.Dispose();

    public ValueTask DisposeAsync() => Content.DisposeAsync();
}
