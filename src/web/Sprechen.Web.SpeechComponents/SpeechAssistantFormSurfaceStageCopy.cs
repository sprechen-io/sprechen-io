using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Web.SpeechComponents;

/// <summary>
/// Liest die sprachabhängige Beschriftung des Formularstatus aus dem Modellkatalog.
/// </summary>
public sealed class SpeechAssistantFormSurfaceStageCopy
{
    private readonly SpeechAssistantFormSurfaceCopyResolver _resolver;

    internal SpeechAssistantFormSurfaceStageCopy(SpeechAssistantFormSurfaceCopyResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public string GetLabel(FormSurfaceStage stage)
        => GetLabel(stage, VoiceInterfaceModelCatalog.DefaultLanguage);

    public string GetLabel(FormSurfaceStage stage, string? isoCode)
    {
        return stage switch
        {
            FormSurfaceStage.Submitted => _resolver.ResolveText(VoiceInterfaceResourceIds.FormSurfaceStageSubmitted, isoCode),
            FormSurfaceStage.Cancelled => _resolver.ResolveText(VoiceInterfaceResourceIds.FormSurfaceStageCancelled, isoCode),
            FormSurfaceStage.Abandoned => _resolver.ResolveText(VoiceInterfaceResourceIds.FormSurfaceStageAbandoned, isoCode),
            _ => _resolver.ResolveText(VoiceInterfaceResourceIds.FormSurfaceStageLive, isoCode)
        };
    }
}
