using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Localization;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant.CatalogPrompting;

/// <summary>
/// Löst einen modellierten Prompt mit Sprachvorgaben und Parameterwerten auf. Fehlende oder leere
/// Ergebnisse werden abgewiesen, bevor sie als Auswahlansage zurückgegeben werden.
/// </summary>
internal static class CatalogPromptResolver
{
    public static bool TryResolvePrompt(
        CatalogPromptContext promptContext,
        string promptId,
        string isoCode,
        PolitenessLevel politenessLevel,
        IReadOnlyDictionary<string, string?>? templateParameters,
        out string resolvedPrompt)
    {
        ArgumentNullException.ThrowIfNull(promptContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(promptId);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        resolvedPrompt = string.Empty;
        if (promptContext.SelectionDialog.Prompts is not { Count: > 0 } prompts)
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
                promptContext.DefaultLanguage,
                promptContext.SupportedLanguages,
                promptContext.SelectionDialog.StrictLocalization,
                politenessLevel,
                out var localized)
            || string.IsNullOrWhiteSpace(localized))
        {
            return false;
        }

        var promptText = localized.Trim();
        resolvedPrompt = ApplyTemplateParameters(promptText, templateParameters);
        return !string.IsNullOrWhiteSpace(resolvedPrompt);
    }

    private static string ApplyTemplateParameters(
        string template,
        IReadOnlyDictionary<string, string?>? templateParameters)
    {
        if (string.IsNullOrEmpty(template) || templateParameters is null || templateParameters.Count == 0)
        {
            return template;
        }

        var resolved = template;
        foreach (var parameter in templateParameters)
        {
            if (string.IsNullOrWhiteSpace(parameter.Key))
            {
                continue;
            }

            var token = $"{{{{{parameter.Key.Trim()}}}}}";
            var value = parameter.Value?.Trim() ?? string.Empty;
            resolved = resolved.Replace(token, value, StringComparison.OrdinalIgnoreCase);
        }

        return resolved;
    }
}


