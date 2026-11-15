namespace Sprechen.Voice.App.Services;

/// <summary>
/// Liefert lokalisierte Texte zur Anredeform aus dem gemeinsamen
/// Katalog der Sitzungseinstellungen.
/// </summary>
internal sealed class VoiceSettingsSessionSetupPolitenessCopy
{
    private readonly VoiceSettingsCopyResolver _resolver;

    internal VoiceSettingsSessionSetupPolitenessCopy(VoiceSettingsCopyResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    internal string GetLabel(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsSessionSetupPolitenessLabel, isoCode);

    internal string GetFormal(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsSessionSetupPolitenessFormal, isoCode);

    internal string GetInformal(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsSessionSetupPolitenessInformal, isoCode);

    internal string GetHelp(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsSessionSetupPolitenessHelp, isoCode);

    internal string GetNoProfile(string? isoCode)
        => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsSessionSetupNoPolitenessProfile, isoCode);

    internal string GetSummary(string? displayIsoCode, string? selectedPolitenessValue)
    {
        return _resolver.ResolveTemplate(
            VoiceInterfaceResourceIds.SettingsSessionSetupPolitenessSummary,
            displayIsoCode,
            "{politeness}",
            GetValue(displayIsoCode, selectedPolitenessValue));
    }

    private string GetValue(string? displayIsoCode, string? selectedPolitenessValue)
    {
        return selectedPolitenessValue switch
        {
            "informal" => GetInformal(displayIsoCode),
            "formal" => GetFormal(displayIsoCode),
            _ => _resolver.ResolveText(VoiceInterfaceResourceIds.SettingsSessionSetupPolitenessDefault, displayIsoCode)
        };
    }
}
