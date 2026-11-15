using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class FixedMdaFormContractDataClient : IMdaFormContractDataClient
{
    private readonly FormContract _contract;

    public FixedMdaFormContractDataClient(FormContract contract)
    {
        _contract = contract ?? throw new ArgumentNullException(nameof(contract));
    }

    public Task<FormContract> GetFormContractAsync(
        string applicationId,
        string formId,
        string contractVersion,
        CancellationToken cancellationToken)
        => Task.FromResult(_contract);
}



