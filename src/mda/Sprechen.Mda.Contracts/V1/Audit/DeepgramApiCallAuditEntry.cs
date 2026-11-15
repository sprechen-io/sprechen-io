using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.Audit;

/// <summary>
/// Erfasst Zeitpunkte, ausgewählte Einstellungen und Ergebnis eines Deepgram-Aufrufs
/// für spätere Nachvollziehbarkeit. Der Datensatz enthält keine Audio- oder Transkriptinhalte.
/// </summary>
public sealed record DeepgramApiCallAuditEntry
{
    /// <summary>UTC-Startzeitpunkt des Aufrufs.</summary>
    [JsonPropertyName("ts_started_utc")]
    public required DateTime StartedUtc { get; init; }

    /// <summary>UTC-Endzeitpunkt, auch bei Fehlschlag.</summary>
    [JsonPropertyName("ts_completed_utc")]
    public required DateTime CompletedUtc { get; init; }

    /// <summary>Verstrichene Dauer des Aufrufs in Millisekunden.</summary>
    [JsonPropertyName("latency_ms")]
    public required int LatencyMs { get; init; }

    /// <summary>Art des protokollierten Deepgram-Aufrufs.</summary>
    [JsonPropertyName("entry_kind")]
    public required AuditEntryKind EntryKind { get; init; }

    /// <summary>Deepgram-Endpunkt ohne Query-String.</summary>
    [JsonPropertyName("endpoint")]
    public required string Endpoint { get; init; }

    /// <summary>Deepgram-Modellname (z. B. <c>nova-3-general</c>, <c>aura-2-aurelia-de</c>).</summary>
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    /// <summary>
    /// Deepgram-Modellversion aus <c>X-DG-Model-Version</c>. Null, falls der Header
    /// fehlt oder nicht ausgewertet werden konnte.
    /// </summary>
    [JsonPropertyName("model_version")]
    public string? ModelVersion { get; init; }

    /// <summary>Audiokodierung (z. B. <c>linear16</c>) bei STT/Conversation. Bei TTS null.</summary>
    [JsonPropertyName("encoding")]
    public string? Encoding { get; init; }

    /// <summary>Abtastrate in Hz.</summary>
    [JsonPropertyName("sample_rate_hz")]
    public int? SampleRateHz { get; init; }

    /// <summary>Der beim Aufruf gesendete Ausschluss vom Modellverbesserungsprogramm.</summary>
    [JsonPropertyName("mip_opt_out")]
    public required bool MipOptOut { get; init; }

    /// <summary>Sprachhinweise an Deepgram. Leer ohne Hinweis.</summary>
    [JsonPropertyName("language_hints")]
    public required IReadOnlyList<string> LanguageHints { get; init; } = Array.Empty<string>();

    /// <summary>Von Deepgram erkannte Sprachen. Leer ohne Erkennung.</summary>
    [JsonPropertyName("detected_languages")]
    public required IReadOnlyList<string> DetectedLanguages { get; init; } = Array.Empty<string>();

    /// <summary>EOT-Schwelle des Anbieters. Null bei TTS oder ohne Schwelle.</summary>
    [JsonPropertyName("eot_threshold")]
    public double? EotThreshold { get; init; }

    /// <summary>Eager-EOT-Schwelle. Null wenn aus oder TTS.</summary>
    [JsonPropertyName("eager_eot_threshold")]
    public double? EagerEotThreshold { get; init; }

    /// <summary>EOT-Timeout in ms. Bei TTS null.</summary>
    [JsonPropertyName("eot_timeout_ms")]
    public int? EotTimeoutMs { get; init; }

    /// <summary>Korrelations-ID des Aufrufs (gRPC-Kopf oder intern erzeugt).</summary>
    [JsonPropertyName("correlation_id")]
    public required string CorrelationId { get; init; }

    /// <summary>Optionale Sitzungskennung zur Zuordnung des Aufrufs zur Interaktion.</summary>
    [JsonPropertyName("session_id")]
    public string? SessionId { get; init; }

    /// <summary>Abschließender Status des protokollierten Aufrufs.</summary>
    [JsonPropertyName("outcome")]
    public required DeepgramApiCallOutcome Outcome { get; init; }

    /// <summary>Transkriptlänge in Zeichen bei Spracherkennung oder Gesprächen. Sonst null.</summary>
    [JsonPropertyName("transcript_length_chars")]
    public int? TranscriptLengthChars { get; init; }

    /// <summary>Konfidenz 0..100, falls Deepgram sie liefert. Sonst null.</summary>
    [JsonPropertyName("confidence_percent")]
    public int? ConfidencePercent { get; init; }

    /// <summary>Optionaler Fehlercode, wenn <see cref="Outcome"/> <see cref="DeepgramApiCallOutcome.Aborted"/> ist.</summary>
    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; init; }
}
