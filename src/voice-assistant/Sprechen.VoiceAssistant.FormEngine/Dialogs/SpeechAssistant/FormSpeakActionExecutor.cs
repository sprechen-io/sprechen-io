using System.Text.Json;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Hängt eine modellierte Sprachaktion an die ausstehenden Ansagen an und meldet dafür keine iterationsrelevante Zustandsänderung.
/// </summary>
internal static class FormSpeakActionExecutor
{
    internal static bool ApplySpeak(JsonElement action, List<string> promptIdsToSpeak)
    {
        if (!action.TryGetProperty("promptId", out var promptIdElement) || promptIdElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var promptId = promptIdElement.GetString();
        if (string.IsNullOrWhiteSpace(promptId))
        {
            return false;
        }

        promptIdsToSpeak.Add(promptId.Trim());
        return false;
    }
}
