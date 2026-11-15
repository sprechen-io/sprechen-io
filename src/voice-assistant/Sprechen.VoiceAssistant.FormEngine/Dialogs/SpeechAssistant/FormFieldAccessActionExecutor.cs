using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Liest Feldkennung und Wahrheitswert aus Regelaktionen und führt damit Aktivierungs- oder Schreibschutzänderungen über den Zustandszugang aus.
/// </summary>
internal sealed class FormFieldAccessActionExecutor
{
    private readonly IFormActionFieldAccessState _fieldAccessState;

    internal FormFieldAccessActionExecutor(IFormActionFieldAccessState fieldAccessState)
    {
        _fieldAccessState = fieldAccessState ?? throw new ArgumentNullException(nameof(fieldAccessState));
    }

    internal bool ApplySetEnabled(JsonElement action)
    {
        if (!TryReadFieldId(action, out var fieldId) || !TryReadBooleanValue(action, out var enabled))
        {
            return false;
        }

        return _fieldAccessState.SetFieldEnabled(FieldId.From(fieldId), enabled);
    }

    internal bool ApplySetReadOnly(JsonElement action)
    {
        if (!TryReadFieldId(action, out var fieldId) || !TryReadBooleanValue(action, out var readOnly))
        {
            return false;
        }

        return _fieldAccessState.SetFieldReadOnly(FieldId.From(fieldId), readOnly);
    }

    private static bool TryReadFieldId(JsonElement action, out string fieldId)
    {
        fieldId = string.Empty;
        if (!action.TryGetProperty("fieldId", out var fieldIdElement) || fieldIdElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var value = fieldIdElement.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        fieldId = value;
        return true;
    }

    private static bool TryReadBooleanValue(JsonElement action, out bool value)
    {
        value = false;
        if (!action.TryGetProperty("value", out var valueElement) || valueElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return false;
        }

        value = valueElement.ValueKind == JsonValueKind.True;
        return true;
    }
}
