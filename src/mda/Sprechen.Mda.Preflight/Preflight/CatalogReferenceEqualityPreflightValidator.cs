namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Vergleicht Kennung und Version einer Katalogreferenz ordinal
/// mit den Werten des geladenen Katalogs.
/// </summary>
internal sealed class CatalogReferenceEqualityPreflightValidator
{
    public void Validate(
        List<PreflightIssue> errors,
        string referencedModelId,
        string loadedModelId,
        string referencedVersion,
        string loadedVersion,
        string modelIdPath,
        string versionPath,
        string modelIdNoun,
        string versionNoun)
    {
        if (!string.Equals(referencedModelId, loadedModelId, StringComparison.Ordinal))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorRefMissing,
                Message: $"VoiceInterface references {modelIdNoun} '{referencedModelId}', but loaded catalog is '{loadedModelId}'.",
                Path: modelIdPath,
                Id: referencedModelId));
        }

        if (!string.Equals(referencedVersion, loadedVersion, StringComparison.Ordinal))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorRefMissing,
                Message: $"VoiceInterface references {versionNoun} '{referencedVersion}', but loaded catalog is '{loadedVersion}'.",
                Path: versionPath,
                Id: referencedVersion));
        }
    }
}
