using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Ermittelt fehlende vorausgesetzte Formularabschlüsse aus den modellierten Kennungen und dem übergebenen Abschlussbestand.
/// </summary>
internal sealed class FormVoiceCommandSwitchFormPrerequisiteResolver
{
    public string[] ResolveMissingPrerequisiteNames(
        FormVoiceCommandGlobalCommandContext context,
        FormVoiceCommandSwitchFormTarget target)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(target);

        return (target.Form.PrerequisiteForms ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(prerequisiteId => !context.CompletedFormIds.Contains(prerequisiteId))
            .Select(prerequisiteId => ResolveSourceFormName(
                context.Application,
                prerequisiteId,
                context.IsoCode,
                context.VoiceSettings))
            .ToArray();
    }

    private static string ResolveSourceFormName(
        ApplicationDescriptor application,
        string sourceFormId,
        string isoCode,
        VoiceDialogSettings voiceSettings)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFormId);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentNullException.ThrowIfNull(voiceSettings);

        var normalizedFormId = sourceFormId.Trim();
        var form = application.Forms.FirstOrDefault(candidate =>
            string.Equals(candidate.FormId, normalizedFormId, StringComparison.OrdinalIgnoreCase));

        return form is null
            ? normalizedFormId
            : new FormsCatalogTextResolver().ResolveFormDisplayName(
                application,
                form,
                isoCode,
                voiceSettings.GetPolitenessLevel());
    }
}
