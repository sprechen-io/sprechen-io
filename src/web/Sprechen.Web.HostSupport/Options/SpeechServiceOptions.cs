using System.ComponentModel.DataAnnotations;

namespace Sprechen.Web.HostSupport.Options;

/// <summary>
/// Konfiguriert die Basisadresse des Sprachdienstes mit einem lokalen Standardwert.
/// </summary>
internal sealed class SpeechServiceOptions
{
    public const string SectionName = "SpeechService";

    [Required]
    public string BaseAddress { get; set; } = "http://localhost:8080";
}
