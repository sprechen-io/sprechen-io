using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft die genaue Sprachabdeckung des Aktivierungswortkatalogs
/// und die daraus zusammengesetzten Aktivierungsformen.
/// </summary>
internal sealed class WakeWordCatalogProfilePreflightValidator
{
    private readonly WakeWordActivationEntryValidator _entryValidator = new();

    public void ValidateCatalog(
        List<PreflightIssue> errors,
        IReadOnlyList<string> supportedLanguages,
        WakeWordCatalog wakeWordCatalog,
        string path)
    {
        VoiceLanguageProfilePreflightValidation.ValidateLanguageProfilesExactMatch(
            errors,
            supportedLanguages,
            wakeWordCatalog.ActivationProfilesByLanguage.Select(profile => profile.LanguageCode).ToArray(),
            $"{path}.activationProfilesByLanguage",
            wakeWordCatalog.WakeWordModelId,
            profileKind: "wake-word profile");

        // Sprachabhängige Namen werden mit den gemeinsamen Einleitungswörtern verbunden.
        // Der Eintragsvalidator prüft die daraus entstehenden Formen.
        _entryValidator.Validate(
            errors,
            wakeWordCatalog,
            path,
            wakeWordCatalog.WakeWordModelId);
    }
}
