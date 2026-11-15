using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormVoice;
using Sprechen.Mda.Contracts.V1.Preflight;

namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Lädt und prüft das Dialogprofil des Formulars vor dem Zwischenspeichern, damit die Sprachinteraktion nur gültige Profile verwendet.
/// </summary>
internal sealed class FormVoiceProfileResourceClient
{
    private readonly ContractsApiResourceStreamClient _resources;
    private readonly ContractsApiJsonStreamReader _jsonReader;
    private readonly ContractsApiPreflightFailureMapper _preflightFailureMapper;
    private readonly ContractsApiResourceCacheKeyFactory _cacheKeyFactory;
    private readonly ContractsApiResourceCache<FormVoiceProfile> _cache = new();

    public FormVoiceProfileResourceClient(
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

    public async Task<FormVoiceProfile?> GetAsync(
        FormContractDocument contractDocument,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contractDocument);

        var reference = contractDocument.Projections?.Voice?.FormVoiceProfile;
        if (reference is null)
        {
            return null;
        }

        var cacheKey = _cacheKeyFactory.Create(reference.FormVoiceProfileId, reference.ProfileVersion);
        return await _cache.GetOrCreateAsync(
                cacheKey,
                ct => LoadAsync(reference.FormVoiceProfileId.Trim(), reference.ProfileVersion.Trim(), ct),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<FormVoiceProfile> LoadAsync(
        string formVoiceProfileId,
        string profileVersion,
        CancellationToken cancellationToken)
    {
        await using var jsonStream = await _resources
            .GetFormVoiceProfileAsync(formVoiceProfileId, profileVersion, cancellationToken)
            .ConfigureAwait(false);
        var profile = await _jsonReader
            .ReadAsync<FormVoiceProfile>(jsonStream, "Failed to deserialize form voice profile.", cancellationToken)
            .ConfigureAwait(false);

        _preflightFailureMapper.ThrowContractsPreflightFailedIfFailed(
            FormVoiceProfilePreflight.Validate(profile),
            "FormVoiceProfile");
        return profile;
    }
}
