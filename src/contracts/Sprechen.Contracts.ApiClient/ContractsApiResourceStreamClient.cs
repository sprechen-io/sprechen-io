namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Lädt Vertragsdokumente als Streams, damit die nachfolgenden Leser keine HTTP-Adressen zusammensetzen müssen.
/// </summary>
internal sealed class ContractsApiResourceStreamClient
{
    private readonly ContractsApiResourceRoutes _routes;
    private readonly ContractsApiHttpStreamFetcher _streamFetcher;

    public ContractsApiResourceStreamClient(
        ContractsApiResourceRoutes routes,
        ContractsApiHttpStreamFetcher streamFetcher)
    {
        _routes = routes ?? throw new ArgumentNullException(nameof(routes));
        _streamFetcher = streamFetcher ?? throw new ArgumentNullException(nameof(streamFetcher));
    }

    public Task<Stream> GetFormsCatalogAsync(CancellationToken cancellationToken)
    {
        return _streamFetcher.GetStreamAsync(_routes.FormsCatalog(), cancellationToken);
    }

    public Task<Stream> GetFormContractAsync(
        string applicationId,
        string formId,
        string contractVersion,
        CancellationToken cancellationToken)
    {
        return _streamFetcher.GetStreamAsync(
            _routes.FormContract(applicationId, formId, contractVersion),
            cancellationToken);
    }

    public Task<Stream> GetFieldDefinitionsAsync(
        string fieldDefinitionsId,
        string catalogVersion,
        CancellationToken cancellationToken)
    {
        return _streamFetcher.GetStreamAsync(
            _routes.FieldDefinitions(fieldDefinitionsId, catalogVersion),
            cancellationToken);
    }

    public Task<Stream> GetVariableDefinitionsAsync(
        string variableDefinitionsId,
        string catalogVersion,
        CancellationToken cancellationToken)
    {
        return _streamFetcher.GetStreamAsync(
            _routes.VariableDefinitions(variableDefinitionsId, catalogVersion),
            cancellationToken);
    }

    public Task<Stream> GetFormVoiceProfileAsync(
        string formVoiceProfileId,
        string profileVersion,
        CancellationToken cancellationToken)
    {
        return _streamFetcher.GetStreamAsync(
            _routes.FormVoiceProfile(formVoiceProfileId, profileVersion),
            cancellationToken);
    }

    public Task<Stream> GetApplicationSelectionDialogAsync(
        string selectionDialogId,
        string modelVersion,
        CancellationToken cancellationToken)
    {
        return _streamFetcher.GetStreamAsync(
            _routes.ApplicationSelectionDialog(selectionDialogId, modelVersion),
            cancellationToken);
    }

    public Task<Stream> GetApplicationSelectionDialogProfileAsync(
        string profileId,
        string profileVersion,
        CancellationToken cancellationToken)
    {
        return _streamFetcher.GetStreamAsync(
            _routes.ApplicationSelectionDialogProfile(profileId, profileVersion),
            cancellationToken);
    }
}
