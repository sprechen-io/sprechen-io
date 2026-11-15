namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Lädt Modellschemas über den gemeinsamen HTTP-Client, damit Schemas und Modelldaten dieselbe konfigurierte Verbindung nutzen.
/// </summary>
internal sealed class ContractsApiModelSchemaStreamClient
{
    private readonly ContractsApiModelSchemaRoutes _routes;
    private readonly ContractsApiHttpStreamFetcher _streamFetcher;

    public ContractsApiModelSchemaStreamClient(
        ContractsApiModelSchemaRoutes routes,
        ContractsApiHttpStreamFetcher streamFetcher)
    {
        _routes = routes ?? throw new ArgumentNullException(nameof(routes));
        _streamFetcher = streamFetcher ?? throw new ArgumentNullException(nameof(streamFetcher));
    }

    public Task<Stream> GetFormsCatalogSchemaAsync(CancellationToken cancellationToken)
    {
        return _streamFetcher.GetStreamAsync(_routes.FormsCatalogSchema(), cancellationToken);
    }

    public Task<Stream> GetFieldDefinitionsSchemaAsync(CancellationToken cancellationToken)
    {
        return _streamFetcher.GetStreamAsync(_routes.FieldDefinitionsSchema(), cancellationToken);
    }

    public Task<Stream> GetVariableDefinitionsSchemaAsync(CancellationToken cancellationToken)
    {
        return _streamFetcher.GetStreamAsync(_routes.VariableDefinitionsSchema(), cancellationToken);
    }

    public Task<Stream> GetFormVoiceProfileSchemaAsync(CancellationToken cancellationToken)
    {
        return _streamFetcher.GetStreamAsync(_routes.FormVoiceProfileSchema(), cancellationToken);
    }

    public Task<Stream> GetApplicationSelectionDialogSchemaAsync(CancellationToken cancellationToken)
    {
        return _streamFetcher.GetStreamAsync(_routes.ApplicationSelectionDialogSchema(), cancellationToken);
    }

    public Task<Stream> GetApplicationSelectionDialogProfileSchemaAsync(CancellationToken cancellationToken)
    {
        return _streamFetcher.GetStreamAsync(_routes.ApplicationSelectionDialogProfileSchema(), cancellationToken);
    }

    public Task<Stream> GetFormContractSchemaAsync(CancellationToken cancellationToken)
    {
        return _streamFetcher.GetStreamAsync(_routes.FormContractSchema(), cancellationToken);
    }
}
