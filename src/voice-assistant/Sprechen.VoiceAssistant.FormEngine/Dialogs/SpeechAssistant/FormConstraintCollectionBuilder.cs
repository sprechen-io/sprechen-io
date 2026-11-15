using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Führt Vertrags-, Feld-, Werttyp- und Variablenbedingungen zu einer geordneten Sammlung zusammen, damit nachgelagerte Prüfungen aus derselben Liste lesen.
/// </summary>
internal static class FormConstraintCollectionBuilder
{
    public static ImmutableArray<ConstraintDefinition> Build(FormContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        var constraints = new List<ConstraintDefinition>();
        constraints.AddRange(contract.Constraints);

        foreach (var field in contract.Fields)
        {
            if (field?.FieldConstraints is null)
            {
                continue;
            }

            constraints.AddRange(field.FieldConstraints);
        }

        foreach (var variable in contract.Variables)
        {
            if (variable is null)
            {
                continue;
            }

            // Auch Grenzen aus dem Werttyp werden hier in die gemeinsame Bedingungsliste
            // aufgenommen, damit nachgelagerte Prüfstellen sie nicht jeweils zusätzlich
            // aus der Werttypdefinition zusammensuchen müssen.
            constraints.AddRange(FormValueTypeStatedRules.From(variable));

            if (variable.InvariantConstraints is null)
            {
                continue;
            }

            foreach (var rawConstraint in variable.InvariantConstraints)
            {
                if (rawConstraint.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException(
                        $"Invariant constraint payload for variable '{variable.VariableId}' must be a JSON object but was '{rawConstraint.ValueKind}'.");
                }

                ConstraintDefinition parsed;
                try
                {
                    parsed = rawConstraint.Deserialize<ConstraintDefinition>()
                        ?? throw new InvalidOperationException(
                            $"Invariant constraint payload for variable '{variable.VariableId}' could not be deserialized.");
                }
                catch (JsonException ex)
                {
                    throw new InvalidOperationException(
                        $"Invariant constraint payload for variable '{variable.VariableId}' could not be deserialized.",
                        ex);
                }

                if (parsed.Target is null)
                {
                    throw new InvalidOperationException(
                        $"Invariant constraint payload for variable '{variable.VariableId}' is missing a target object.");
                }

                constraints.Add(parsed);
            }
        }

        return constraints.ToImmutableArray();
    }
}
