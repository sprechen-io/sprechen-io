using Sprechen.Contracts.ApiClient;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Forms.Services;

/// <summary>
/// Lädt einen Formularvertrag vom Contracts-Dienst und wandelt dessen Preflight-Fehler in die eigene Ladeausnahme um.
/// </summary>
internal sealed class MdaFormContractDataClient : IMdaFormContractDataClient
{
    private readonly MdaFormContractTypedClient _typedClient;

    public MdaFormContractDataClient(MdaFormContractTypedClient typedClient)
    {
        _typedClient = typedClient ?? throw new ArgumentNullException(nameof(typedClient));
    }

    public async Task<FormContract> GetFormContractAsync(
        string applicationId,
        string formId,
        string contractVersion,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _typedClient
                .GetFormContractAsync(applicationId, formId, contractVersion, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ContractsPreflightFailedException ex)
        {
            throw new FormContractLoadFailedException(ex.Message, ex.Issues);
        }
    }
}

