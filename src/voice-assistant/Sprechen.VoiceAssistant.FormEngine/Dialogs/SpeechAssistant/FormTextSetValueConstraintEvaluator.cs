using System.Text.Json;
using System.Text.RegularExpressions;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft gespeicherte Rohtexte auf UTF-16-Länge und vollständige Musterübereinstimmung.
/// Die Anzahl von Mengenelementen wird dabei nicht ermittelt.
/// </summary>
internal sealed class FormTextSetValueConstraintEvaluator(IReadOnlyDictionary<string, string> rawValuesByVariableId)
{
    private readonly IReadOnlyDictionary<string, string> _rawValuesByVariableId =
        rawValuesByVariableId ?? throw new ArgumentNullException(nameof(rawValuesByVariableId));

    internal bool Supports(string kind)
    {
        return string.Equals(kind, ConstraintKinds.Pattern, StringComparison.OrdinalIgnoreCase)
            || string.Equals(kind, ConstraintKinds.MinLength, StringComparison.OrdinalIgnoreCase)
            || string.Equals(kind, ConstraintKinds.MaxLength, StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsSatisfied(string kind, ConstraintDefinition constraint, string variableId)
    {
        if (string.Equals(kind, ConstraintKinds.Pattern, StringComparison.OrdinalIgnoreCase))
        {
            return IsPatternSatisfied(constraint, variableId);
        }

        if (string.Equals(kind, ConstraintKinds.MinLength, StringComparison.OrdinalIgnoreCase))
        {
            return IsMinLengthSatisfied(constraint, variableId);
        }

        if (string.Equals(kind, ConstraintKinds.MaxLength, StringComparison.OrdinalIgnoreCase))
        {
            return IsMaxLengthSatisfied(constraint, variableId);
        }

        throw new InvalidOperationException($"Unsupported text constraint kind '{kind}'.");
    }

    private bool IsPatternSatisfied(ConstraintDefinition constraint, string variableId)
    {
        if (!FormConstraintJsonValueReader.TryGetAdditionalProperty(constraint, ConstraintPropertyNames.Pattern, out var patternElement))
        {
            return true;
        }

        var pattern = patternElement.GetString();
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return true;
        }

        var rawValue = _rawValuesByVariableId.TryGetValue(variableId, out var resolvedValue) ? resolvedValue : string.Empty;

        // Das modellierte Muster gegen den vollständigen Wert prüfen. Die äußere Gruppe
        // hält Alternativen zwischen den absoluten Anfangs- und Endankern, sodass auch
        // ein zusätzlicher abschließender Zeilenumbruch zum geprüften Inhalt zählt.
        try
        {
            return Regex.IsMatch(rawValue ?? string.Empty, $@"\A(?:{pattern})\z", RegexOptions.CultureInvariant);
        }
        catch (ArgumentException)
        {
            throw new FormConstraintAuthoringException(
                $"Constraint '{constraint.ConstraintId}' has an unreadable '{ConstraintPropertyNames.Pattern}' value.");
        }
    }

    private bool IsMinLengthSatisfied(ConstraintDefinition constraint, string variableId)
    {
        if (!FormConstraintJsonValueReader.TryGetAdditionalProperty(constraint, ConstraintPropertyNames.MinLength, out var minElement))
        {
            return true;
        }

        if (minElement.ValueKind != JsonValueKind.Number)
        {
            return true;
        }

        var min = minElement.GetInt32();
        var rawValue = _rawValuesByVariableId.TryGetValue(variableId, out var resolvedValue) ? resolvedValue : string.Empty;
        return (rawValue ?? string.Empty).Length >= min;
    }

    private bool IsMaxLengthSatisfied(ConstraintDefinition constraint, string variableId)
    {
        if (!FormConstraintJsonValueReader.TryGetAdditionalProperty(constraint, ConstraintPropertyNames.MaxLength, out var maxElement))
        {
            return true;
        }

        if (maxElement.ValueKind != JsonValueKind.Number)
        {
            return true;
        }

        var max = maxElement.GetInt32();
        var rawValue = _rawValuesByVariableId.TryGetValue(variableId, out var resolvedValue) ? resolvedValue : string.Empty;
        return (rawValue ?? string.Empty).Length <= max;
    }
}
