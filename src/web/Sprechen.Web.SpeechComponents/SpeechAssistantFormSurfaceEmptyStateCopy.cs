using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Web.SpeechComponents;

/// <summary>
/// Liest die Texte für eine Formularoberfläche ohne aktive Felder aus dem Modellkatalog.
/// </summary>
public sealed class SpeechAssistantFormSurfaceEmptyStateCopy
{
    private readonly SpeechAssistantFormSurfaceCopyResolver _resolver;

    internal SpeechAssistantFormSurfaceEmptyStateCopy(SpeechAssistantFormSurfaceCopyResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    /// <summary>
    /// Kurze Überschrift vor dem Titel des Leerzustands.
    /// </summary>
    public string Kicker => GetKicker(VoiceInterfaceModelCatalog.DefaultLanguage);

    public string Title => GetTitle(VoiceInterfaceModelCatalog.DefaultLanguage);

    public string Description => GetDescription(VoiceInterfaceModelCatalog.DefaultLanguage);

    public string GetKicker(string? isoCode) => _resolver.ResolveText(VoiceInterfaceResourceIds.FormSurfaceEmptyStateKicker, isoCode);

    public string GetTitle(string? isoCode) => _resolver.ResolveText(VoiceInterfaceResourceIds.FormSurfaceEmptyStateTitle, isoCode);

    public string GetDescription(string? isoCode) => _resolver.ResolveText(VoiceInterfaceResourceIds.FormSurfaceEmptyStateDescription, isoCode);
}
