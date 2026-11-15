using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Liefert eine Quellenbezeichnung, wenn unter den bekannten Quellenkennungen
/// genau ein unterschiedliches Formular vorkommt. Fehlende Kennungen zählen nicht mit.
/// </summary>
internal sealed class SharedVariableAnnouncementSourceResolver
{
    public string? ResolveSourceName(
        ApplicationDescriptor application,
        ImmutableArray<(string VariableId, string? SourceFormId)> adoptions,
        string isoCode,
        VoiceDialogSettings voiceSettings)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentNullException.ThrowIfNull(voiceSettings);

        var sources = adoptions
            .Select(adoption => adoption.SourceFormId)
            .Where(sourceId => !string.IsNullOrWhiteSpace(sourceId))
            .Select(sourceId => sourceId!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return sources.Length == 1
            ? ResolveSourceFormName(application, sources[0], isoCode, voiceSettings)
            : null;
    }

    private static string ResolveSourceFormName(
        ApplicationDescriptor application,
        string sourceFormId,
        string isoCode,
        VoiceDialogSettings voiceSettings)
    {
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
