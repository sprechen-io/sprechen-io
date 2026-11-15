using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft die Eignung gebundener Werttypen für <c>codeStyle</c> sowie ausgewählte
/// Beziehungen zwischen Mindest-, Höchst- und Genauigkeitsangaben der Werttypen.
/// </summary>
internal sealed class FormBindingValueTypeValidator
{
    public void Validate(FormContract contract, FormContractPreflightIndex index, List<PreflightIssue> errors)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(errors);
        ValidateBindingTypes(contract, index.VariablesById, errors);
        ValidateValueTypeInvariants(contract, errors);
    }
    private static void ValidateBindingTypes(FormContract contract, IReadOnlyDictionary<string, VariableDefinition> variablesById, List<PreflightIssue> errors)
    {
        for (var i = 0; i < contract.Fields.Count; i++)
        {
            var field = contract.Fields[i];
            if (!variablesById.TryGetValue(field.VariableId, out var variable))
            {
                continue;
            }

            var valueTypeKind = PreflightJsonValueReader.TryGetKind(variable.ValueType);
            if (field.CodeStyle && !FieldInteractionSemantics.AllowsCodeStyle(valueTypeKind))
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorBindingTypeMismatch,
                    Message: $"codeStyle requires valueType 'string', but field '{field.FieldId}' is bound to '{valueTypeKind ?? "<missing>"}'.",
                    Path: $"$.fields[{i}]",
                    Id: field.FieldId));
            }
        }
    }

    private static void ValidateValueTypeInvariants(FormContract contract, List<PreflightIssue> errors)
    {
        for (var i = 0; i < contract.Variables.Count; i++)
        {
            var variable = contract.Variables[i];
            var vt = variable.ValueType;
            if (vt.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var kind = PreflightJsonValueReader.TryGetKind(vt);

            switch (kind)
            {
                case "string":
                    {
                        var min = PreflightJsonValueReader.TryGetInt(vt, "minLength");
                        var max = PreflightJsonValueReader.TryGetInt(vt, "maxLength");
                        if (min.HasValue && max.HasValue && min.Value > max.Value)
                        {
                            errors.Add(new PreflightIssue(
                                Code: PreflightCodes.ErrorSettingsInvalid,
                                Message: $"minLength ({min.Value}) must not exceed maxLength ({max.Value}) for variable '{variable.VariableId}'.",
                                Path: $"$.variables[{i}].valueType",
                                Id: variable.VariableId));
                        }

                        break;
                    }

                case "set":
                    {
                        var min = PreflightJsonValueReader.TryGetInt(vt, "minSelected");
                        var max = PreflightJsonValueReader.TryGetInt(vt, "maxSelected");
                        if (min.HasValue && max.HasValue && min.Value > max.Value)
                        {
                            errors.Add(new PreflightIssue(
                                Code: PreflightCodes.ErrorSettingsInvalid,
                                Message: $"minSelected ({min.Value}) must not exceed maxSelected ({max.Value}) for variable '{variable.VariableId}'.",
                                Path: $"$.variables[{i}].valueType",
                                Id: variable.VariableId));
                        }

                        break;
                    }

                case "list":
                    {
                        var min = PreflightJsonValueReader.TryGetInt(vt, "minItems");
                        var max = PreflightJsonValueReader.TryGetInt(vt, "maxItems");
                        if (min.HasValue && max.HasValue && min.Value > max.Value)
                        {
                            errors.Add(new PreflightIssue(
                                Code: PreflightCodes.ErrorSettingsInvalid,
                                Message: $"minItems ({min.Value}) must not exceed maxItems ({max.Value}) for variable '{variable.VariableId}'.",
                                Path: $"$.variables[{i}].valueType",
                                Id: variable.VariableId));
                        }

                        break;
                    }

                case "decimal":
                    {
                        var precision = PreflightJsonValueReader.TryGetInt(vt, "precision");
                        var scale = PreflightJsonValueReader.TryGetInt(vt, "scale");
                        if (precision.HasValue && scale.HasValue && scale.Value > precision.Value)
                        {
                            errors.Add(new PreflightIssue(
                                Code: PreflightCodes.ErrorSettingsInvalid,
                                Message: $"scale ({scale.Value}) must not exceed precision ({precision.Value}) for variable '{variable.VariableId}'.",
                                Path: $"$.variables[{i}].valueType",
                                Id: variable.VariableId));
                        }

                        break;
                    }
            }
        }
    }

}
