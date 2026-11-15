using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Sprachabhängiger Name des Aktivierungsworts. Die vollständigen Formen entstehen
/// zur Laufzeit aus diesem Namen und den gemeinsamen <c>commandAnchorLeadIns</c>.
/// </summary>
public sealed class WakeWordLanguageProfile
{
    /// <summary>
    /// ISO-639-1-Sprachcode des Profils.
    /// </summary>
    [JsonPropertyName("languageCode")]
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>
    /// Name des Aktivierungsworts für diese Sprache. Der Referenzvertrag verwendet
    /// sprachübergreifend „alex“. Zusammen mit den Einleitungswörtern entstehen etwa
    /// „okay alex“, „hey alex“ und die zusätzlich erkannte Form „ok alex“.
    /// </summary>
    [JsonPropertyName("wakeWordName")]
    public string WakeWordName { get; set; } = string.Empty;
}
