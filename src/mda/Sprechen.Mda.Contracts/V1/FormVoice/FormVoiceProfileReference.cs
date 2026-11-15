using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.FormVoice;

/// <summary>
/// <para>DE: Referenz auf ein eigenständiges Sprachdialogprofil für ein Formular.</para>
/// <para>EN: Reference to a standalone form voice profile.</para>
/// </summary>
public sealed class FormVoiceProfileReference
{
    /// <summary>
    /// <para>DE: Referenzierte technische Profil-ID.</para>
    /// <para>EN: Referenced technical profile ID.</para>
    /// </summary>
    [JsonPropertyName("formVoiceProfileId")]
    public string FormVoiceProfileId { get; set; } = string.Empty;

    /// <summary>
    /// <para>DE: Referenzierte Profilversion.</para>
    /// <para>EN: Referenced profile version.</para>
    /// </summary>
    [JsonPropertyName("profileVersion")]
    public string ProfileVersion { get; set; } = string.Empty;
}
