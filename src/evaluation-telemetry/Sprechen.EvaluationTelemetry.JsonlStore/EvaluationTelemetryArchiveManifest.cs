using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sprechen.EvaluationTelemetry.JsonlStore;

/// <summary>
/// Verzeichnet die Archivdateien und Ereigniszahlen,
/// damit der exportierte Bestand auf Vollständigkeit geprüft werden kann.
/// </summary>
internal sealed record EvaluationTelemetryArchiveManifest(
    [property: JsonPropertyName("schema_version")] int SchemaVersion,
    [property: JsonPropertyName("generated_at_utc")] DateTime GeneratedAtUtc,
    [property: JsonPropertyName("partition_count")] int PartitionCount,
    [property: JsonPropertyName("event_count")] int EventCount,
    [property: JsonPropertyName("partitions")] IReadOnlyList<EvaluationTelemetryArchivePartition> Partitions)
{
    internal static JsonSerializerOptions SerializerOptions { get; } = new()
    {
        WriteIndented = true,
    };
}
