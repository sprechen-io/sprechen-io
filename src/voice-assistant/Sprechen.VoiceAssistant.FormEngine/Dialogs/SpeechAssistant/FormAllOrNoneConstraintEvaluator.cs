using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft für die angegebenen Variablen, ob entweder alle einen Wert tragen oder alle ohne Wert sind. Eine fehlende Variablenliste stellt keine zusätzliche Bedingung.
/// </summary>
internal sealed class FormAllOrNoneConstraintEvaluator
{
    private readonly FormVariableValuePresenceResolver variableValuePresenceResolver;

    public FormAllOrNoneConstraintEvaluator(FormVariableValuePresenceResolver variableValuePresenceResolver)
    {
        this.variableValuePresenceResolver = variableValuePresenceResolver
            ?? throw new ArgumentNullException(nameof(variableValuePresenceResolver));
    }

    public bool IsSatisfied(ConstraintDefinition constraint)
    {
        if (!FormConstraintJsonValueReader.TryGetAdditionalProperty(constraint, ConstraintPropertyNames.VariableIds, out var variableIdsElement))
        {
            return true;
        }

        if (variableIdsElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var anySet = false;
        var anyUnset = false;

        foreach (var element in variableIdsElement.EnumerateArray())
        {
            if (!FormConstraintJsonValueReader.TryGetString(element, out var relatedVariableId))
            {
                return false;
            }

            if (variableValuePresenceResolver.IsVariableValueSet(relatedVariableId))
            {
                anySet = true;
            }
            else
            {
                anyUnset = true;
            }

            if (anySet && anyUnset)
            {
                return false;
            }
        }

        return true;
    }
}
