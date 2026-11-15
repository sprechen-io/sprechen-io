using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Localization;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Löst Ansagetext nach Kennung und Sprache aus dem Vertragsbestand auf, damit Wiedergabewege denselben Textzugriff verwenden.
/// </summary>
internal sealed class FormVoiceCommandPromptTextResolver
{
    public ImmutableArray<string> Resolve(
        FormContract contract,
        ImmutableArray<string> promptIds,
        string isoCode,
        VoiceDialogSettings voiceSettings)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(voiceSettings);

        if (promptIds.Length == 0 || contract.Projections?.Voice?.Hints?.Prompts is not { Count: > 0 } prompts)
        {
            return [];
        }

        var resolvedPrompts = ImmutableArray.CreateBuilder<string>();
        foreach (var promptId in promptIds)
        {
            if (TryResolvePromptText(contract, prompts, promptId, isoCode, voiceSettings, out var resolved))
            {
                resolvedPrompts.Add(resolved);
            }
        }

        return resolvedPrompts.ToImmutable();
    }

    private static bool TryResolvePromptText(
        FormContract contract,
        IReadOnlyCollection<VoicePrompt> prompts,
        string promptId,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        out string resolved)
    {
        resolved = string.Empty;
        if (string.IsNullOrWhiteSpace(promptId))
        {
            return false;
        }

        var prompt = prompts.FirstOrDefault(item =>
            string.Equals(item.PromptId, promptId, StringComparison.OrdinalIgnoreCase));

        if (prompt is null)
        {
            return false;
        }

        if (!LocalizedTextResolver.TryResolve(
                prompt.Text,
                isoCode,
                contract.DefaultLanguage,
                contract.SupportedLanguages,
                contract.StrictLocalization,
                voiceSettings.GetPolitenessLevel(),
                out var promptText)
            || string.IsNullOrWhiteSpace(promptText))
        {
            return false;
        }

        resolved = promptText.Trim();
        return true;
    }
}
