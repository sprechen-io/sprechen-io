using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft doppelte Ressourcenkennungen und delegiert die konfigurierte
/// Übersetzungsprüfung der Ressourcentexte.
/// </summary>
internal sealed class VoiceResourceCatalogResourceValidator
{
    private readonly VoiceResourceLocalizationCoverageValidator _localizationValidator = new();

    public void Validate(
        List<PreflightIssue> errors,
        IReadOnlyList<string> supportedLanguages,
        bool strictLocalization,
        IReadOnlyList<VoiceInteractionResource> resources,
        string path)
    {
        var resourceIds = new HashSet<string>(StringComparer.Ordinal);
        for (var resourceIndex = 0; resourceIndex < resources.Count; resourceIndex++)
        {
            var resource = resources[resourceIndex];
            ValidateResourceId(errors, resourceIds, resource, $"{path}[{resourceIndex}].id");
            _localizationValidator.Validate(
                errors,
                supportedLanguages,
                strictLocalization,
                resource.Texts,
                $"{path}[{resourceIndex}].texts",
                resource.Id);
        }
    }

    private static void ValidateResourceId(
        List<PreflightIssue> errors,
        HashSet<string> resourceIds,
        VoiceInteractionResource resource,
        string path)
    {
        if (resourceIds.Add(resource.Id))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorIdDuplicate,
            Message: $"Duplicate voice resource id '{resource.Id}'.",
            Path: path,
            Id: resource.Id));
    }
}
