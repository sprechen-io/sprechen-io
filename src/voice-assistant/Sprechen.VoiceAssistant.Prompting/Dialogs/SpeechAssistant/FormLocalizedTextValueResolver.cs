using Sprechen.Mda.Contracts.V1;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Liefert eine nutzbare Feldbeschriftung auch bei fehlender Zielübersetzung. Dafür folgen auf
/// die angeforderte Sprache die Standardsprache, Englisch und ein vorhandener nichtleerer Eintrag.
/// </summary>
internal sealed class FormLocalizedTextValueResolver
{
    public string? Resolve(LocalizedText? text, string isoCode, string? defaultLanguage)
    {
        if (text is null || text.Count == 0)
        {
            return null;
        }

        if (text.TryGetValue(isoCode, out var exact) && !string.IsNullOrWhiteSpace(exact))
        {
            return exact.Trim();
        }

        if (!string.IsNullOrWhiteSpace(defaultLanguage) &&
            text.TryGetValue(defaultLanguage, out var defaultValue) &&
            !string.IsNullOrWhiteSpace(defaultValue))
        {
            return defaultValue.Trim();
        }

        if (text.TryGetValue("en", out var english) && !string.IsNullOrWhiteSpace(english))
        {
            return english.Trim();
        }

        return text.Values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
    }
}
