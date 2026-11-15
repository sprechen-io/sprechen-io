using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Bestimmt den aufgabenweiten Abweichungscode aus Pflichtstand, Sichtbarkeit
/// und dem Vergleich jeder zweigliedrigen Aufgabenkarte. Soll- und Istwerte
/// verlassen diese Funktion nicht.
/// </summary>
public static class TaskCardMismatchAssessor
{
    public static string Assess(
        FormContract contract,
        IReadOnlyDictionary<string, string> valuesByVariableId,
        IReadOnlySet<string> relevantFieldIds,
        bool processSubmitted,
        int requiredFieldsTotal,
        int requiredFieldsCompleted,
        int openBlockingValidationCount,
        string terminationClassification)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(valuesByVariableId);
        ArgumentNullException.ThrowIfNull(relevantFieldIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(terminationClassification);

        if (terminationClassification == TelemetryTaskTerminationClassifications.TechnicalFailure
            || terminationClassification == TelemetryTaskTerminationClassifications.StudyDirectedAbort)
        {
            return TelemetryMismatchCodes.NotAssessed;
        }

        var walk = Walk(contract, valuesByVariableId, relevantFieldIds);
        if (requiredFieldsTotal > 0 && requiredFieldsCompleted < requiredFieldsTotal)
        {
            return TelemetryMismatchCodes.MissingRequired;
        }

        if (walk.WrongFieldClass)
        {
            return TelemetryMismatchCodes.WrongFieldClass;
        }

        if (walk.WrongDependencyState)
        {
            return TelemetryMismatchCodes.WrongDependencyState;
        }

        if (openBlockingValidationCount > 0)
        {
            return TelemetryMismatchCodes.ValidationErrorOpen;
        }

        if (!processSubmitted)
        {
            return TelemetryMismatchCodes.NotSubmitted;
        }

        if (walk.Undecidable)
        {
            return TelemetryMismatchCodes.NotAssessed;
        }

        return walk.TaskCardMismatch
            ? TelemetryMismatchCodes.TaskCardMismatch
            : TelemetryMismatchCodes.None;
    }

    private static (bool WrongFieldClass, bool WrongDependencyState, bool TaskCardMismatch, bool Undecidable) Walk(
        FormContract contract,
        IReadOnlyDictionary<string, string> valuesByVariableId,
        IReadOnlySet<string> relevantFieldIds)
    {
        var fieldsById = contract.Fields.ToDictionary(field => field.FieldId.Value, StringComparer.Ordinal);
        var variablesById = contract.Variables.ToDictionary(variable => variable.VariableId, variable => variable);
        var optionsResolver = new FormChoiceOptionsResolver(variablesById, TaskCardChoiceOptions.ResolveLabel);
        var wrongFieldClass = false;
        var wrongDependencyState = false;
        var taskCardMismatch = false;
        var undecidable = false;

        foreach (var fieldGroup in contract.FieldGroups)
        {
            var memberFieldIds = fieldGroup.Members
                .Where(member => string.Equals(member.Kind, MemberKinds.Field, StringComparison.Ordinal)
                    && member.FieldId is not null)
                .Select(member => member.FieldId!.Value.Value)
                .ToArray();
            if (memberFieldIds.Length != 2
                || !fieldsById.TryGetValue(memberFieldIds[0], out var instruction)
                || !fieldsById.TryGetValue(memberFieldIds[1], out var answer)
                || instruction.Editability is not FieldEditability.ReadOnly
                || answer.Editability is not FieldEditability.UserEditable)
            {
                continue;
            }

            var instructionRelevant = relevantFieldIds.Contains(instruction.FieldId.Value);
            var answerRelevant = relevantFieldIds.Contains(answer.FieldId.Value);
            if (instructionRelevant != answerRelevant)
            {
                wrongDependencyState = true;
                continue;
            }

            if (!answerRelevant)
            {
                continue;
            }

            var target = ReadTarget(variablesById, instruction);
            if (target is null)
            {
                undecidable = true;
                continue;
            }

            valuesByVariableId.TryGetValue(answer.VariableId.Value, out var actual);
            actual ??= string.Empty;
            var options = TaskCardChoiceOptions.Resolve(optionsResolver, variablesById, answer);
            if (options.Length > 0)
            {
                var coded = TaskCardCodedValueComparer.Compare(actual, target, options);
                if (coded == TelemetryMismatchCodes.WrongFieldClass)
                {
                    wrongFieldClass = true;
                }
                else if (coded == TelemetryMismatchCodes.TaskCardMismatch)
                {
                    taskCardMismatch = true;
                }

                continue;
            }

            if (!string.Equals(TaskCardComparisonKey.From(actual), TaskCardComparisonKey.From(target), StringComparison.Ordinal))
            {
                taskCardMismatch = true;
            }
        }

        return (wrongFieldClass, wrongDependencyState, taskCardMismatch, undecidable);
    }

    private static string? ReadTarget(
        IReadOnlyDictionary<VariableId, VariableDefinition> variablesById,
        FieldDefinition instruction)
    {
        if (!variablesById.TryGetValue(instruction.VariableId, out var variable)
            || variable.InitialValue is not JsonElement initial
            || initial.ValueKind is not JsonValueKind.String)
        {
            return null;
        }

        return initial.GetString();
    }
}
