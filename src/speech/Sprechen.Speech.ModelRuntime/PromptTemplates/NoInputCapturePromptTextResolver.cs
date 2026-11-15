using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.PromptTemplates;
using Sprechen.Speech.ModelRuntime.Services;

namespace Sprechen.Speech.ModelRuntime.PromptTemplates;

/// <summary>
/// Liefert dem Aufnahmeablauf die modellierte Rückmeldung bei ausgebliebener Eingabe. Sprache und Höflichkeitsstufe bestimmen die Vorlage, fehlende Vorlagen werden zurückgewiesen.
/// </summary>
public static class NoInputCapturePromptTextResolver
{
    public static string GetNoInputPrompt(string isoCode)
        => GetNoInputPrompt(isoCode, PolitenessLevel.Formal);

    public static string GetNoInputPrompt(string isoCode, PolitenessLevel politenessLevel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        if (!VoiceInterfaceModelCatalog.Resources.TryGetPromptTemplate(
                FormDialogPromptIds.CaptureNoInput,
                isoCode,
                politenessLevel,
                out var template))
        {
            throw new InvalidOperationException(
                $"Prompt template '{FormDialogPromptIds.CaptureNoInput}' is missing for '{isoCode}'.");
        }

        return template;
    }
}
