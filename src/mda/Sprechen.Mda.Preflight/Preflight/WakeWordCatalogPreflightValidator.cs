using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Koordiniert die Prüfung von Sprachprofilen, Aktivierungsformen
/// und Einstellungen eines eigenständigen Aktivierungswortkatalogs.
/// </summary>
internal sealed class WakeWordCatalogPreflightValidator
{
    private readonly WakeWordCatalogSettingsValidator _settingsValidator = new();

    public PreflightResult Validate(WakeWordCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var errors = new List<PreflightIssue>();
        var languageCodes = catalog.ActivationProfilesByLanguage.Select(profile => profile.LanguageCode).ToArray();

        VoiceLanguageProfilePreflightValidation.ValidateLanguageProfiles(
            errors,
            languageCodes.Distinct(StringComparer.Ordinal).ToArray(),
            languageCodes,
            "$.activationProfilesByLanguage",
            profileKind: "wake-word profile");

        // Namen sind sprachabhängig, Einleitungswörter gemeinsam modelliert.
        // Der Eintragsvalidator prüft die zusammengesetzten Formen auf leere Werte,
        // Duplikate und Überschneidungen zwischen angebotenen und nur zusätzlich erkannten Formen.
        WakeWordCatalogProfilePreflightValidation.ValidateActivationEntries(
            errors,
            catalog,
            "$",
            catalog.WakeWordModelId);

        _settingsValidator.Validate(errors, catalog);

        return new PreflightResult(errors, []);
    }
}
