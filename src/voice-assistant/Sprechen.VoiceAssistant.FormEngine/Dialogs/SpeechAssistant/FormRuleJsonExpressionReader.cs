using System.Text.Json;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Liest erforderliche Zeichenketten aus Regelausdrücken mit getrennten Regeln für bereinigte und unveränderte Inhalte.
/// </summary>
internal static class FormRuleJsonExpressionReader
{
    public static bool TryGetStringProperty(JsonElement expression, string propertyName, out string value)
    {
        value = string.Empty;

        if (!expression.TryGetProperty(propertyName, out var propertyElement) || propertyElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var rawValue = propertyElement.GetString();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        value = rawValue.Trim();
        return true;
    }

    public static bool TryGetExactStringProperty(JsonElement expression, string propertyName, out string value)
    {
        value = string.Empty;

        if (!expression.TryGetProperty(propertyName, out var propertyElement) || propertyElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var rawValue = propertyElement.GetString();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        value = rawValue;
        return true;
    }
}
