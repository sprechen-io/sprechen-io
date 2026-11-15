using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class MapMdaFormContractDataClient : IMdaFormContractDataClient
{
    private readonly IReadOnlyDictionary<string, FormContract> _contractsByKey;

    public MapMdaFormContractDataClient(IEnumerable<FormContract> contracts)
    {
        ArgumentNullException.ThrowIfNull(contracts);

        _contractsByKey = contracts
            .Where(contract => contract is not null
                && !string.IsNullOrWhiteSpace(contract.ApplicationId)
                && !string.IsNullOrWhiteSpace(contract.FormId)
                && !string.IsNullOrWhiteSpace(contract.ContractVersion))
            .ToDictionary(
                contract => Key(contract.ApplicationId, contract.FormId, contract.ContractVersion),
                contract => contract,
                StringComparer.OrdinalIgnoreCase);
    }

    public Task<FormContract> GetFormContractAsync(
        string applicationId,
        string formId,
        string contractVersion,
        CancellationToken cancellationToken)
    {
        var key = Key(applicationId, formId, contractVersion);

        if (!_contractsByKey.TryGetValue(key, out var contract))
        {
            throw new InvalidOperationException($"No form contract is configured for '{key}'.");
        }

        return Task.FromResult(contract);
    }

    private static string Key(string applicationId, string formId, string contractVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(formId);
        ArgumentException.ThrowIfNullOrWhiteSpace(contractVersion);

        return $"{applicationId.Trim()}/{formId.Trim()}/{contractVersion.Trim()}";
    }
}



