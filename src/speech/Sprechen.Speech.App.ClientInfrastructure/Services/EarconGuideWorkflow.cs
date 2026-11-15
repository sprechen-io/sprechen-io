namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Verbindet die sprachlich passende Hinweistoneinführung mit steuerbaren Hörproben. Texte kommen aus dem Katalog, die Wiedergabe übernimmt der Player.
/// </summary>
internal sealed class EarconGuideWorkflow(
    IUserLanguageState userLanguageState,
    IEarconPlayer earconPlayer,
    SettingsVoiceLanguageResolver languageResolver,
    EarconGuideCatalog guideCatalog) : IEarconGuideWorkflow
{
    private readonly IEarconPlayer _earconPlayer =
        earconPlayer ?? throw new ArgumentNullException(nameof(earconPlayer));
    private readonly EarconGuideCatalog _guideCatalog =
        guideCatalog ?? throw new ArgumentNullException(nameof(guideCatalog));
    private readonly SettingsVoiceLanguageResolver _languageResolver =
        languageResolver ?? throw new ArgumentNullException(nameof(languageResolver));
    private readonly IUserLanguageState _userLanguageState =
        userLanguageState ?? throw new ArgumentNullException(nameof(userLanguageState));

    public EarconGuideState GetEarconGuideState()
    {
        return GetEarconGuideState(preferredIsoCode: null);
    }

    public EarconGuideState GetEarconGuideState(string? preferredIsoCode)
    {
        var selectedIsoCode = _languageResolver.ResolveSelectedIsoCode(_userLanguageState, preferredIsoCode);
        return _guideCatalog.CreateState(selectedIsoCode);
    }

    public ValueTask PreviewEarconAsync(EarconId earcon, CancellationToken cancellationToken)
    {
        return _earconPlayer.PreviewAsync(earcon, cancellationToken);
    }

    public ValueTask StopEarconPreviewsAsync(CancellationToken cancellationToken)
    {
        return _earconPlayer.StopAllAsync(cancellationToken);
    }
}
