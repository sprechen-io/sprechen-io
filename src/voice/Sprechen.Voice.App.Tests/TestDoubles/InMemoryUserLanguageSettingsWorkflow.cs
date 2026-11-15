using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Voice.App.Tests.TestDoubles;

internal sealed class InMemoryUserLanguageSettingsWorkflow(
    IUserLanguageState userLanguageState,
    IUserLanguageSettingsStore settingsStore) :
    IUserLanguageSelectionWriter,
    IUserLanguageSettingsWorkflow
{
    public ValueTask SaveEntryOfferHeardAsync(string isoCode, CancellationToken cancellationToken)
        // Die gesonderte Operation lässt die übrigen Einstellungen unverändert.
        => settingsStore.SetEntryOfferHeardAsync(isoCode, cancellationToken);

    public async ValueTask<string?> ReadEntryOfferHeardIsoCodeAsync(CancellationToken cancellationToken)
        => (await settingsStore.GetAsync(cancellationToken).ConfigureAwait(false)).EntryOfferHeardIsoCode;

    public async ValueTask<UserLanguageSettings> SaveSelectedLanguageAsync(
        string? selectedIsoCode,
        CancellationToken cancellationToken)
    {
        var isoCode = NormalizeIsoCode(selectedIsoCode);
        var politenessLevel = ResolvePolitenessLevel(isoCode, userLanguageState.SelectedPolitenessLevel);
        var heardIn = (await settingsStore.GetAsync(cancellationToken).ConfigureAwait(false))
            .EntryOfferHeardIsoCode;
        var settings = new UserLanguageSettings(isoCode, politenessLevel, heardIn);
        await settingsStore.SetAsync(settings, cancellationToken).ConfigureAwait(false);
        userLanguageState.SetSelectedIsoCode(isoCode);
        userLanguageState.SetSelectedPolitenessLevel(politenessLevel);
        return settings;
    }

    public async ValueTask<UserLanguageSettings> SaveSelectedPolitenessAsync(
        PolitenessLevel? selectedPolitenessLevel,
        CancellationToken cancellationToken)
    {
        var isoCode = NormalizeIsoCode(userLanguageState.SelectedIsoCode);
        var politenessLevel = ResolvePolitenessLevel(isoCode, selectedPolitenessLevel);
        var heardIn = (await settingsStore.GetAsync(cancellationToken).ConfigureAwait(false))
            .EntryOfferHeardIsoCode;
        var settings = new UserLanguageSettings(isoCode, politenessLevel, heardIn);
        await settingsStore.SetAsync(settings, cancellationToken).ConfigureAwait(false);
        userLanguageState.SetSelectedIsoCode(isoCode);
        userLanguageState.SetSelectedPolitenessLevel(politenessLevel);
        return settings;
    }

    internal static string NormalizeIsoCode(string? isoCode)
    {
        return string.IsNullOrWhiteSpace(isoCode)
            ? VoiceInterfaceModelCatalog.DefaultLanguage
            : isoCode.Trim().ToLowerInvariant();
    }

    internal static PolitenessLevel? ResolvePolitenessLevel(string isoCode, PolitenessLevel? selectedPolitenessLevel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        if (!VoiceInterfaceModelCatalog.Lexicon.Politeness.HasProfile(isoCode))
        {
            return null;
        }

        return selectedPolitenessLevel ?? PolitenessLevel.Formal;
    }

    internal static PolitenessLevel? ParsePolitenessLevel(string isoCode, string? selectedPolitenessValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        if (!VoiceInterfaceModelCatalog.Lexicon.Politeness.HasProfile(isoCode))
        {
            return null;
        }

        return string.Equals(selectedPolitenessValue?.Trim(), "informal", StringComparison.OrdinalIgnoreCase)
            ? PolitenessLevel.Informal
            : PolitenessLevel.Formal;
    }

    internal static string ToPolitenessValue(string isoCode, PolitenessLevel? selectedPolitenessLevel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        if (!VoiceInterfaceModelCatalog.Lexicon.Politeness.HasProfile(isoCode))
        {
            return string.Empty;
        }

        return selectedPolitenessLevel == PolitenessLevel.Informal
            ? "informal"
            : "formal";
    }
}
