using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Localization;

namespace Sprechen.Speech.App.Contracts.PromptTemplates;

/// <summary>
/// Löst modellierte Ansagen aus dem Formularvertrag sprachabhängig auf und setzt übergebene Vorlagenparameter ein.
/// </summary>
public static class FormContractPromptResolver
{
    public static bool TryResolvePrompt(
        FormContract contract,
        string promptId,
        string isoCode,
        PolitenessLevel politenessLevel,
        IReadOnlyDictionary<string, string?>? templateParameters,
        out string resolvedPrompt)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(promptId);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        resolvedPrompt = string.Empty;
        if (contract.Projections?.Voice?.Hints?.Prompts is not { Count: > 0 } prompts)
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
                politenessLevel,
                out var localized)
            || string.IsNullOrWhiteSpace(localized))
        {
            return false;
        }

        var promptText = localized.Trim();
        resolvedPrompt = PromptTemplateFormatter.Apply(promptText, templateParameters);
        return !string.IsNullOrWhiteSpace(resolvedPrompt);
    }
}



