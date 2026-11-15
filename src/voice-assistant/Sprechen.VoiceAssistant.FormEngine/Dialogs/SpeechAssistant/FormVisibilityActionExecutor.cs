using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Überträgt modellierte Sichtbarkeitsaktionen für Felder und Gruppen auf den Zustand und meldet dessen Änderungsergebnis zurück.
/// </summary>
internal sealed class FormVisibilityActionExecutor
{
    private readonly IFormActionVisibilityState _visibilityState;

    internal FormVisibilityActionExecutor(IFormActionVisibilityState visibilityState)
    {
        _visibilityState = visibilityState ?? throw new ArgumentNullException(nameof(visibilityState));
    }

    internal bool ApplySetVisible(JsonElement action)
    {
        if (!TryReadVisibilityTarget(action, out var targetKind, out var targetId)
            || !TryReadBooleanValue(action, out var visible))
        {
            return false;
        }

        if (string.Equals(targetKind, TargetKinds.Field, StringComparison.OrdinalIgnoreCase))
        {
            return _visibilityState.SetFieldVisible(FieldId.From(targetId), visible);
        }

        if (string.Equals(targetKind, TargetKinds.FieldGroup, StringComparison.OrdinalIgnoreCase))
        {
            return _visibilityState.SetFieldGroupVisible(FieldGroupId.From(targetId), visible);
        }

        return false;
    }

    private static bool TryReadVisibilityTarget(JsonElement action, out string targetKind, out string targetId)
    {
        targetKind = string.Empty;
        targetId = string.Empty;

        if (!action.TryGetProperty("target", out var targetElement) || targetElement.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!TryReadStringProperty(targetElement, "kind", out targetKind))
        {
            return false;
        }

        if (string.Equals(targetKind, TargetKinds.Field, StringComparison.OrdinalIgnoreCase))
        {
            return TryReadStringProperty(targetElement, "fieldId", out targetId);
        }

        if (string.Equals(targetKind, TargetKinds.FieldGroup, StringComparison.OrdinalIgnoreCase))
        {
            return TryReadStringProperty(targetElement, "fieldGroupId", out targetId);
        }

        return false;
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

    private static bool TryReadStringProperty(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var propertyElement) || propertyElement.ValueKind != JsonValueKind.String)
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
