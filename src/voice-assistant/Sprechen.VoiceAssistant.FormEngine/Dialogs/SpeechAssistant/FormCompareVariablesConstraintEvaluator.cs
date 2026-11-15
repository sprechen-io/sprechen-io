using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Vergleicht vorhandene ganzzahlige Werte oder Datumswerte zweier Variablen über denselben Ordnungsoperator. Bei fehlendem Operanden wird noch kein Vergleichsverstoß gemeldet.
/// </summary>
internal sealed class FormCompareVariablesConstraintEvaluator
{
    private readonly Func<string, FormValue?> getVariableValue;

    public FormCompareVariablesConstraintEvaluator(Func<string, FormValue?> getVariableValue)
    {
        this.getVariableValue = getVariableValue ?? throw new ArgumentNullException(nameof(getVariableValue));
    }

    public bool IsSatisfied(ConstraintDefinition constraint)
    {
        if (!TryReadComparisonDefinition(constraint, out var leftVariableId, out var rightVariableId, out var operatorValue))
        {
            return true;
        }

        if (leftVariableId is null || rightVariableId is null || operatorValue is null)
        {
            return false;
        }

        var leftValue = getVariableValue(leftVariableId);
        var rightValue = getVariableValue(rightVariableId);
        if (leftValue is null || rightValue is null)
        {
            return true;
        }

        if (leftValue.Value.Kind == FormValueKind.Integer && rightValue.Value.Kind == FormValueKind.Integer)
        {
            return FormIntegerComparisonOperatorEvaluator.TryEvaluate(operatorValue, leftValue.Value.Integer, rightValue.Value.Integer, out var integerResult)
                && integerResult;
        }

        // ISO-Datumswerte werden über ihre fortlaufenden Tagesnummern mit denselben Vergleichsoperatoren wie Ganzzahlen verglichen.
        if (FormConstraintJsonValueReader.TryGetDateOnly(leftValue.Value, out var leftDate)
            && FormConstraintJsonValueReader.TryGetDateOnly(rightValue.Value, out var rightDate))
        {
            return FormIntegerComparisonOperatorEvaluator.TryEvaluate(operatorValue, leftDate.DayNumber, rightDate.DayNumber, out var dateResult)
                && dateResult;
        }

        return false;
    }

    private static bool TryReadComparisonDefinition(
        ConstraintDefinition constraint,
        out string? leftVariableId,
        out string? rightVariableId,
        out string? operatorValue)
    {
        leftVariableId = null;
        rightVariableId = null;
        operatorValue = null;

        if (!FormConstraintJsonValueReader.TryGetAdditionalProperty(constraint, ConstraintPropertyNames.LeftVariableId, out var leftVariableElement)
            || !FormConstraintJsonValueReader.TryGetAdditionalProperty(constraint, ConstraintPropertyNames.RightVariableId, out var rightVariableElement)
            || !FormConstraintJsonValueReader.TryGetAdditionalProperty(constraint, ConstraintPropertyNames.Operator, out var operatorElement))
        {
            return false;
        }

        if (!FormConstraintJsonValueReader.TryGetString(leftVariableElement, out leftVariableId)
            || !FormConstraintJsonValueReader.TryGetString(rightVariableElement, out rightVariableId)
            || !FormConstraintJsonValueReader.TryGetString(operatorElement, out operatorValue))
        {
            return true;
        }

        return true;
    }
}
