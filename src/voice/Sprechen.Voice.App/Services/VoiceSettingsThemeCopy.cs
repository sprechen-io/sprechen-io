using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Voice.App.Services;

/// <summary>
/// Liefert lokalisierte Texte zur Auswahl des hellen, dunklen oder systemabhängigen Farbschemas.
/// </summary>
internal sealed class VoiceSettingsThemeCopy
{
    private readonly VoiceSettingsCopyResolver _resolver;

    internal VoiceSettingsThemeCopy(VoiceSettingsCopyResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    internal string GetTitle(string? isoCode)
        => Select(nameof(GetTitle), isoCode);

    internal string GetDescription(string? isoCode)
        => Select(nameof(GetDescription), isoCode);

    internal string GetSystemHint(string? isoCode)
        => Select(nameof(GetSystemHint), isoCode);

    internal string GetOptionLabel(ThemeMode mode, string? isoCode)
        => VoiceThemeOptionCopyData.Select($"{nameof(GetOptionLabel)}.{mode}", _resolver.ResolveDisplayLanguage(isoCode));

    private string Select(string copyId, string? isoCode)
        => VoiceThemeCopyData.Select(copyId, _resolver.ResolveDisplayLanguage(isoCode));
}
