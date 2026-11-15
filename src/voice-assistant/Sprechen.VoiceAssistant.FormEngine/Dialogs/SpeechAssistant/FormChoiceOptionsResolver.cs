using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Liest Auswahloptionen mit nichtleerem Code in ihrer Definitionsreihenfolge. Fehlende oder nicht lesbare Anzeigenamen werden durch den Code ersetzt.
/// </summary>
internal sealed class FormChoiceOptionsResolver
{
    private readonly IReadOnlyDictionary<VariableId, VariableDefinition> variablesById;
    private readonly Func<LocalizedText?, string?, string> resolveText;

    public FormChoiceOptionsResolver(
        IReadOnlyDictionary<VariableId, VariableDefinition> variablesById,
        Func<LocalizedText?, string?, string> resolveText)
    {
        this.variablesById = variablesById ?? throw new ArgumentNullException(nameof(variablesById));
        this.resolveText = resolveText ?? throw new ArgumentNullException(nameof(resolveText));
    }

    public ImmutableArray<FormChoiceOption> ResolveChoiceOptions(string variableId)
    {
        if (!variablesById.TryGetValue(VariableId.From(variableId), out var variable))
        {
            return [];
        }

        if (!TryGetValuesElement(variable, out var valuesElement))
        {
            return [];
        }

        var results = new List<FormChoiceOption>();
        foreach (var value in valuesElement.EnumerateArray())
        {
            if (TryResolveOption(value, out var option))
            {
                results.Add(option);
            }
        }

        return results.ToImmutableArray();
    }

    private static bool TryGetValuesElement(VariableDefinition variable, out JsonElement valuesElement)
    {
        valuesElement = default;

        if (variable.ValueType.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return variable.ValueType.TryGetProperty(ValueTypePropertyNames.Values, out valuesElement)
            && valuesElement.ValueKind == JsonValueKind.Array;
    }

    private bool TryResolveOption(JsonElement value, out FormChoiceOption option)
    {
        option = new FormChoiceOption(string.Empty, string.Empty);
        if (value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!value.TryGetProperty(ValueTypePropertyNames.Code, out var codeElement) || codeElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var code = codeElement.GetString();
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var displayName = ResolveDisplayName(value, code.Trim());
        option = new FormChoiceOption(code.Trim(), displayName);
        return true;
    }

    private string ResolveDisplayName(JsonElement value, string fallback)
    {
        if (!value.TryGetProperty(ValueTypePropertyNames.DisplayName, out var displayNameElement) || displayNameElement.ValueKind != JsonValueKind.Object)
        {
            return fallback;
        }

        try
        {
            var localized = JsonSerializer.Deserialize<LocalizedText>(displayNameElement);
            var resolved = resolveText(localized, fallback);
            return string.IsNullOrWhiteSpace(resolved) ? fallback : resolved.Trim();
        }
        catch (JsonException)
        {
            return fallback;
        }
    }
}
