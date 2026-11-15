using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Betriebsmodus eines modellierten Aktivierungswortdetektors.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<WakeWordDetectorOperatingMode>))]
public enum WakeWordDetectorOperatingMode
{
    /// <summary>
    /// Aktivierungswort ausschließlich im Transkript erkennen.
    /// </summary>
    [JsonStringEnumMemberName("lexicalFallbackOnly")]
    LexicalFallbackOnly = 0,

    /// <summary>
    /// Akustische Erkennung bevorzugen und bei Bedarf das Aktivierungswort im Transkript erkennen.
    /// </summary>
    [JsonStringEnumMemberName("acousticPreferred")]
    AcousticPreferred = 1
}
