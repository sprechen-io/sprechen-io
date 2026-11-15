using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Lädt das Katalogdokument aus JSON, damit nachfolgende Verarbeitungsschritte nicht selbst HTTP-Antworten deserialisieren müssen.
/// </summary>
internal sealed class FormsCatalogDocumentResourceClient
{
    private readonly ContractsApiResourceStreamClient _resources;
    private readonly ContractsApiJsonStreamReader _jsonReader;

    public FormsCatalogDocumentResourceClient(
        ContractsApiResourceStreamClient resources,
        ContractsApiJsonStreamReader jsonReader)
    {
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
        _jsonReader = jsonReader ?? throw new ArgumentNullException(nameof(jsonReader));
    }

    public async Task<FormsCatalogDocument> LoadAsync(CancellationToken cancellationToken)
    {
        await using var jsonStream = await _resources.GetFormsCatalogAsync(cancellationToken).ConfigureAwait(false);
        return await _jsonReader
            .ReadAsync<FormsCatalogDocument>(jsonStream, "Failed to deserialize application catalog.", cancellationToken)
            .ConfigureAwait(false);
    }
}
