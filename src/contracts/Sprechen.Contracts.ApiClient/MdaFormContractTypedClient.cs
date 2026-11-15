using Sprechen.Contracts.MdaFormAccess;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Preflight;

namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Führt Formularvertrag, Feld- und Variablendefinitionen sowie das optionale Dialogprofil zusammen und prüft sie, damit Aufrufer einen aufgelösten Vertrag erhalten.
/// </summary>
public sealed class MdaFormContractTypedClient : IMdaFormContractDataClient
{
    private readonly ContractsApiResourceCache<FormContract> _contractCache = new();
    private readonly ContractsApiResourceCacheKeyFactory _cacheKeyFactory;
    private readonly FormContractDocumentResourceClient _formContractDocumentClient;
    private readonly FieldDefinitionsResourceClient _fieldDefinitionsClient;
    private readonly VariableDefinitionsResourceClient _variableDefinitionsClient;
    private readonly FormVoiceProfileResourceClient _formVoiceProfileClient;
    private readonly ContractsApiPreflightFailureMapper _preflightFailureMapper;

    internal MdaFormContractTypedClient(
        ContractsApiResourceCacheKeyFactory cacheKeyFactory,
        FormContractDocumentResourceClient formContractDocumentClient,
        FieldDefinitionsResourceClient fieldDefinitionsClient,
        VariableDefinitionsResourceClient variableDefinitionsClient,
        FormVoiceProfileResourceClient formVoiceProfileClient,
        ContractsApiPreflightFailureMapper preflightFailureMapper)
    {
        _cacheKeyFactory = cacheKeyFactory ?? throw new ArgumentNullException(nameof(cacheKeyFactory));
        _formContractDocumentClient = formContractDocumentClient ?? throw new ArgumentNullException(nameof(formContractDocumentClient));
        _fieldDefinitionsClient = fieldDefinitionsClient ?? throw new ArgumentNullException(nameof(fieldDefinitionsClient));
        _variableDefinitionsClient = variableDefinitionsClient ?? throw new ArgumentNullException(nameof(variableDefinitionsClient));
        _formVoiceProfileClient = formVoiceProfileClient ?? throw new ArgumentNullException(nameof(formVoiceProfileClient));
        _preflightFailureMapper = preflightFailureMapper ?? throw new ArgumentNullException(nameof(preflightFailureMapper));
    }

    public Task<FormContract> GetFormContractAsync(
        string applicationId,
        string formId,
        string contractVersion,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(formId);
        ArgumentException.ThrowIfNullOrWhiteSpace(contractVersion);

        var normalizedApplicationId = applicationId.Trim();
        var normalizedFormId = formId.Trim();
        var normalizedContractVersion = contractVersion.Trim();
        var cacheKey = _cacheKeyFactory.Create(
            normalizedApplicationId,
            normalizedFormId,
            normalizedContractVersion);

        return _contractCache.GetOrCreateAsync(
            cacheKey,
            ct => LoadFormContractAsync(
                normalizedApplicationId,
                normalizedFormId,
                normalizedContractVersion,
                ct),
            cancellationToken);
    }

    private async Task<FormContract> LoadFormContractAsync(
        string applicationId,
        string formId,
        string contractVersion,
        CancellationToken cancellationToken)
    {
        var contractDocument = await _formContractDocumentClient
            .LoadAsync(applicationId, formId, contractVersion, cancellationToken)
            .ConfigureAwait(false);
        var fieldCatalog = await _fieldDefinitionsClient
            .GetAsync(
                contractDocument.FieldDefinitions.FieldDefinitionsId,
                contractDocument.FieldDefinitions.CatalogVersion,
                cancellationToken)
            .ConfigureAwait(false);
        var variableCatalog = await _variableDefinitionsClient
            .GetAsync(
                contractDocument.VariableDefinitions.VariableDefinitionsId,
                contractDocument.VariableDefinitions.CatalogVersion,
                cancellationToken)
            .ConfigureAwait(false);
        var formVoiceProfile = await _formVoiceProfileClient
            .GetAsync(contractDocument, cancellationToken)
            .ConfigureAwait(false);

        _preflightFailureMapper.ThrowContractsPreflightFailedIfFailed(
            FormContractDocumentPreflight.Validate(contractDocument, fieldCatalog, variableCatalog, formVoiceProfile),
            "FormContract document");

        var contract = FormContractResolver.Resolve(contractDocument, fieldCatalog, variableCatalog, formVoiceProfile);
        _preflightFailureMapper.ThrowContractsPreflightFailedIfFailed(
            FormContractPreflight.Validate(contract),
            "FormContract");
        return contract;
    }
}
