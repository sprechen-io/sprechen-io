using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet Bedingungen ihren Prüfern zu und weist nicht unterstützte Arten zurück. Unterstützte Wertbedingungen werden erst geprüft, wenn der Pflichtwertprüfer einen vorhandenen Wert feststellt.
/// </summary>
internal sealed class FormConstraintKindDispatcher
{
    private readonly FormRequiredConstraintEvaluator requiredConstraintEvaluator;
    private readonly FormOneOfConstraintEvaluator oneOfConstraintEvaluator;
    private readonly FormAllOrNoneConstraintEvaluator allOrNoneConstraintEvaluator;
    private readonly FormCompareVariablesConstraintEvaluator compareVariablesConstraintEvaluator;
    private readonly FormSetValueConstraintEvaluator setValueConstraintEvaluator;

    public FormConstraintKindDispatcher(
        IReadOnlyDictionary<string, string> rawValuesByVariableId,
        Func<string, FormValue?> getVariableValue)
    {
        requiredConstraintEvaluator = new FormRequiredConstraintEvaluator(rawValuesByVariableId);
        oneOfConstraintEvaluator = new FormOneOfConstraintEvaluator();
        var variableValuePresenceResolver = new FormVariableValuePresenceResolver(getVariableValue);
        allOrNoneConstraintEvaluator = new FormAllOrNoneConstraintEvaluator(variableValuePresenceResolver);
        compareVariablesConstraintEvaluator = new FormCompareVariablesConstraintEvaluator(getVariableValue);
        setValueConstraintEvaluator = new FormSetValueConstraintEvaluator(rawValuesByVariableId);
    }

    public bool IsConstraintSatisfied(
        ConstraintDefinition constraint,
        string variableId,
        FormValue value,
        bool isValueSet,
        string valueTypeKind)
    {
        var kind = constraint.Kind;
        if (kind == ConstraintKind.Unknown)
        {
            throw new InvalidOperationException("Unsupported constraint kind '<missing>'.");
        }

        switch (kind)
        {
            case ConstraintKind.Required:
                return requiredConstraintEvaluator.IsSatisfied(variableId, value, isValueSet, valueTypeKind);
            case ConstraintKind.AllOrNone:
                return allOrNoneConstraintEvaluator.IsSatisfied(constraint);
            case ConstraintKind.CompareVariables:
                return compareVariablesConstraintEvaluator.IsSatisfied(constraint);
            case ConstraintKind.CustomConstraint:
                throw new InvalidOperationException($"Unsupported constraint kind '{kind.ToWireToken()}'.");
        }

        return IsValueRuleSatisfied(kind, constraint, variableId, value, isValueSet, valueTypeKind);
    }

    private bool IsValueRuleSatisfied(
        ConstraintKind kind,
        ConstraintDefinition constraint,
        string variableId,
        FormValue value,
        bool isValueSet,
        string valueTypeKind)
    {
        // Nicht unterstützte Wertbedingungen werden vor der Prüfung auf einen fehlenden
        // Wert zurückgewiesen. So hängt ihre Ablehnung nicht davon ab, ob das Feld
        // gerade leer oder gefüllt ist.
        var wireToken = kind.ToWireToken();
        if (kind != ConstraintKind.OneOf && !FormSetValueConstraintEvaluator.Supports(wireToken))
        {
            throw new InvalidOperationException($"Unsupported constraint kind '{wireToken}'.");
        }

        // Wertbedingungen werden erst bei vorhandenem Wert geprüft. Ob ein Wert fehlt,
        // entscheidet derselbe Pflichtwertprüfer. Form-, Längen-, Bereichs- oder
        // Auswahlbedingungen erzeugen dadurch keinen zusätzlichen Verstoß allein
        // wegen dieses fehlenden Werts.
        if (!requiredConstraintEvaluator.IsSatisfied(variableId, value, isValueSet, valueTypeKind))
        {
            return true;
        }

        if (kind == ConstraintKind.OneOf)
        {
            return oneOfConstraintEvaluator.IsSatisfied(constraint, value);
        }

        return setValueConstraintEvaluator.IsSatisfied(wireToken, constraint, variableId, value);
    }
}
