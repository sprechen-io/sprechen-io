using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Gemeinsame Einstiege zur Prüfung von Aktivierungswortprofilen
/// und ihren zusammengesetzten Aktivierungsformen.
/// </summary>
internal static class WakeWordCatalogProfilePreflightValidation
{
    private static readonly WakeWordActivationEntryValidator EntryValidator = new();
    private static readonly WakeWordCatalogProfilePreflightValidator ProfileValidator = new();

    internal static void ValidateCatalog(
        List<PreflightIssue> errors,
        IReadOnlyList<string> supportedLanguages,
        WakeWordCatalog wakeWordCatalog,
        string path)
    {
        ProfileValidator.ValidateCatalog(errors, supportedLanguages, wakeWordCatalog, path);
    }

    internal static void ValidateActivationEntries(
        List<PreflightIssue> errors,
        WakeWordCatalog catalog,
        string basePath,
        string? catalogId)
    {
        EntryValidator.Validate(errors, catalog, basePath, catalogId);
    }
}
