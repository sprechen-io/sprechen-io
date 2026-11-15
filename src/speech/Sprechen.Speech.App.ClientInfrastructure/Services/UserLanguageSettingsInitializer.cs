using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Initialisiert die Sitzungssprache aus gültiger Speicherung, bevorzugten
/// Browsersprachen oder dem Modellstandard und übernimmt passende Höflichkeitseinstellungen.
/// </summary>
public sealed class UserLanguageSettingsInitializer(
    IUserLanguageSettingsStore userLanguageSettingsStore,
    IUserLanguageState userLanguageState,
    IBrowserLanguageProvider browserLanguageProvider,
    ILogger<UserLanguageSettingsInitializer> logger)
{
    private readonly IUserLanguageSettingsStore _userLanguageSettingsStore =
        userLanguageSettingsStore ?? throw new ArgumentNullException(nameof(userLanguageSettingsStore));
    private readonly IUserLanguageState _userLanguageState =
        userLanguageState ?? throw new ArgumentNullException(nameof(userLanguageState));
    private readonly IBrowserLanguageProvider _browserLanguageProvider =
        browserLanguageProvider ?? throw new ArgumentNullException(nameof(browserLanguageProvider));
    private readonly ILogger<UserLanguageSettingsInitializer> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async ValueTask InitializeAsync(CancellationToken cancellationToken)
    {
        var settings = await _userLanguageSettingsStore.GetAsync(cancellationToken).ConfigureAwait(false);
        var normalizedSelectedIsoCode = NormalizeSelectedIsoCode(settings.SelectedIsoCode);
        var selectedIsoCode = normalizedSelectedIsoCode;
        if (selectedIsoCode is not null)
        {
            _logger.LogDebug("Applying persisted user language '{IsoCode}'.", selectedIsoCode);
        }
        else
        {
            selectedIsoCode = await ResolveEnvironmentSelectedIsoCodeAsync(cancellationToken).ConfigureAwait(false);
        }

        _userLanguageState.SetSelectedIsoCode(selectedIsoCode);
        _userLanguageState.SetSelectedPolitenessLevel(
            normalizedSelectedIsoCode is not null
            && VoiceInterfaceModelCatalog.Lexicon.Politeness.HasProfile(selectedIsoCode)
                ? settings.SelectedPolitenessLevel
                : null);
    }

    private async ValueTask<string> ResolveEnvironmentSelectedIsoCodeAsync(CancellationToken cancellationToken)
    {
        var preferredLanguageTags = await _browserLanguageProvider
            .GetPreferredLanguageTagsAsync(cancellationToken)
            .ConfigureAwait(false);
        var resolvedIsoCode = SupportedLanguageResolver.ResolvePreferredIsoCode(preferredLanguageTags);
        if (resolvedIsoCode is not null)
        {
            _logger.LogDebug(
                "Applying browser/environment language '{IsoCode}' from preferred tags: {LanguageTags}.",
                resolvedIsoCode,
                preferredLanguageTags.Count == 0
                    ? "<none>"
                    : string.Join(", ", preferredLanguageTags));
            return resolvedIsoCode;
        }

        _logger.LogDebug(
            "No supported browser/environment language was found in preferred tags: {LanguageTags}. Falling back to modeled default '{IsoCode}'.",
            preferredLanguageTags.Count == 0
                ? "<none>"
                : string.Join(", ", preferredLanguageTags),
            VoiceInterfaceModelCatalog.DefaultLanguage);
        return VoiceInterfaceModelCatalog.DefaultLanguage;
    }

    private static string? NormalizeSelectedIsoCode(string? selectedIsoCode)
    {
        var normalizedIsoCode = IsoCode.TryNormalizeIso6391(selectedIsoCode);
        if (normalizedIsoCode is null)
        {
            return null;
        }

        return VoiceInterfaceModelCatalog.SupportedLanguages.Contains(normalizedIsoCode, StringComparer.Ordinal)
            ? normalizedIsoCode
            : null;
    }
}
