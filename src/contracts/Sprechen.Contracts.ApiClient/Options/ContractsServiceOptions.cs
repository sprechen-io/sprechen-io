using System.ComponentModel.DataAnnotations;

namespace Sprechen.Contracts.ApiClient.Options;

/// <summary>
/// Hält die konfigurierbare Basisadresse des Vertragsdienstes bereit, damit HTTP-Aufrufe den vorgesehenen Dienst erreichen.
/// </summary>
public sealed class ContractsServiceOptions
{
    public const string SectionName = "ContractsService";

    [Required]
    public string BaseAddress { get; set; } = "http://localhost:8090";
}
