using System.Text.Json;

namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Liest JSON aus einem Stream und weist null zurück, damit Aufrufer ein deserialisiertes Dokument erhalten.
/// </summary>
internal sealed class ContractsApiJsonStreamReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<T> ReadAsync<T>(
        Stream jsonStream,
        string nullDocumentMessage,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(jsonStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(nullDocumentMessage);

        var document = await JsonSerializer.DeserializeAsync<T>(jsonStream, JsonOptions, cancellationToken).ConfigureAwait(false);
        return document ?? throw new InvalidOperationException(nullDocumentMessage);
    }
}
