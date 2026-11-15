namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Gleicht referenzierte und geladene Artefakte ab, damit fehlende oder unreferenzierte Bestandteile die Bereitstellung verhindern.
/// </summary>
internal sealed class MdaReferenceArtifactPresenceValidator
{
    public void EnsureReferencedContractsExist(
        IReadOnlySet<FormContractKey> referencedContractKeys,
        IReadOnlyDictionary<FormContractKey, MdaJsonArtifact> contracts)
    {
        ArgumentNullException.ThrowIfNull(referencedContractKeys);
        ArgumentNullException.ThrowIfNull(contracts);

        foreach (var key in referencedContractKeys)
        {
            if (contracts.ContainsKey(key))
            {
                continue;
            }

            throw new InvalidOperationException(
                $"Catalog references missing FormContract (applicationId='{key.ApplicationId}', formId='{key.FormId}', contractVersion='{key.ContractVersion}').");
        }
    }

    public void EnsureReferencedArtifactsExist<TKey>(
        IReadOnlySet<TKey> referencedKeys,
        IReadOnlyDictionary<TKey, MdaJsonArtifact> artifacts,
        string artifactName,
        Func<TKey, string> formatKey)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(referencedKeys);
        ArgumentNullException.ThrowIfNull(artifacts);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactName);
        ArgumentNullException.ThrowIfNull(formatKey);

        foreach (var key in referencedKeys)
        {
            if (artifacts.ContainsKey(key))
            {
                continue;
            }

            throw new InvalidOperationException($"Catalog references missing {artifactName} ({formatKey(key)}).");
        }
    }

    public void EnsureNoUnreferencedArtifacts<TKey>(
        IEnumerable<TKey> availableKeys,
        IEnumerable<TKey> referencedKeys,
        string artifactName,
        Func<TKey, string> formatKey)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(availableKeys);
        ArgumentNullException.ThrowIfNull(referencedKeys);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactName);
        ArgumentNullException.ThrowIfNull(formatKey);

        var referenced = referencedKeys.ToHashSet();
        foreach (var key in availableKeys)
        {
            if (referenced.Contains(key))
            {
                continue;
            }

            throw new InvalidOperationException($"Runtime bundle contains {artifactName} not referenced ({formatKey(key)}).");
        }
    }
}
