using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.SpeechCapture;

/// <summary>
/// Katalog der modellierten Zeitparameter je Sprachaufnahmemodus.
/// </summary>
public sealed class SpeechCaptureProfileCatalog
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = string.Empty;

    [JsonPropertyName("catalogVersion")]
    public string CatalogVersion { get; set; } = string.Empty;

    [JsonPropertyName("captureProfilesByMode")]
    public List<SpeechCaptureProfile> CaptureProfilesByMode { get; set; } = [];

    [JsonPropertyName("micGate")]
    public SpeechCaptureMicGateProfile? MicGate { get; set; }

    [JsonPropertyName("segmenter")]
    public SpeechCaptureSegmenterProfile? Segmenter { get; set; }

    /// <summary>
    /// Erforderliche Einstellungen für den Sitzungseinstieg. Ein Katalog ohne diesen
    /// Block wird beim Lesen abgelehnt, statt mit Ersatzwerten aus dem Code fortzufahren.
    /// </summary>
    [JsonPropertyName("sessionEntry")]
    public required SpeechCaptureSessionEntryProfile SessionEntry { get; set; }
}
