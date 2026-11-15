using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Voice.App.Tests.TestDoubles;

internal sealed class InMemorySessionSetupSettingsWorkflow(
    IUserLanguageState userLanguageState,
    IUserLanguageSettingsStore settingsStore) : ISessionSetupSettingsWorkflow
{
    private static readonly VoiceInterfaceLanguageCatalog LanguageCatalog = VoiceInterfaceLanguageCatalog.CreateDefault();

    public IReadOnlyList<string> SupportedLanguages { get; } = VoiceInterfaceModelCatalog.SupportedLanguages
        .OrderBy(GetEndonym, StringComparer.InvariantCultureIgnoreCase)
        .ToArray();

    public SessionSetupSettingsState GetSessionSetupState()
    {
        return CreateState(userLanguageState.SelectedIsoCode, userLanguageState.SelectedPolitenessLevel);
    }

    public SessionSetupSettingsState SelectSessionLanguage(string? selectedIsoCode, string? selectedPolitenessValue)
    {
        return CreateState(selectedIsoCode, selectedPolitenessValue);
    }

    public async ValueTask<SessionSetupSettingsState> SaveSessionSetupAsync(
        string? selectedIsoCode,
        string? selectedPolitenessValue,
        CancellationToken cancellationToken)
    {
        var isoCode = InMemoryUserLanguageSettingsWorkflow.NormalizeIsoCode(selectedIsoCode);
        var politeness = InMemoryUserLanguageSettingsWorkflow.ParsePolitenessLevel(isoCode, selectedPolitenessValue);
        var settings = new UserLanguageSettings(isoCode, politeness);
        await settingsStore.SetAsync(settings, cancellationToken).ConfigureAwait(false);
        userLanguageState.SetSelectedIsoCode(isoCode);
        userLanguageState.SetSelectedPolitenessLevel(politeness);
        return CreateState(isoCode, politeness);
    }

    private static SessionSetupSettingsState CreateState(string? isoCode, PolitenessLevel? politeness)
    {
        var selectedIsoCode = InMemoryUserLanguageSettingsWorkflow.NormalizeIsoCode(isoCode);
        var selectedPolitenessLevel = InMemoryUserLanguageSettingsWorkflow.ResolvePolitenessLevel(
            selectedIsoCode,
            politeness);
        return new SessionSetupSettingsState(
            selectedIsoCode,
            InMemoryUserLanguageSettingsWorkflow.ToPolitenessValue(selectedIsoCode, selectedPolitenessLevel),
            VoiceInterfaceModelCatalog.Lexicon.Politeness.HasProfile(selectedIsoCode));
    }

    private static SessionSetupSettingsState CreateState(string? isoCode, string? politenessValue)
    {
        var selectedIsoCode = InMemoryUserLanguageSettingsWorkflow.NormalizeIsoCode(isoCode);
        var selectedPolitenessLevel = InMemoryUserLanguageSettingsWorkflow.ParsePolitenessLevel(
            selectedIsoCode,
            politenessValue);

        return new SessionSetupSettingsState(
            selectedIsoCode,
            InMemoryUserLanguageSettingsWorkflow.ToPolitenessValue(selectedIsoCode, selectedPolitenessLevel),
            VoiceInterfaceModelCatalog.Lexicon.Politeness.HasProfile(selectedIsoCode));
    }

    private static string GetEndonym(string isoCode)
    {
        return LanguageCatalog.GetEndonym(LanguageCode.ParseIso6391(isoCode));
    }
}
