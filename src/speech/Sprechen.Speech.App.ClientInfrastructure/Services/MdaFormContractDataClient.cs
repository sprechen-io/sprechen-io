using Sprechen.Contracts.ApiClient;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Vermittelt den Abruf eines versionierten Formularvertrags des Informationsmodells
/// über den typisierten HTTP-Client des Vertragsdienstes.
/// </summary>
public sealed class MdaFormContractDataClient : IMdaFormContractDataClient
{
    private readonly MdaFormContractTypedClient _typedClient;

    public MdaFormContractDataClient(MdaFormContractTypedClient typedClient)
    {
        _typedClient = typedClient ?? throw new ArgumentNullException(nameof(typedClient));
    }

    public Task<FormContract> GetFormContractAsync(
        string applicationId,
        string formId,
        string contractVersion,
        CancellationToken cancellationToken)
    {
        return _typedClient.GetFormContractAsync(applicationId, formId, contractVersion, cancellationToken);
    }
}

