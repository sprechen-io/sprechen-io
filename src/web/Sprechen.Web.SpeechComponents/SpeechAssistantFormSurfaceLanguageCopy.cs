using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Web.SpeechComponents;

/// <summary>
/// Erzeugt sprachabhängige Hinweise zur gewählten Sprache und zum fokussierten Feld aus Katalogvorlagen.
/// </summary>
public sealed class SpeechAssistantFormSurfaceLanguageCopy
{
    private readonly SpeechAssistantFormSurfaceCopyResolver _resolver;

    internal SpeechAssistantFormSurfaceLanguageCopy(SpeechAssistantFormSurfaceCopyResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public string GetSummary(string isoCode)
    {
        var displayLanguage = _resolver.ResolveDisplayLanguage(isoCode);
        var languageName = _resolver.GetLanguageDisplayName(displayLanguage, displayLanguage);
        return _resolver.ResolveTemplate(VoiceInterfaceResourceIds.FormSurfaceLanguageSummary, displayLanguage, "{languageName}", languageName);
    }

    public string GetFocusedFieldSummary(string focusedFieldLabel)
    {
        return GetFocusedFieldSummary(focusedFieldLabel, VoiceInterfaceModelCatalog.DefaultLanguage);
    }

    public string GetFocusedFieldSummary(string focusedFieldLabel, string? isoCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(focusedFieldLabel);

        return _resolver.ResolveTemplate(
            VoiceInterfaceResourceIds.FormSurfaceFocusedFieldSummary,
            isoCode,
            "{fieldLabel}",
            focusedFieldLabel.Trim());
    }
}
