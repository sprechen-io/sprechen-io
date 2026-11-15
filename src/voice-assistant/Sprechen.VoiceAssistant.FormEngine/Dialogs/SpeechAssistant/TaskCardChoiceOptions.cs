using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Liefert die Auswahlmenge eines Antwortfeldes: modellierte Codes oder die
/// sprachabhängigen Wahrheitswerte, wenn der Vertrag einen Boolean beschreibt.
/// </summary>
internal static class TaskCardChoiceOptions
{
    private static readonly ImmutableArray<FormChoiceOption> BooleanOptions =
    [
        new("true", "Ja"),
        new("true", "Yes"),
        new("true", "Oui"),
        new("true", "Sí"),
        new("true", "Sì"),
        new("false", "Nein"),
        new("false", "No"),
        new("false", "Non"),
    ];

    internal static ImmutableArray<FormChoiceOption> Resolve(
        FormChoiceOptionsResolver optionsResolver,
        IReadOnlyDictionary<VariableId, VariableDefinition> variablesById,
        FieldDefinition answer)
    {
        var resolved = optionsResolver.ResolveChoiceOptions(answer.VariableId.Value);
        if (!resolved.IsEmpty)
        {
            return resolved;
        }

        return IsBoolean(variablesById, answer) ? BooleanOptions : [];
    }

    internal static string ResolveLabel(LocalizedText? text, string? fallback)
    {
        if (text is not null)
        {
            foreach (var language in LanguageDescriptors.AllIsoCodes)
            {
                if (text.TryGetValue(language, out var label) && !string.IsNullOrWhiteSpace(label))
                {
                    return label.Trim();
                }
            }

            foreach (var label in text.Values)
            {
                if (!string.IsNullOrWhiteSpace(label))
                {
                    return label.Trim();
                }
            }
        }

        return fallback?.Trim() ?? string.Empty;
    }

    private static bool IsBoolean(
        IReadOnlyDictionary<VariableId, VariableDefinition> variablesById,
        FieldDefinition answer)
    {
        return variablesById.TryGetValue(answer.VariableId, out var variable)
            && variable.ValueType.ValueKind == JsonValueKind.Object
            && variable.ValueType.TryGetProperty(ValueTypePropertyNames.Kind, out var kind)
            && kind.ValueKind == JsonValueKind.String
            && string.Equals(kind.GetString(), ValueTypeKinds.Boolean, StringComparison.Ordinal);
    }
}
