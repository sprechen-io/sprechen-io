using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Datumswerte gegen vorhandene Grenzen einschließlich der Grenzwerte, damit Formularbedingungen denselben Vergleich verwenden.
/// </summary>
internal sealed class FormDateSetValueConstraintEvaluator
{
    internal bool Supports(string kind)
    {
        return string.Equals(kind, ConstraintKinds.DateMin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(kind, ConstraintKinds.DateMax, StringComparison.OrdinalIgnoreCase)
            || string.Equals(kind, ConstraintKinds.DateRange, StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsSatisfied(string kind, ConstraintDefinition constraint, FormValue value)
    {
        if (string.Equals(kind, ConstraintKinds.DateMin, StringComparison.OrdinalIgnoreCase))
        {
            return IsDateMinSatisfied(constraint, value);
        }

        if (string.Equals(kind, ConstraintKinds.DateMax, StringComparison.OrdinalIgnoreCase))
        {
            return IsDateMaxSatisfied(constraint, value);
        }

        if (string.Equals(kind, ConstraintKinds.DateRange, StringComparison.OrdinalIgnoreCase))
        {
            return IsDateRangeSatisfied(constraint, value);
        }

        throw new InvalidOperationException($"Unsupported date constraint kind '{kind}'.");
    }

    private static bool IsDateMinSatisfied(ConstraintDefinition constraint, FormValue value)
    {
        var minDate = ReadOptionalDateBound(constraint, ConstraintPropertyNames.MinDate);
        if (!minDate.HasValue)
        {
            return true;
        }

        if (!FormConstraintJsonValueReader.TryGetDateOnly(value, out var actualDate))
        {
            return false;
        }

        return actualDate >= minDate.Value;
    }

    private static bool IsDateMaxSatisfied(ConstraintDefinition constraint, FormValue value)
    {
        var maxDate = ReadOptionalDateBound(constraint, ConstraintPropertyNames.MaxDate);
        if (!maxDate.HasValue)
        {
            return true;
        }

        if (!FormConstraintJsonValueReader.TryGetDateOnly(value, out var actualDate))
        {
            return false;
        }

        return actualDate <= maxDate.Value;
    }

    private static bool IsDateRangeSatisfied(ConstraintDefinition constraint, FormValue value)
    {
        var minDate = ReadOptionalDateBound(constraint, ConstraintPropertyNames.MinDate);
        var maxDate = ReadOptionalDateBound(constraint, ConstraintPropertyNames.MaxDate);
        if (!minDate.HasValue && !maxDate.HasValue)
        {
            return true;
        }

        if (!FormConstraintJsonValueReader.TryGetDateOnly(value, out var actualDate))
        {
            return false;
        }

        return (!minDate.HasValue || actualDate >= minDate.Value)
            && (!maxDate.HasValue || actualDate <= maxDate.Value);
    }

    private static DateOnly? ReadOptionalDateBound(ConstraintDefinition constraint, string propertyName)
    {
        if (!FormConstraintJsonValueReader.TryGetAdditionalProperty(constraint, propertyName, out var element))
        {
            return null;
        }

        return FormConstraintJsonValueReader.TryGetDateOnly(element, out var value)
            ? value
            : throw new FormConstraintAuthoringException(
                $"Constraint '{constraint.ConstraintId}' has invalid '{propertyName}' value.");
    }
}
