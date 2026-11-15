using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Variables;

namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Lädt und prüft Variablendefinitionen vor dem Zwischenspeichern, damit Ausdrücke nur mit gültigen Definitionen ausgewertet werden.
/// </summary>
internal sealed class VariableDefinitionsResourceClient
{
    private readonly ContractsApiResourceStreamClient _resources;
    private readonly ContractsApiJsonStreamReader _jsonReader;
    private readonly ContractsApiPreflightFailureMapper _preflightFailureMapper;
    private readonly ContractsApiResourceCacheKeyFactory _cacheKeyFactory;
    private readonly ContractsApiResourceCache<VariableDefinitions> _cache = new();

    public VariableDefinitionsResourceClient(
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

    public Task<VariableDefinitions> GetAsync(
        string variableDefinitionsId,
        string catalogVersion,
        CancellationToken cancellationToken)
    {
        var cacheKey = _cacheKeyFactory.Create(variableDefinitionsId, catalogVersion);
        return _cache.GetOrCreateAsync(
            cacheKey,
            ct => LoadAsync(variableDefinitionsId.Trim(), catalogVersion.Trim(), ct),
            cancellationToken);
    }

    private async Task<VariableDefinitions> LoadAsync(
        string variableDefinitionsId,
        string catalogVersion,
        CancellationToken cancellationToken)
    {
        await using var jsonStream = await _resources
            .GetVariableDefinitionsAsync(variableDefinitionsId, catalogVersion, cancellationToken)
            .ConfigureAwait(false);
        var variableCatalog = await _jsonReader
            .ReadAsync<VariableDefinitions>(jsonStream, "Failed to deserialize variable catalog.", cancellationToken)
            .ConfigureAwait(false);

        _preflightFailureMapper.ThrowContractsPreflightFailedIfFailed(
            VariableDefinitionsPreflight.Validate(variableCatalog),
            "VariableDefinitions");
        return variableCatalog;
    }
}
