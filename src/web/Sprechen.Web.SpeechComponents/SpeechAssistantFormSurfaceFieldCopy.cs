using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Web.FormSurfaces;

namespace Sprechen.Web.SpeechComponents;

/// <summary>
/// Ermittelt die Anzeigewerte und sprachabhängigen Statusbeschriftungen einzelner Formularfelder.
/// </summary>
public sealed class SpeechAssistantFormSurfaceFieldCopy
{
    private readonly SpeechAssistantFormSurfaceCopyResolver _resolver;

    internal SpeechAssistantFormSurfaceFieldCopy(SpeechAssistantFormSurfaceCopyResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public string GetValue(FormFieldPresentation field)
        => GetValue(field, VoiceInterfaceModelCatalog.DefaultLanguage);

    public string GetValue(FormFieldPresentation field, string? isoCode)
    {
        ArgumentNullException.ThrowIfNull(field);

        return field.HasDisplayValue
            ? field.DisplayValue!
            : _resolver.ResolveText(VoiceInterfaceResourceIds.FormSurfaceFieldValueNotCaptured, isoCode);
    }

    public string GetStatusLabel(FormSurfaceStage stage, FormFieldPresentation field)
        => GetStatusLabel(stage, field, VoiceInterfaceModelCatalog.DefaultLanguage);

    public string GetStatusLabel(FormSurfaceStage stage, FormFieldPresentation field, string? isoCode)
    {
        ArgumentNullException.ThrowIfNull(field);

        if (field.IsFocused && stage == FormSurfaceStage.Active)
        {
            return _resolver.ResolveText(VoiceInterfaceResourceIds.FormSurfaceFieldStatusListening, isoCode);
        }

        if (field.Status is null)
        {
            return field.Field.Required
                ? _resolver.ResolveText(VoiceInterfaceResourceIds.FormSurfaceFieldStatusPending, isoCode)
                : _resolver.ResolveText(VoiceInterfaceResourceIds.FormSurfaceFieldStatusOptional, isoCode);
        }

        if (!field.Status.Relevant)
        {
            return _resolver.ResolveText(VoiceInterfaceResourceIds.FormSurfaceFieldStatusNotRelevant, isoCode);
        }

        if (field.Status.Completed)
        {
            return _resolver.ResolveText(VoiceInterfaceResourceIds.FormSurfaceFieldStatusCompleted, isoCode);
        }

        return field.Status.Required
            ? _resolver.ResolveText(VoiceInterfaceResourceIds.FormSurfaceFieldStatusRequired, isoCode)
            : _resolver.ResolveText(VoiceInterfaceResourceIds.FormSurfaceFieldStatusOptional, isoCode);
    }
}
