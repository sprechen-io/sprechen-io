using System.Text.Json.Serialization;

namespace Sprechen.Forms.Pages;

/// <summary>
/// JSON-Fehlerantwort des Formularlisten-Endpunkts: trägt den stabilen Fehlercode (z. B. CATALOG_LOAD_FAILED) als `errorCode`.
/// </summary>
internal sealed class IndexPageFormsEndpointError
{
    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; init; } = string.Empty;
}
