using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Gemeinsamer Einstieg für die eigenständige und kontextabhängige
/// Prüfung allgemeiner Ansagenkataloge.
/// </summary>
internal static class GenericPromptCatalogPreflightValidation
{
    private static readonly GenericPromptCatalogPreflightValidator Validator = new();

    internal static void ValidateStandaloneCatalog(
        List<PreflightIssue> errors,
        GenericPromptCatalog catalog)
    {
        Validator.ValidateStandaloneCatalog(errors, catalog);
    }

    internal static void ValidatePromptCatalog(
        List<PreflightIssue> errors,
        IReadOnlyList<string> supportedLanguages,
        string defaultLanguage,
        bool strictLocalization,
        GenericPromptCatalog promptCatalog,
        string path)
    {
        Validator.ValidatePromptCatalog(errors, supportedLanguages, defaultLanguage, strictLocalization, promptCatalog, path);
    }
}
