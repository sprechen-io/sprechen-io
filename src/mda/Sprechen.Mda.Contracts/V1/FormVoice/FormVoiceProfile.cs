using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.FormVoice;

/// <summary>
/// <para>DE: Eigenständiges Laufzeitprofil für die Spracheinstellungen eines Formulardialogs (V1).</para>
/// <para>EN: Standalone runtime profile for form-dialog-specific voice settings (V1).</para>
/// </summary>
public sealed class FormVoiceProfile
{
    /// <summary>
    /// <para>DE: Schema-Hauptversion des Sprachdialogprofils.</para>
    /// <para>EN: Schema major version of the form voice profile.</para>
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = string.Empty;

    /// <summary>
    /// <para>DE: Inhaltliche Profilversion des Sprachdialogprofils.</para>
    /// <para>EN: Content-level profile version of the form voice profile.</para>
    /// </summary>
    [JsonPropertyName("profileVersion")]
    public string ProfileVersion { get; set; } = string.Empty;

    /// <summary>
    /// <para>DE: Stabile technische ID des wiederverwendbaren Sprachdialogprofils.</para>
    /// <para>EN: Stable technical ID of the reusable form voice profile.</para>
    /// </summary>
    [JsonPropertyName("formVoiceProfileId")]
    public string FormVoiceProfileId { get; set; } = string.Empty;

    /// <summary>
    /// <para>DE: Ob Nutzerinnen und Nutzer die TTS-Ausgabe per Spracheingabe unterbrechen können.</para>
    /// <para>EN: Whether users can interrupt TTS output with speech input.</para>
    /// </summary>
    [JsonPropertyName("allowBargeIn")]
    public bool? AllowBargeIn { get; set; }

    /// <summary>
    /// <para>DE: Wartezeit in Millisekunden auf eine Eingabe während der Aufnahme.</para>
    /// <para>EN: No-input timeout in milliseconds while listening.</para>
    /// </summary>
    [JsonPropertyName("noInputTimeoutMs")]
    public int? NoInputTimeoutMs { get; set; }

    /// <summary>
    /// <para>DE: Höchstzahl aufeinanderfolgender Wiederholungen bei ausbleibender Eingabe.</para>
    /// <para>EN: Maximum number of consecutive no-input retries.</para>
    /// </summary>
    [JsonPropertyName("noInputMaxRetries")]
    public int? NoInputMaxRetries { get; set; }

    /// <summary>
    /// <para>DE: Minimale STT-Konfidenz (0..100) für direkte Akzeptanz ohne Rückfrage.</para>
    /// <para>EN: Minimum STT confidence (0..100) for direct acceptance without confirmation.</para>
    /// </summary>
    [JsonPropertyName("sttMinConfidenceToAccept")]
    public int? SttMinConfidenceToAccept { get; set; }

    /// <summary>
    /// <para>DE: Minimale STT-Konfidenz (0..100) für nutzbare, aber bestätigungspflichtige Eingaben.</para>
    /// <para>EN: Minimum STT confidence (0..100) for usable but confirmation-requiring input.</para>
    /// </summary>
    [JsonPropertyName("sttMinConfidenceToConfirm")]
    public int? SttMinConfidenceToConfirm { get; set; }

    /// <summary>
    /// <para>DE: Standard-Sprechgeschwindigkeit für TTS als Multiplikator (0.5 bis 2.0).</para>
    /// <para>EN: Default TTS speech rate as multiplier (0.5 to 2.0).</para>
    /// </summary>
    [JsonPropertyName("speechRate")]
    public double? SpeechRate { get; set; }

    /// <summary>
    /// <para>DE: Vorgabe zum Umgang mit diakritischen Zeichen bei Spracheingaben.</para>
    /// <para>EN: Strategy hint for diacritics canonicalization in voice-first flows.</para>
    /// </summary>
    [JsonPropertyName("diacriticsStrategy")]
    public string? DiacriticsStrategy { get; set; }

    /// <summary>
    /// <para>DE: Pause in Millisekunden nach einem Prompt, bevor die Aufnahme beginnt.</para>
    /// <para>EN: Pause in milliseconds after a prompt before the system starts listening.</para>
    /// </summary>
    [JsonPropertyName("pauseAfterPromptMs")]
    public int? PauseAfterPromptMs { get; set; }
}
