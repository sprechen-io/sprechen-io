using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Preflight;

namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Lädt und prüft Auswahlprofile vor dem Zwischenspeichern, damit nur gültige Profile weitergegeben werden.
/// </summary>
internal sealed class ApplicationSelectionDialogProfileResourceClient
{
    private readonly ContractsApiResourceStreamClient _resources;
    private readonly ContractsApiJsonStreamReader _jsonReader;
    private readonly ContractsApiPreflightFailureMapper _preflightFailureMapper;
    private readonly ContractsApiResourceCacheKeyFactory _cacheKeyFactory;
    private readonly ContractsApiResourceCache<ApplicationSelectionDialogProfile> _cache = new();

    public ApplicationSelectionDialogProfileResourceClient(
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

    public async Task<IReadOnlyDictionary<ApplicationSelectionDialogProfileKey, ApplicationSelectionDialogProfile>> LoadProfilesAsync(
        IReadOnlyCollection<ApplicationSelectionDialogProfileKey> profileReferences,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profileReferences);

        var profiles = new Dictionary<ApplicationSelectionDialogProfileKey, ApplicationSelectionDialogProfile>(profileReferences.Count);
        foreach (var reference in profileReferences)
        {
            profiles[reference] = await GetAsync(reference.ProfileId, reference.ProfileVersion, cancellationToken).ConfigureAwait(false);
        }

        return profiles;
    }

    private Task<ApplicationSelectionDialogProfile> GetAsync(
        string profileId,
        string profileVersion,
        CancellationToken cancellationToken)
    {
        var cacheKey = _cacheKeyFactory.Create(profileId, profileVersion);
        return _cache.GetOrCreateAsync(
            cacheKey,
            ct => LoadAsync(profileId.Trim(), profileVersion.Trim(), ct),
            cancellationToken);
    }

    private async Task<ApplicationSelectionDialogProfile> LoadAsync(
        string profileId,
        string profileVersion,
        CancellationToken cancellationToken)
    {
        await using var jsonStream = await _resources
            .GetApplicationSelectionDialogProfileAsync(profileId, profileVersion, cancellationToken)
            .ConfigureAwait(false);
        var profile = await _jsonReader
            .ReadAsync<ApplicationSelectionDialogProfile>(
                jsonStream,
                "Failed to deserialize application selection-dialog profile.",
                cancellationToken)
            .ConfigureAwait(false);

        _preflightFailureMapper.ThrowInvalidOperationIfFailed(
            ApplicationSelectionDialogProfilePreflight.Validate(profile),
            "ApplicationSelectionDialogProfile");
        return profile;
    }
}
