using Sprechen.Speech.App.Contracts.PromptTemplates;
using Sprechen.Speech.ModelRuntime.Services;

namespace Sprechen.Speech.ModelRuntime.PromptTemplates;

/// <summary>
/// Löst die modellierte kurze Einleitung vor einer automatischen Wiederholung
/// auf, damit Hörende die erneute Ausgabe als Wiederholung erkennen können.
/// </summary>
public static class CaptureReplayLeadInPromptTextResolver
{
    public static string GetReplayLeadIn(string isoCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        if (!VoiceInterfaceModelCatalog.Resources.TryGetPromptTemplate(
                FormDialogPromptIds.CaptureReplayLeadIn,
                isoCode,
                out var template))
        {
            throw new InvalidOperationException(
                $"Prompt template '{FormDialogPromptIds.CaptureReplayLeadIn}' is missing for '{isoCode}'.");
        }

        return template;
    }
}
