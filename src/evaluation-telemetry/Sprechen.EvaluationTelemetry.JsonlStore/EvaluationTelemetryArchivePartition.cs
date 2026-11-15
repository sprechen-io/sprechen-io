using System.Text.Json.Serialization;

namespace Sprechen.EvaluationTelemetry.JsonlStore;

/// <summary>
/// Beschreibt eine Archivdatei mit Pfad, Prüfsumme, Länge und Ereigniszahl,
/// damit ihr Inhalt geprüft werden kann.
/// </summary>
internal sealed record EvaluationTelemetryArchivePartition(
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("sha256")] string Sha256,
    [property: JsonPropertyName("byte_length")] long ByteLength,
    [property: JsonPropertyName("event_count")] int EventCount);
