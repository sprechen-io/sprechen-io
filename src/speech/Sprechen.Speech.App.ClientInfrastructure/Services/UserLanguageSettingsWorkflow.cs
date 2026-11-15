using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Übernimmt Sprach- und Höflichkeitswahl in Sitzungszustand und Speicherung.
/// Den Marker einer beantworteten Einstiegsentscheidung aktualisiert der Ablauf
/// gesondert über den Speicherzugang.
/// </summary>
internal sealed class UserLanguageSettingsWorkflow(
    IUserLanguageState userLanguageState,
    IUserLanguageSettingsStore userLanguageSettingsStore,
    SettingsVoiceLanguageResolver languageResolver) :
    IUserLanguageSelectionWriter,
    IUserLanguageSettingsWorkflow,
    ISessionSetupSettingsWorkflow
{
    private readonly SettingsVoiceLanguageResolver _languageResolver =
        languageResolver ?? throw new ArgumentNullException(nameof(languageResolver));
    private readonly IUserLanguageSettingsStore _userLanguageSettingsStore =
        userLanguageSettingsStore ?? throw new ArgumentNullException(nameof(userLanguageSettingsStore));
    private readonly IUserLanguageState _userLanguageState =
        userLanguageState ?? throw new ArgumentNullException(nameof(userLanguageState));

    public IReadOnlyList<string> SupportedLanguages => _languageResolver.SupportedLanguages;

    public SessionSetupSettingsState GetSessionSetupState()
    {
        return _languageResolver.CreateSessionSetupState(
            _userLanguageState.SelectedIsoCode,
            _userLanguageState.SelectedPolitenessLevel);
    }

    public SessionSetupSettingsState SelectSessionLanguage(string? selectedIsoCode, string? selectedPolitenessValue)
    {
        return _languageResolver.CreateSessionSetupState(selectedIsoCode, selectedPolitenessValue);
    }

    public ValueTask<UserLanguageSettings> SaveSelectedLanguageAsync(
        string? selectedIsoCode,
        CancellationToken cancellationToken)
    {
        var state = _languageResolver.CreateSessionSetupState(
            selectedIsoCode,
            _userLanguageState.SelectedPolitenessLevel);
        return ApplyUserLanguageSettingsAsync(state, cancellationToken);
    }

    public ValueTask<UserLanguageSettings> SaveSelectedPolitenessAsync(
        PolitenessLevel? selectedPolitenessLevel,
        CancellationToken cancellationToken)
    {
        var state = _languageResolver.CreateSessionSetupState(
            _userLanguageState.SelectedIsoCode,
            selectedPolitenessLevel);
        return ApplyUserLanguageSettingsAsync(state, cancellationToken);
    }

    public async ValueTask SaveEntryOfferHeardAsync(string isoCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        // Nur den Marker der beantworteten Einstiegsentscheidung aktualisieren. Ein lokales Lesen und Zurückschreiben
        // könnte bei fehlgeschlagenem Lesen die gespeicherte Sprache überschreiben.
        // Das Zusammenführen gehört deshalb in den Speicherzugang.
        await _userLanguageSettingsStore
            .SetEntryOfferHeardAsync(isoCode, cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask<string?> ReadEntryOfferHeardIsoCodeAsync(CancellationToken cancellationToken)
    {
        var stored = await _userLanguageSettingsStore.GetAsync(cancellationToken).ConfigureAwait(false);
        return stored?.EntryOfferHeardIsoCode;
    }

    public async ValueTask<SessionSetupSettingsState> SaveSessionSetupAsync(
        string? selectedIsoCode,
        string? selectedPolitenessValue,
        CancellationToken cancellationToken)
    {
        var state = _languageResolver.CreateSessionSetupState(selectedIsoCode, selectedPolitenessValue);
        await ApplyUserLanguageSettingsAsync(state, cancellationToken).ConfigureAwait(false);
        return state;
    }

    private async ValueTask<UserLanguageSettings> ApplyUserLanguageSettingsAsync(
        SessionSetupSettingsState state,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);

        var politenessLevel = _languageResolver.ParseSelectedPolitenessLevel(
            state.SelectedPolitenessValue,
            state.SelectedIsoCode);
        // Den gelesenen Einstiegsvermerk übernehmen, damit eine neue Sprachauswahl
        // den Marker der beantworteten Einstiegsentscheidung nicht bewusst verwirft.
        var stored = await _userLanguageSettingsStore.GetAsync(cancellationToken).ConfigureAwait(false);
        var settings = new UserLanguageSettings(
            state.SelectedIsoCode,
            politenessLevel,
            stored?.EntryOfferHeardIsoCode);

        _userLanguageState.SetSelectedIsoCode(state.SelectedIsoCode);
        _userLanguageState.SetSelectedPolitenessLevel(politenessLevel);

        await _userLanguageSettingsStore
            .SetAsync(settings, cancellationToken)
            .ConfigureAwait(false);

        return settings;
    }
}
