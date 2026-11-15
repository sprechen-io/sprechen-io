using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.SpeechCapture;

/// <summary>
/// Antwortfenster der Sprachaushandlung und zusätzliche Zeit zum Einpendeln nach Ansagen.
/// Erstkontakt, wiederkehrende Teilnehmende und weitere Sprachfragen besitzen
/// getrennte Fenster, damit ihre Antwortzeiten unabhängig einstellbar bleiben.
/// </summary>
public sealed class SpeechCaptureSessionEntryProfile
{
    /// <summary>
    /// Antwortfenster für die erste Frage, wenn sie in dieser Sprache noch nicht bekannt ist.
    /// </summary>
    [JsonPropertyName("firstContactInitialSilenceTimeoutMs")]
    public int FirstContactInitialSilenceTimeoutMs { get; set; }

    /// <summary>
    /// Antwortfenster für die erste Frage bei wiederkehrenden Teilnehmenden.
    /// </summary>
    [JsonPropertyName("repeatInitialSilenceTimeoutMs")]
    public int RepeatInitialSilenceTimeoutMs { get; set; }

    /// <summary>
    /// Antwortfenster für weitere Sprachfragen, die vollständig in der angebotenen
    /// Sprache gesprochen werden. Unabhängig von Erstkontakt- und Wiederholungsfenster einstellbar.
    /// </summary>
    [JsonPropertyName("languageQuestionInitialSilenceTimeoutMs")]
    public int LanguageQuestionInitialSilenceTimeoutMs { get; set; }

    /// <summary>
    /// Zusätzliche Zeit zum Einpendeln, die der Sitzungseinstieg beim Anfordern jedes
    /// Antwortfensters zu dessen Dauer addiert. Sie bleibt getrennt modelliert,
    /// damit Antwortdauer und Ausgleich unabhängig angepasst werden können.
    /// </summary>
    [JsonPropertyName("playbackSettleAllowanceMs")]
    public int PlaybackSettleAllowanceMs { get; set; }
}
