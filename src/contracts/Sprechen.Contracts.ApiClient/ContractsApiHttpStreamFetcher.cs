using System.Net;

namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Puffert den Antwortinhalt und weist leere Antworten zurück, damit der zurückgegebene Stream nach Freigabe der HTTP-Antwort lesbar bleibt.
/// </summary>
internal sealed class ContractsApiHttpStreamFetcher
{
    private readonly HttpClient _httpClient;

    public ContractsApiHttpStreamFetcher(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<Stream> GetStreamAsync(string relativePath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        using var response = await _httpClient
            .GetAsync(relativePath, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.NoContent)
        {
            throw new InvalidOperationException($"No response body received for '{relativePath}'.");
        }

        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        if (bytes.Length == 0)
        {
            throw new InvalidOperationException($"No response body received for '{relativePath}'.");
        }

        return new MemoryStream(bytes, writable: false);
    }
}
