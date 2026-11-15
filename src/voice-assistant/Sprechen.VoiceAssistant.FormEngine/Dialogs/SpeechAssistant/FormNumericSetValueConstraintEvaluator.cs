using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft ganzzahlige Formularwerte gegen modellierte Grenzen und Vergleiche.
/// </summary>
internal sealed class FormNumericSetValueConstraintEvaluator
{
    internal bool Supports(string kind)
    {
        return string.Equals(kind, ConstraintKinds.Min, StringComparison.OrdinalIgnoreCase)
            || string.Equals(kind, ConstraintKinds.Max, StringComparison.OrdinalIgnoreCase)
            || string.Equals(kind, ConstraintKinds.Range, StringComparison.OrdinalIgnoreCase)
            || string.Equals(kind, ConstraintKinds.Compare, StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsSatisfied(string kind, ConstraintDefinition constraint, FormValue value)
    {
        if (string.Equals(kind, ConstraintKinds.Min, StringComparison.OrdinalIgnoreCase))
        {
            return IsMinSatisfied(constraint, value);
        }

        if (string.Equals(kind, ConstraintKinds.Max, StringComparison.OrdinalIgnoreCase))
        {
            return IsMaxSatisfied(constraint, value);
        }

        if (string.Equals(kind, ConstraintKinds.Range, StringComparison.OrdinalIgnoreCase))
        {
            return IsRangeSatisfied(constraint, value);
        }

        if (string.Equals(kind, ConstraintKinds.Compare, StringComparison.OrdinalIgnoreCase))
        {
            return IsCompareSatisfied(constraint, value);
        }

        throw new InvalidOperationException($"Unsupported numeric constraint kind '{kind}'.");
    }

    private static bool IsMinSatisfied(ConstraintDefinition constraint, FormValue value)
    {
        var min = ReadOptionalInt32Bound(constraint, ConstraintPropertyNames.Min);
        if (!min.HasValue)
        {
            return true;
        }

        if (value.Kind != FormValueKind.Integer)
        {
            return false;
        }

        return value.Integer >= min.Value;
    }

    private static bool IsMaxSatisfied(ConstraintDefinition constraint, FormValue value)
    {
        var max = ReadOptionalInt32Bound(constraint, ConstraintPropertyNames.Max);
        if (!max.HasValue)
        {
            return true;
        }

        if (value.Kind != FormValueKind.Integer)
        {
            return false;
        }

        return value.Integer <= max.Value;
    }

    private static bool IsRangeSatisfied(ConstraintDefinition constraint, FormValue value)
    {
        var min = ReadOptionalInt32Bound(constraint, ConstraintPropertyNames.Min);
        var max = ReadOptionalInt32Bound(constraint, ConstraintPropertyNames.Max);
        if (!min.HasValue && !max.HasValue)
        {
            return true;
        }

        if (value.Kind != FormValueKind.Integer)
        {
            return false;
        }

        return (!min.HasValue || value.Integer >= min.Value)
            && (!max.HasValue || value.Integer <= max.Value);
    }

    private static bool IsCompareSatisfied(ConstraintDefinition constraint, FormValue value)
    {
        if (!FormConstraintJsonValueReader.TryGetAdditionalProperty(constraint, ConstraintPropertyNames.Operator, out var operatorElement)
            || !FormConstraintJsonValueReader.TryGetAdditionalProperty(constraint, ConstraintPropertyNames.Value, out var expectedElement))
        {
            return true;
        }

        if (!FormConstraintJsonValueReader.TryGetString(operatorElement, out var operatorValue)
            || !FormRuleEngine.TryParseConstValue(expectedElement, out var expectedValue)
            || value.Kind != FormValueKind.Integer
            || expectedValue.Kind != FormValueKind.Integer)
        {
            return false;
        }

        return FormIntegerComparisonOperatorEvaluator.TryEvaluate(operatorValue, value.Integer, expectedValue.Integer, out var result)
            && result;
    }

    private static int? ReadOptionalInt32Bound(ConstraintDefinition constraint, string propertyName)
    {
        if (!FormConstraintJsonValueReader.TryGetAdditionalProperty(constraint, propertyName, out var element))
        {
            return null;
        }

        return FormConstraintJsonValueReader.TryGetInt32(element, out var value)
            ? value
            : throw new FormConstraintAuthoringException(
                $"Constraint '{constraint.ConstraintId}' has invalid '{propertyName}' value.");
    }
}
