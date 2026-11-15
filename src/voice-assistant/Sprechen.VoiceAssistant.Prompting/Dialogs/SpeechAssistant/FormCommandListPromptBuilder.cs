using System.Globalization;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.VoiceCommands;
using Sprechen.Speech.App.Contracts.PromptTemplates;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Setzt die übergebenen Befehlsgruppen in ihrer Reihenfolge unter lokalisierten
/// Kategorieüberschriften zur Vorleseansage zusammen. Leere Gruppen entfallen. Gesamtzahl und
/// Kürzungskennzeichen bestimmen den abschließenden Text. Eine nichtpositive Gesamtzahl wählt die
/// Ansage ohne verfügbare Befehle.
/// </summary>
public static class FormCommandListPromptBuilder
{
    public static string BuildPrompt(
        FormContract? contract,
        IReadOnlyList<(VoiceCommandCategory Category, IReadOnlyList<string> Labels)> groups,
        int totalCount,
        bool truncated,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(groups);

        if (totalCount <= 0)
        {
            return FormStatusPromptResolver.ResolveOrFallback(
                contract,
                FormDialogPromptIds.CommandListNone,
                isoCode,
                politenessLevel);
        }

        var segments = new List<string>();
        foreach (var (category, labels) in groups)
        {
            if (labels.Count == 0)
            {
                continue;
            }

            var groupParameters = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [FormPromptTemplateParameterNames.Items] = FormStatusPromptResolver.JoinPromptItems(labels),
            };

            segments.Add(FormStatusPromptResolver.ResolveOrFallback(
                contract,
                GroupHeaderPromptId(category),
                isoCode,
                politenessLevel,
                groupParameters));
        }

        var promptId = truncated
            ? FormDialogPromptIds.CommandListSummaryTruncated
            : FormDialogPromptIds.CommandListSummary;

        var templateParameters = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [FormPromptTemplateParameterNames.Count] = totalCount.ToString(CultureInfo.InvariantCulture),
            [FormPromptTemplateParameterNames.Items] = string.Join(". ", segments),
        };

        return FormStatusPromptResolver.ResolveOrFallback(
            contract,
            promptId,
            isoCode,
            politenessLevel,
            templateParameters);
    }

    private static string GroupHeaderPromptId(VoiceCommandCategory category) => category switch
    {
        VoiceCommandCategory.Navigation => FormDialogPromptIds.CommandListGroupNavigation,
        VoiceCommandCategory.Monitoring => FormDialogPromptIds.CommandListGroupMonitoring,
        VoiceCommandCategory.Discovery => FormDialogPromptIds.CommandListGroupDiscovery,
        VoiceCommandCategory.InputMode => FormDialogPromptIds.CommandListGroupInputMode,
        VoiceCommandCategory.Control => FormDialogPromptIds.CommandListGroupControl,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unmapped voice command category."),
    };
}
