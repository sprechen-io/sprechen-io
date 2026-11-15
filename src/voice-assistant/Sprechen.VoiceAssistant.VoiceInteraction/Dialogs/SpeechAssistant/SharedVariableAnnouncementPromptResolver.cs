using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.PromptTemplates;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Erstellt die Ansage zur Herkunft übernommener Angaben. Die Modellvorlage
/// wird zuerst aufgelöst. Ein verfügbarer Vertragstext ersetzt anschließend
/// diesen vorbereiteten Rückfalltext.
/// </summary>
internal sealed class SharedVariableAnnouncementPromptResolver
{
    public string Resolve(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        SharedVariableAnnouncementContent content)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentNullException.ThrowIfNull(content);

        var normalizedIsoCode = IsoCode.TryNormalizeIso6391(isoCode) ?? "en";
        var promptId = ResolvePromptId(content);
        var templateParameters = new Dictionary<string, string?>
        {
            [FormPromptTemplateParameterNames.SourceFormName] = content.SourceName,
            [FormPromptTemplateParameterNames.FieldLabels] = content.LabelText
        };
        var fallback = ResolveFallbackPrompt(normalizedIsoCode, promptId, templateParameters);

        return FormContractPromptResolver.TryResolvePrompt(
            contract,
            promptId,
            normalizedIsoCode,
            politenessLevel,
            templateParameters,
            out var resolvedPrompt)
            ? resolvedPrompt
            : fallback;
    }

    private static string ResolvePromptId(SharedVariableAnnouncementContent content)
    {
        var hasSourceName = !string.IsNullOrWhiteSpace(content.SourceName);
        var hasLabels = content.LabelText.Length > 0;

        return hasSourceName && hasLabels
            ? FormDialogPromptIds.SharedVariableAnnouncementSourceAndLabels
            : hasSourceName
                ? FormDialogPromptIds.SharedVariableAnnouncementSourceOnly
                : hasLabels
                    ? FormDialogPromptIds.SharedVariableAnnouncementLabelsOnly
                    : FormDialogPromptIds.SharedVariableAnnouncementDefault;
    }

    private static string ResolveFallbackPrompt(
        string isoCode,
        string promptId,
        IReadOnlyDictionary<string, string?> templateParameters)
    {
        if (!VoiceInterfaceModelCatalog.Resources.TryGetPromptTemplate(promptId, isoCode, out var template))
        {
            throw new InvalidOperationException($"No shared-variable prompt template registered for '{promptId}' and isoCode '{isoCode}'.");
        }

        return PromptTemplateFormatter.Apply(template, templateParameters);
    }
}
