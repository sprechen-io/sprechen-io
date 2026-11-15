using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Speech.ModelRuntime.Services;

/// <summary>
/// Bereitet modellkonforme Sprach- und Höflichkeitswerte für die Einstellungsoberfläche vor. Änderungen am Sitzungszustand und Speicherung gehören zum aufrufenden Ablauf.
/// </summary>
public sealed class SettingsVoiceLanguageResolver
{
    private readonly VoiceInterfaceLanguageCatalog _languageCatalog = VoiceInterfaceLanguageCatalog.CreateDefault();
    private readonly string[] _orderedSupportedLanguages;

    public SettingsVoiceLanguageResolver()
    {
        _orderedSupportedLanguages = VoiceInterfaceModelCatalog.SupportedLanguages
            .OrderBy(
                language => _languageCatalog.GetEndonym(LanguageCode.ParseIso6391(language)),
                StringComparer.InvariantCultureIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<string> SupportedLanguages => _orderedSupportedLanguages;

    public string ResolveSelectedIsoCode(IUserLanguageState userLanguageState, string? preferredIsoCode = null)
    {
        ArgumentNullException.ThrowIfNull(userLanguageState);

        return NormalizeSelectedIsoCode(preferredIsoCode)
            ?? NormalizeSelectedIsoCode(userLanguageState.SelectedIsoCode)
            ?? VoiceInterfaceModelCatalog.DefaultLanguage;
    }

    public SessionSetupSettingsState CreateSessionSetupState(
        string? selectedIsoCode,
        PolitenessLevel? selectedPolitenessLevel)
    {
        var normalizedIsoCode = NormalizeSelectedIsoCode(selectedIsoCode) ?? VoiceInterfaceModelCatalog.DefaultLanguage;
        return CreateSessionSetupState(normalizedIsoCode, GetInitialPolitenessValue(normalizedIsoCode, selectedPolitenessLevel));
    }

    public SessionSetupSettingsState CreateSessionSetupState(
        string? selectedIsoCode,
        string? selectedPolitenessValue)
    {
        var normalizedIsoCode = NormalizeSelectedIsoCode(selectedIsoCode) ?? VoiceInterfaceModelCatalog.DefaultLanguage;
        var hasPolitenessProfile = VoiceInterfaceModelCatalog.Lexicon.Politeness.HasProfile(normalizedIsoCode);
        var normalizedPolitenessValue = hasPolitenessProfile
            ? string.Equals(selectedPolitenessValue, "informal", StringComparison.Ordinal)
                ? "informal"
                : "formal"
            : string.Empty;

        return new SessionSetupSettingsState(
            normalizedIsoCode,
            normalizedPolitenessValue,
            hasPolitenessProfile);
    }

    public PolitenessLevel? ParseSelectedPolitenessLevel(string? value, string isoCode)
    {
        if (!VoiceInterfaceModelCatalog.Lexicon.Politeness.HasProfile(isoCode))
        {
            return null;
        }

        return value switch
        {
            "informal" => PolitenessLevel.Informal,
            _ => PolitenessLevel.Formal
        };
    }

    public string ResolveRequiredResourceText(string resourceId, string isoCode)
    {
        if (VoiceInterfaceModelCatalog.Resources.TryGetResourceText(resourceId, isoCode, out var text))
        {
            return text;
        }

        throw new InvalidOperationException(
            $"Voice interface resource '{resourceId}' is missing text for '{isoCode}'.");
    }

    private string? NormalizeSelectedIsoCode(string? value)
    {
        var normalizedIsoCode = IsoCode.TryNormalizeIso6391(value);
        if (normalizedIsoCode is null)
        {
            return null;
        }

        return _orderedSupportedLanguages.Contains(normalizedIsoCode, StringComparer.Ordinal)
            ? normalizedIsoCode
            : null;
    }

    private string GetInitialPolitenessValue(string? selectedIsoCode, PolitenessLevel? selectedPolitenessLevel)
    {
        var normalizedIsoCode = NormalizeSelectedIsoCode(selectedIsoCode) ?? VoiceInterfaceModelCatalog.DefaultLanguage;
        if (!VoiceInterfaceModelCatalog.Lexicon.Politeness.HasProfile(normalizedIsoCode))
        {
            return string.Empty;
        }

        return selectedPolitenessLevel == PolitenessLevel.Informal
            ? "informal"
            : "formal";
    }
}
