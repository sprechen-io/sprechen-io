namespace Sprechen.Voice.App.Services;

/// <summary>
/// Liefert lokalisierte Texte für Navigation, Statusmeldungen und den Start
/// der Sprach-App aus dem Katalog der Oberflächentexte.
/// </summary>
internal sealed class VoiceChromeCopy
{
    private readonly VoiceSettingsCopyResolver _resolver;

    internal VoiceChromeCopy(VoiceSettingsCopyResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    internal string GetDiagnosticModeNotice(string? isoCode)
        => Select(nameof(GetDiagnosticModeNotice), isoCode);

    internal string GetHomeTitle(string? isoCode)
        => Select(nameof(GetHomeTitle), isoCode);

    internal string GetInvalidStudyLinkHeading(string? isoCode)
        => Select(nameof(GetInvalidStudyLinkHeading), isoCode);

    internal string GetInvalidStudyLinkMessage(string? isoCode)
        => Select(nameof(GetInvalidStudyLinkMessage), isoCode);

    internal string GetNotFoundHeading(string? isoCode)
        => Select(nameof(GetNotFoundHeading), isoCode);

    internal string GetNotFoundMessage(string? isoCode)
        => Select(nameof(GetNotFoundMessage), isoCode);

    internal string GetLanguageTesterTitle(string? isoCode)
        => Select(nameof(GetLanguageTesterTitle), isoCode);

    internal string GetSettingsNavLabel(string? isoCode)
        => Select(nameof(GetSettingsNavLabel), isoCode);

    internal string GetNavigationLabel(string? isoCode)
        => Select(nameof(GetNavigationLabel), isoCode);

    internal string GetOpenNavigation(string? isoCode)
        => Select(nameof(GetOpenNavigation), isoCode);

    internal string GetCloseNavigation(string? isoCode)
        => Select(nameof(GetCloseNavigation), isoCode);

    internal string GetUnexpectedError(string? isoCode)
        => Select(nameof(GetUnexpectedError), isoCode);

    /// <summary>
    /// Liefert die Beschriftung des Startknopfs für assistive Technologien,
    /// damit dessen Aktion erkennbar ist.
    /// </summary>
    internal string GetStartControlAccessibleName(string? isoCode)
        => Select(nameof(GetStartControlAccessibleName), isoCode);


    internal string GetStartControlInstruction(string? isoCode)
        => Select(nameof(GetStartControlInstruction), isoCode);

    private string Select(string copyId, string? isoCode)
        => VoiceChromeCopyData.Select(copyId, _resolver.ResolveDisplayLanguage(isoCode));
}
