using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Überträgt Setz-, Lösch- und Kopieraktionen auf den Variablenzustand, damit der Regelablauf dieselben typisierten Zugriffe verwendet.
/// </summary>
internal sealed class FormVariableValueActionExecutor
{
    private readonly IFormActionVariableValueState _variableValueState;

    internal FormVariableValueActionExecutor(IFormActionVariableValueState variableValueState)
    {
        _variableValueState = variableValueState ?? throw new ArgumentNullException(nameof(variableValueState));
    }

    internal bool ApplySetValue(JsonElement action)
    {
        if (!TryReadStringProperty(action, "variableId", out var variableId)
            || !action.TryGetProperty("value", out var valueElement)
            || !FormActionJsonValueEncoder.TryEncodeJsonValue(valueElement, out var encoded))
        {
            return false;
        }

        return _variableValueState.SetVariableValue(VariableId.From(variableId), encoded);
    }

    internal bool ApplyClearValue(JsonElement action)
    {
        if (!TryReadStringProperty(action, "variableId", out var variableId))
        {
            return false;
        }

        return _variableValueState.ClearVariableValue(VariableId.From(variableId));
    }

    internal bool ApplyCopyValue(JsonElement action)
    {
        if (!TryReadStringProperty(action, "fromVariableId", out var from)
            || !TryReadStringProperty(action, "toVariableId", out var to))
        {
            return false;
        }

        var value = _variableValueState.GetVariableValue(VariableId.From(from));
        if (value is null)
        {
            return false;
        }

        return _variableValueState.SetVariableValue(VariableId.From(to), value);
    }

    private static bool TryReadStringProperty(JsonElement action, string propertyName, out string value)
    {
        value = string.Empty;
        if (!action.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var rawValue = element.GetString();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        value = rawValue;
        return true;
    }
}
