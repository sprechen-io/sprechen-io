using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet eine Bedingung dem abgefragten Feld oder seiner Variablen zu. Bedingungen mit Formularziel gelten für jede solche Zuordnung.
/// </summary>
internal sealed class FormConstraintTargetMatcher
{
    public bool IsRelevantToField(ConstraintDefinition constraint, string fieldId, string variableId)
    {
        ArgumentNullException.ThrowIfNull(constraint);

        if (string.Equals(constraint.Target.Kind, TargetKinds.Field, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(constraint.Target.FieldId, fieldId, StringComparison.Ordinal);
        }

        if (string.Equals(constraint.Target.Kind, TargetKinds.Variable, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(constraint.Target.VariableId, variableId, StringComparison.Ordinal);
        }

        if (string.Equals(constraint.Target.Kind, TargetKinds.Form, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
