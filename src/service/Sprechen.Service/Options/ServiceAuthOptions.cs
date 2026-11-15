using System.ComponentModel.DataAnnotations;

namespace Sprechen.Service.Options;

/// <summary>
/// Enthält den erforderlichen API-Schlüssel zur Authentifizierung am Sprachdienst.
/// </summary>
internal sealed class ServiceAuthOptions
{
    public const string SectionName = "ServiceAuth";

    [Required]
    public string ApiKey { get; set; } = string.Empty;
}
