namespace Sprechen.Web.HostSupport.Endpoints.Adapter;

/// <summary>
/// Liest textuelle Antworten des Vertragsdienstes für die Fehlerabbildung.
/// </summary>
internal sealed class ProxyTextResponseReader
{
    public async Task<string?> ReadAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.Content is null || !HasTextPayload(response))
        {
            return null;
        }

        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    private static bool HasTextPayload(HttpResponseMessage response)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType;
        return string.IsNullOrWhiteSpace(contentType) ||
            contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
            contentType.Equals("application/json", StringComparison.OrdinalIgnoreCase);
    }
}
