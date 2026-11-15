using System.Text.Json;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet die Zeichenfolge im JSON-Feld kind nach Entfernen äußerer Leerzeichen unabhängig von Groß- und Kleinschreibung einer unterstützten Regelaktion zu.
/// </summary>
internal static class FormActionKindResolver
{
    private static readonly IReadOnlyDictionary<string, FormActionKind> KindsByName =
        new Dictionary<string, FormActionKind>(StringComparer.OrdinalIgnoreCase)
        {
            [ActionKinds.SetVisible] = FormActionKind.SetVisible,
            [ActionKinds.SetEnabled] = FormActionKind.SetEnabled,
            [ActionKinds.SetReadOnly] = FormActionKind.SetReadOnly,
            [ActionKinds.SetValue] = FormActionKind.SetValue,
            [ActionKinds.ClearValue] = FormActionKind.ClearValue,
            [ActionKinds.CopyValue] = FormActionKind.CopyValue,
            [ActionKinds.Speak] = FormActionKind.Speak,
            [ActionKinds.NavigateToFieldGroup] = FormActionKind.NavigateToFieldGroup,
            [ActionKinds.FocusField] = FormActionKind.FocusField
        };

    internal static bool TryResolve(JsonElement action, out FormActionKind kind)
    {
        kind = default;

        if (action.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!action.TryGetProperty("kind", out var kindElement) || kindElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var value = kindElement.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return TryResolveKind(value.Trim(), out kind);
    }

    private static bool TryResolveKind(string value, out FormActionKind kind)
        => KindsByName.TryGetValue(value, out kind);
}
