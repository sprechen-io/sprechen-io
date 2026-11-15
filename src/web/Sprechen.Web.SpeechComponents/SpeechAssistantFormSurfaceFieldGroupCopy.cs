using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Web.FormSurfaces;

namespace Sprechen.Web.SpeechComponents;

/// <summary>
/// Liest sprachabhängige Statusbeschriftungen für Feldgruppen aus dem Modellkatalog.
/// </summary>
public sealed class SpeechAssistantFormSurfaceFieldGroupCopy
{
    private readonly SpeechAssistantFormSurfaceCopyResolver _resolver;

    internal SpeechAssistantFormSurfaceFieldGroupCopy(SpeechAssistantFormSurfaceCopyResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public string GetStatusLabel(FormFieldGroupPresentation fieldGroup)
        => GetStatusLabel(fieldGroup, VoiceInterfaceModelCatalog.DefaultLanguage);

    public string GetStatusLabel(FormFieldGroupPresentation fieldGroup, string? isoCode)
    {
        ArgumentNullException.ThrowIfNull(fieldGroup);

        if (fieldGroup.Status is null)
        {
            return _resolver.ResolveText(VoiceInterfaceResourceIds.FormSurfaceFieldGroupStatusPending, isoCode);
        }

        if (!fieldGroup.Status.Relevant)
        {
            return _resolver.ResolveText(VoiceInterfaceResourceIds.FormSurfaceFieldGroupStatusNotRelevant, isoCode);
        }

        return fieldGroup.Status.Completed
            ? _resolver.ResolveText(VoiceInterfaceResourceIds.FormSurfaceFieldGroupStatusCompleted, isoCode)
            : _resolver.ResolveText(VoiceInterfaceResourceIds.FormSurfaceFieldGroupStatusInProgress, isoCode);
    }
}
