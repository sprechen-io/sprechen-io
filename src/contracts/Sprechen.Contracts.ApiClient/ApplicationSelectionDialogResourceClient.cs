using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Preflight;

namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Lädt und prüft den Anwendungsauswahldialog vor dem Zwischenspeichern, damit der Katalog nur ein gültiges Dialogmodell erhält.
/// </summary>
internal sealed class ApplicationSelectionDialogResourceClient
{
    private readonly ContractsApiResourceStreamClient _resources;
    private readonly ContractsApiJsonStreamReader _jsonReader;
    private readonly ContractsApiPreflightFailureMapper _preflightFailureMapper;
    private readonly ContractsApiResourceCacheKeyFactory _cacheKeyFactory;
    private readonly ContractsApiResourceCache<ApplicationSelectionDialog> _cache = new();

    public ApplicationSelectionDialogResourceClient(
        ContractsApiResourceStreamClient resources,
        ContractsApiJsonStreamReader jsonReader,
        ContractsApiPreflightFailureMapper preflightFailureMapper,
        ContractsApiResourceCacheKeyFactory cacheKeyFactory)
    {
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
        _jsonReader = jsonReader ?? throw new ArgumentNullException(nameof(jsonReader));
        _preflightFailureMapper = preflightFailureMapper ?? throw new ArgumentNullException(nameof(preflightFailureMapper));
        _cacheKeyFactory = cacheKeyFactory ?? throw new ArgumentNullException(nameof(cacheKeyFactory));
    }

    public Task<ApplicationSelectionDialog> GetAsync(
        string selectionDialogId,
        string modelVersion,
        CancellationToken cancellationToken)
    {
        var cacheKey = _cacheKeyFactory.Create(selectionDialogId, modelVersion);
        return _cache.GetOrCreateAsync(
            cacheKey,
            ct => LoadAsync(selectionDialogId.Trim(), modelVersion.Trim(), ct),
            cancellationToken);
    }

    private async Task<ApplicationSelectionDialog> LoadAsync(
        string selectionDialogId,
        string modelVersion,
        CancellationToken cancellationToken)
    {
        await using var jsonStream = await _resources
            .GetApplicationSelectionDialogAsync(selectionDialogId, modelVersion, cancellationToken)
            .ConfigureAwait(false);
        var model = await _jsonReader
            .ReadAsync<ApplicationSelectionDialog>(
                jsonStream,
                "Failed to deserialize application selection dialog.",
                cancellationToken)
            .ConfigureAwait(false);

        _preflightFailureMapper.ThrowInvalidOperationIfFailed(
            ApplicationSelectionDialogPreflight.Validate(model),
            "ApplicationSelectionDialog");
        return model;
    }
}
