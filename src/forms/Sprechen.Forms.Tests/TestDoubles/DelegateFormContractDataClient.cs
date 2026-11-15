using Sprechen.Forms.Services;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Forms.Tests.TestDoubles;

internal sealed class DelegateFormContractDataClient : IMdaFormContractDataClient
{
    private readonly Func<string, string, string, CancellationToken, Task<FormContract>> _getAsync;

    public DelegateFormContractDataClient(Func<string, string, string, CancellationToken, Task<FormContract>> getAsync)
    {
        _getAsync = getAsync ?? throw new ArgumentNullException(nameof(getAsync));
    }

    public Task<FormContract> GetFormContractAsync(
        string applicationId,
        string formId,
        string contractVersion,
        CancellationToken cancellationToken)
        => _getAsync(applicationId, formId, contractVersion, cancellationToken);
}

