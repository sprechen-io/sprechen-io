using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Gemeinsamer Einstieg zur Prüfung von Ressourcenkennungen
/// und sprachabhängiger Textabdeckung.
/// </summary>
internal static class VoiceResourceCatalogPreflightValidation
{
    private static readonly VoiceResourceCatalogResourceValidator Validator = new();

    internal static void ValidateResources(
        List<PreflightIssue> errors,
        IReadOnlyList<string> supportedLanguages,
        bool strictLocalization,
        IReadOnlyList<VoiceInteractionResource> resources,
        string path)
    {
        Validator.Validate(errors, supportedLanguages, strictLocalization, resources, path);
    }
}
