using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Web.SpeechComponents;

/// <summary>
/// Liest Oberflächentexte aus dem Sprachmodellkatalog und wählt bei nicht unterstützter Sprache die Standardsprache.
/// </summary>
internal sealed class SpeechAssistantFormSurfaceCopyResolver
{
    private readonly VoiceInterfaceLanguageCatalog _languageCatalog;

    internal SpeechAssistantFormSurfaceCopyResolver()
        : this(VoiceInterfaceLanguageCatalog.CreateDefault())
    {
    }

    private SpeechAssistantFormSurfaceCopyResolver(VoiceInterfaceLanguageCatalog languageCatalog)
    {
        _languageCatalog = languageCatalog;
    }

    internal string GetLanguageDisplayName(string languageIsoCode, string displayIsoCode)
    {
        return _languageCatalog.ResolveDisplayName(languageIsoCode, displayIsoCode);
    }

    internal string ResolveTemplate(string resourceId, string? isoCode, string placeholder, string replacement)
    {
        return ResolveText(resourceId, isoCode).Replace(placeholder, replacement, StringComparison.Ordinal);
    }

    internal string ResolveText(string resourceId, string? isoCode)
    {
        var resolvedIsoCode = ResolveDisplayLanguage(isoCode);
        if (!VoiceInterfaceModelCatalog.Resources.TryGetResourceText(resourceId, resolvedIsoCode, out var text))
        {
            throw new InvalidOperationException($"Voice interface resource '{resourceId}' is missing text for '{resolvedIsoCode}'.");
        }

        return text;
    }

    internal string ResolveDisplayLanguage(string? isoCode)
    {
        if (!LanguageCode.TryParseIso6391(isoCode, out var language)
            || !_languageCatalog.IsSupported(language))
        {
            return VoiceInterfaceModelCatalog.DefaultLanguage;
        }

        return language.Value;
    }
}
