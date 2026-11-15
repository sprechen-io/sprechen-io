using System.Diagnostics.Metrics;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Zählt ohne Anbieterfehler abgeschlossene Spracherkennungsanfragen mit leerem Transkript.
/// </summary>
internal static class SpeechRecognitionTelemetry
{
    private static readonly Meter Meter = new("Sprechen.Service.SpeechRecognition", "1.0.0");

    private static readonly Counter<long> EmptyTranscriptsTotal = Meter.CreateCounter<long>(
        "stt.empty_transcripts_total",
        unit: "transcripts",
        description: "Successful speech-to-text requests that produced an empty transcript.");

    public static void RecordEmptyTranscript(
        string operation,
        string provider,
        string? languageCode,
        string mimeType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);

        EmptyTranscriptsTotal.Add(
            1,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("provider", provider),
            new KeyValuePair<string, object?>("language_code", string.IsNullOrWhiteSpace(languageCode) ? "auto" : languageCode),
            new KeyValuePair<string, object?>("mime_type", mimeType));
    }
}
