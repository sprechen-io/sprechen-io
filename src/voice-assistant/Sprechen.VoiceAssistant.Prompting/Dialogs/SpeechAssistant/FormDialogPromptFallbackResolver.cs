using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Liest die eingebettete Ansagevorlage in der normalisierten Sprache und meldet eine fehlende Vorlage
/// ausdrücklich, statt einen technischen Schlüssel als Sprechtext zurückzugeben.
/// </summary>
internal static class FormDialogPromptFallbackResolver
{
    public static string ResolveTemplate(string isoCode, string promptId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptId);
        var normalizedIsoCode = IsoCode.TryNormalizeIso6391(isoCode) ?? VoiceInterfaceModelCatalog.DefaultLanguage;
        if (VoiceInterfaceModelCatalog.Resources.TryGetPromptTemplate(promptId, normalizedIsoCode, out var template))
        {
            return template;
        }

        throw new InvalidOperationException($"No fallback prompt template registered for '{promptId}' and isoCode '{normalizedIsoCode}'.");
    }
}

