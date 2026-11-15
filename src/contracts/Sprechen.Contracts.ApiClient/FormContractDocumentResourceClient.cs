using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Lädt den Formularvertrag als typisiertes Dokument, damit seine Referenzen beim Zusammenführen mit den zugehörigen Definitionen verfügbar sind.
/// </summary>
internal sealed class FormContractDocumentResourceClient
{
    private readonly ContractsApiResourceStreamClient _resources;
    private readonly ContractsApiJsonStreamReader _jsonReader;

    public FormContractDocumentResourceClient(
        ContractsApiResourceStreamClient resources,
        ContractsApiJsonStreamReader jsonReader)
    {
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
        _jsonReader = jsonReader ?? throw new ArgumentNullException(nameof(jsonReader));
    }

    public async Task<FormContractDocument> LoadAsync(
        string applicationId,
        string formId,
        string contractVersion,
        CancellationToken cancellationToken)
    {
        await using var jsonStream = await _resources
            .GetFormContractAsync(applicationId, formId, contractVersion, cancellationToken)
            .ConfigureAwait(false);
        return await _jsonReader
            .ReadAsync<FormContractDocument>(jsonStream, "Failed to deserialize form contract document.", cancellationToken)
            .ConfigureAwait(false);
    }
}
