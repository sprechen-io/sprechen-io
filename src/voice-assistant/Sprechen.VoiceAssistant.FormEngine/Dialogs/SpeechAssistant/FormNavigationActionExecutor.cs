using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Liest Feld- oder Gruppenkennungen aus Regelaktionen und führt die Navigation über den Zustandszugang aus.
/// </summary>
internal sealed class FormNavigationActionExecutor
{
    private readonly IFormActionNavigationState _navigationState;

    internal FormNavigationActionExecutor(IFormActionNavigationState navigationState)
    {
        _navigationState = navigationState ?? throw new ArgumentNullException(nameof(navigationState));
    }

    internal bool ApplyNavigateToFieldGroup(JsonElement action)
    {
        if (!TryReadStringProperty(action, "fieldGroupId", out var fieldGroupId))
        {
            return false;
        }

        return _navigationState.TryNavigateToFieldGroup(FieldGroupId.From(fieldGroupId));
    }

    internal bool ApplyFocusField(JsonElement action)
    {
        if (!TryReadStringProperty(action, "fieldId", out var fieldId))
        {
            return false;
        }

        return _navigationState.TrySetFocus(FieldId.From(fieldId));
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
