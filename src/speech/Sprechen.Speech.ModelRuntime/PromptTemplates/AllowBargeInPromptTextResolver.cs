using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.ModelRuntime.PromptTemplates;

/// <summary>
/// Liefert die modellierten Rückmeldungen zur Einwurfsfreigabe für den Befehlsdialog. Die Freigabe selbst und ihre Änderung bleiben beim Aufrufer.
/// </summary>
public static class AllowBargeInPromptTextResolver
{
    public static string GetEnabledPrompt(string isoCode)
        => GetEnabledPrompt(isoCode, PolitenessLevel.Formal);

    public static string GetEnabledPrompt(string isoCode, PolitenessLevel politenessLevel)
        => ResolvePrompt(isoCode, FormDialogPromptIds.AllowBargeInEnabled, politenessLevel);

    public static string GetDisabledPrompt(string isoCode)
        => ResolvePrompt(isoCode, FormDialogPromptIds.AllowBargeInDisabled, PolitenessLevel.Formal);

    public static string GetAlreadyEnabledPrompt(string isoCode)
        => ResolvePrompt(isoCode, FormDialogPromptIds.AllowBargeInAlreadyEnabled, PolitenessLevel.Formal);

    public static string GetAlreadyDisabledPrompt(string isoCode)
        => ResolvePrompt(isoCode, FormDialogPromptIds.AllowBargeInAlreadyDisabled, PolitenessLevel.Formal);

    private static string ResolvePrompt(string isoCode, string promptId, PolitenessLevel politenessLevel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(promptId);

        if (!VoiceInterfaceModelCatalog.Resources.TryGetPromptTemplate(promptId, isoCode, politenessLevel, out var template))
        {
            throw new InvalidOperationException($"Prompt template '{promptId}' is missing for '{isoCode}'.");
        }

        return PromptTemplateFormatter.Apply(template, new Dictionary<string, string?>());
    }
}
