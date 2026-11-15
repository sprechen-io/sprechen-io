using System.ComponentModel.DataAnnotations;

namespace Sprechen.Web.HostSupport.Options;

/// <summary>
/// Dienstauthentisierung des Hosts, damit der API-Schlüssel nicht in der WASM-App liegt.
/// </summary>
internal sealed class ServiceAuthOptions
{
    public const string SectionName = "ServiceAuth";

    [Required]
    public string ApiKey { get; set; } = string.Empty;
}
