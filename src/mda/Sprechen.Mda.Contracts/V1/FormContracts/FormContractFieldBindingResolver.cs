using Sprechen.Mda.Contracts.V1.Fields;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Löst Feldbindungen gegen den Feldkatalog auf und passt Regelziele
/// von der wiederverwendbaren Definition an die gebundene Feldkennung an.
/// </summary>
internal static class FormContractFieldBindingResolver
{
    internal static List<FieldDefinition> Resolve(
        IReadOnlyList<FormFieldBindingDefinition> bindings,
        IReadOnlyDictionary<string, ReusableFieldDefinition> definitionsById)
    {
        var resolvedFields = new List<FieldDefinition>(bindings.Count);
        foreach (var binding in bindings)
        {
            if (!definitionsById.TryGetValue(binding.FieldDefinitionId, out var definition))
            {
                throw new InvalidOperationException(
                    $"FormContract references missing fieldDefinitionId '{binding.FieldDefinitionId}'.");
            }

            resolvedFields.Add(new FieldDefinition
            {
                FieldId = binding.FieldId,
                VariableId = binding.VariableId,
                FieldDefinitionId = binding.FieldDefinitionId,
                Label = FormContractResolutionJsonCloner.CloneRequiredLocalizedText(definition.Label),
                HelpText = FormContractResolutionJsonCloner.CloneOptionalLocalizedText(definition.HelpText),
                Placeholder = FormContractResolutionJsonCloner.CloneOptionalLocalizedText(definition.Placeholder),
                Examples = FormContractResolutionJsonCloner.CloneLocalizedTextList(definition.Examples),
                Editability = definition.Editability,
                CodeStyle = definition.CodeStyle == true,
                PreferredInputModes = definition.PreferredInputModes is null ? null : [.. definition.PreferredInputModes],
                FieldConstraints = CloneFieldConstraints(definition.FieldConstraints, binding.FieldId, definition.FieldDefinitionId),
                PresentationHints = CloneFieldPresentationHints(definition.PresentationHints),
                Projections = CloneFieldProjections(definition.Projections),
                Trace = binding.Trace ?? definition.Trace,
                Extensions = FormContractResolutionJsonCloner.CloneExtensions(binding.Extensions ?? definition.Extensions)
            });
        }

        return resolvedFields;
    }

    private static List<ConstraintDefinition>? CloneFieldConstraints(
        List<ConstraintDefinition>? constraints,
        string resolvedFieldId,
        string sourceFieldDefinitionId)
    {
        if (constraints is null)
        {
            return null;
        }

        var clones = new List<ConstraintDefinition>(constraints.Count);
        foreach (var constraint in constraints)
        {
            clones.Add(new ConstraintDefinition
            {
                ConstraintId = constraint.ConstraintId,
                Target = new ConstraintTargetRef
                {
                    Kind = constraint.Target.Kind,
                    FieldId = RewriteFieldConstraintTarget(constraint.Target.FieldId, resolvedFieldId, sourceFieldDefinitionId) is { } rewrittenFieldId
                        ? FieldId.From(rewrittenFieldId)
                        : null,
                    VariableId = constraint.Target.VariableId
                },
                Severity = constraint.Severity,
                Code = constraint.Code,
                Message = FormContractResolutionJsonCloner.CloneRequiredLocalizedText(constraint.Message),
                Remediation = FormContractResolutionJsonCloner.CloneOptionalLocalizedText(constraint.Remediation),
                When = FormContractResolutionJsonCloner.CloneJsonElement(constraint.When),
                Kind = constraint.Kind,
                Scope = constraint.Scope,
                AdditionalProperties = FormContractResolutionJsonCloner.CloneExtensions(constraint.AdditionalProperties)
            });
        }

        return clones;
    }

    private static string? RewriteFieldConstraintTarget(string? targetFieldId, string resolvedFieldId, string sourceFieldDefinitionId)
    {
        if (string.IsNullOrWhiteSpace(targetFieldId))
        {
            return targetFieldId;
        }

        return string.Equals(targetFieldId, sourceFieldDefinitionId, StringComparison.Ordinal)
            ? resolvedFieldId
            : targetFieldId;
    }

    private static FieldPresentationHints? CloneFieldPresentationHints(FieldPresentationHints? hints)
    {
        if (hints is null)
        {
            return null;
        }

        return new FieldPresentationHints
        {
            MultilinePreferred = hints.MultilinePreferred
        };
    }

    private static FieldProjections? CloneFieldProjections(FieldProjections? projections)
    {
        if (projections is null)
        {
            return null;
        }

        return new FieldProjections
        {
            Html = CloneFieldHtmlProjection(projections.Html),
            Voice = CloneFieldVoiceProjection(projections.Voice)
        };
    }

    private static FieldHtmlProjection? CloneFieldHtmlProjection(FieldHtmlProjection? projection)
    {
        if (projection is null)
        {
            return null;
        }

        return new FieldHtmlProjection
        {
            InputMode = projection.InputMode,
            Autocomplete = projection.Autocomplete,
            AccessibleName = FormContractResolutionJsonCloner.CloneOptionalLocalizedText(projection.AccessibleName),
            AccessibleDescription = FormContractResolutionJsonCloner.CloneOptionalLocalizedText(projection.AccessibleDescription)
        };
    }

    private static FieldVoiceProjection? CloneFieldVoiceProjection(FieldVoiceProjection? projection)
    {
        if (projection is null)
        {
            return null;
        }

        return new FieldVoiceProjection
        {
            Names = FormContractResolutionJsonCloner.CloneOptionalLocalizedText(projection.Names),
            Synonyms = FormContractResolutionJsonCloner.CloneLocalizedTextList(projection.Synonyms),
            ConfirmationStyle = projection.ConfirmationStyle
        };
    }
}
