using Sprechen.Mda.Contracts.V1.Fields;
using Sprechen.Mda.Contracts.V1.Preflight;

namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Lädt und prüft Felddefinitionen vor dem Zwischenspeichern, damit ungültige Definitionen nicht im Formularvertrag verwendet werden.
/// </summary>
internal sealed class FieldDefinitionsResourceClient
{
    private readonly ContractsApiResourceStreamClient _resources;
    private readonly ContractsApiJsonStreamReader _jsonReader;
    private readonly ContractsApiPreflightFailureMapper _preflightFailureMapper;
    private readonly ContractsApiResourceCacheKeyFactory _cacheKeyFactory;
    private readonly ContractsApiResourceCache<FieldDefinitions> _cache = new();

    public FieldDefinitionsResourceClient(
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

    public Task<FieldDefinitions> GetAsync(
        string fieldDefinitionsId,
        string catalogVersion,
        CancellationToken cancellationToken)
    {
        var cacheKey = _cacheKeyFactory.Create(fieldDefinitionsId, catalogVersion);
        return _cache.GetOrCreateAsync(
            cacheKey,
            ct => LoadAsync(fieldDefinitionsId.Trim(), catalogVersion.Trim(), ct),
            cancellationToken);
    }

    private async Task<FieldDefinitions> LoadAsync(
        string fieldDefinitionsId,
        string catalogVersion,
        CancellationToken cancellationToken)
    {
        await using var jsonStream = await _resources
            .GetFieldDefinitionsAsync(fieldDefinitionsId, catalogVersion, cancellationToken)
            .ConfigureAwait(false);
        var fieldCatalog = await _jsonReader
            .ReadAsync<FieldDefinitions>(jsonStream, "Failed to deserialize field catalog.", cancellationToken)
            .ConfigureAwait(false);

        _preflightFailureMapper.ThrowContractsPreflightFailedIfFailed(
            FieldDefinitionsPreflight.Validate(fieldCatalog),
            "FieldDefinitions");
        return fieldCatalog;
    }
}
