using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Liest Werttyp, Auswahlgrenzen und gespeicherte Variablenwerte für Formularprüfungen, damit deren Zugriffe dieselben Auflösungsregeln verwenden.
/// </summary>
internal static class FormVariableValueReader
{
    public static string? TryGetValueTypeKind(
        ImmutableDictionary<VariableId, VariableDefinition> variablesById,
        string variableId)
    {
        ArgumentNullException.ThrowIfNull(variablesById);
        ArgumentException.ThrowIfNullOrWhiteSpace(variableId);

        if (!variablesById.TryGetValue(VariableId.From(variableId), out var variable))
        {
            return null;
        }

        if (variable.ValueType.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!variable.ValueType.TryGetProperty(ValueTypePropertyNames.Kind, out var kindElement)
            || kindElement.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var kind = kindElement.GetString();
        return string.IsNullOrWhiteSpace(kind) ? null : kind.Trim();
    }

    public static (int? MinSelected, int? MaxSelected) GetSetSelectionBounds(
        ImmutableDictionary<VariableId, VariableDefinition> variablesById,
        string variableId)
    {
        ArgumentNullException.ThrowIfNull(variablesById);
        ArgumentException.ThrowIfNullOrWhiteSpace(variableId);

        if (!variablesById.TryGetValue(VariableId.From(variableId), out var variable)
            || variable.ValueType.ValueKind != JsonValueKind.Object)
        {
            return (null, null);
        }

        return (
            ReadOptionalInt(variable.ValueType, "minSelected"),
            ReadOptionalInt(variable.ValueType, "maxSelected"));
    }

    private static int? ReadOptionalInt(JsonElement valueType, string propertyName)
        => valueType.TryGetProperty(propertyName, out var element)
            && element.ValueKind == JsonValueKind.Number
            && element.TryGetInt32(out var value)
            ? value
            : null;

    public static bool TryGetValue(
        ImmutableDictionary<VariableId, VariableDefinition> variablesById,
        IReadOnlyDictionary<string, string> valuesByVariableId,
        string variableId,
        out FormValue value)
    {
        ArgumentNullException.ThrowIfNull(variablesById);
        ArgumentNullException.ThrowIfNull(valuesByVariableId);
        ArgumentException.ThrowIfNullOrWhiteSpace(variableId);

        value = default;
        if (!valuesByVariableId.TryGetValue(variableId, out var raw))
        {
            return false;
        }

        var kind = TryGetValueTypeKind(variablesById, variableId) ?? ValueTypeKinds.String;
        return FormValueParsers.TryParseFormValue(raw, kind, out value);
    }

    public static bool IsVariableValueSet(
        ImmutableDictionary<VariableId, VariableDefinition> variablesById,
        IReadOnlyDictionary<string, string> valuesByVariableId,
        string variableId)
    {
        ArgumentNullException.ThrowIfNull(variablesById);
        ArgumentNullException.ThrowIfNull(valuesByVariableId);
        ArgumentException.ThrowIfNullOrWhiteSpace(variableId);

        var valueTypeKind = TryGetValueTypeKind(variablesById, variableId) ?? ValueTypeKinds.String;
        return IsVariableValueSet(variablesById, valuesByVariableId, variableId, valueTypeKind);
    }

    public static bool IsVariableValueSet(
        ImmutableDictionary<VariableId, VariableDefinition> variablesById,
        IReadOnlyDictionary<string, string> valuesByVariableId,
        string variableId,
        string valueTypeKind)
    {
        ArgumentNullException.ThrowIfNull(variablesById);
        ArgumentNullException.ThrowIfNull(valuesByVariableId);
        ArgumentException.ThrowIfNullOrWhiteSpace(variableId);
        ArgumentException.ThrowIfNullOrWhiteSpace(valueTypeKind);

        if (!valuesByVariableId.TryGetValue(variableId, out var rawValue))
        {
            return false;
        }

        if (valueTypeKind is ValueTypeKinds.String or ValueTypeKinds.Enum or ValueTypeKinds.Date or ValueTypeKinds.Time)
        {
            return !string.IsNullOrWhiteSpace(rawValue);
        }

        if (valueTypeKind is ValueTypeKinds.Set)
        {
            return TryGetValue(variablesById, valuesByVariableId, variableId, out var setValue)
                && setValue.Kind == FormValueKind.Set
                && setValue.Set.Length > 0;
        }

        return TryGetValue(variablesById, valuesByVariableId, variableId, out _);
    }
}


