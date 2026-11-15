using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Regel zur Zulässigkeit eines akustischen Aktivierungswortdetektors.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<WakeWordDetectorEligibilityPolicy>))]
public enum WakeWordDetectorEligibilityPolicy
{
    /// <summary>
    /// Akustische Erkennung ist nur für den modellierten Standardwortbestand zulässig.
    /// </summary>
    [JsonStringEnumMemberName("defaultInventoryOnly")]
    DefaultInventoryOnly = 0
}
