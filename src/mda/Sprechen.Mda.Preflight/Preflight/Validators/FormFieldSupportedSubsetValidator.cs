using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft bevorzugte Eingabemodi auf Unterstützung und Eignung für den Feldtyp
/// sowie Feldbedingungen auf unterstützte Arten.
/// </summary>
internal sealed class FormFieldSupportedSubsetValidator
{
    private static readonly IReadOnlySet<string> SupportedInputModes = ReferenceImplementationSupportProfile.SupportedInputModes;
    private static readonly IReadOnlySet<string> SupportedConstraintKinds = ReferenceImplementationSupportProfile.SupportedFormContractConstraintKinds;

    private static readonly IReadOnlySet<string> DigitFriendlyFormatHints = new HashSet<string>(StringComparer.Ordinal)
    {
        "phone",
        "postalCode",
        "pin",
        "digits"
    };

    public void Validate(FormContract contract, List<PreflightIssue> errors)
    {
        var variablesById = contract.Variables
            .GroupBy(variable => variable.VariableId.Value)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        for (var i = 0; i < contract.Fields.Count; i++)
        {
            ValidatePreferredInputModes(contract.Fields[i], i, variablesById, errors);
            ValidateFieldConstraints(contract.Fields[i], i, errors);
        }
    }

    private static void ValidatePreferredInputModes(
        FieldDefinition field,
        int fieldIndex,
        IReadOnlyDictionary<string, VariableDefinition> variablesById,
        List<PreflightIssue> errors)
    {
        if (field.PreferredInputModes is null)
        {
            return;
        }

        for (var j = 0; j < field.PreferredInputModes.Count; j++)
        {
            var inputMode = field.PreferredInputModes[j];
            if (string.IsNullOrWhiteSpace(inputMode) || !SupportedInputModes.Contains(inputMode))
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorFeatureNotSupported,
                    Message: $"Unsupported preferredInputMode '{inputMode}' in field '{field.FieldId}'.",
                    Path: $"$.fields[{fieldIndex}].preferredInputModes[{j}]",
                    Id: field.FieldId));
                continue;
            }

            if (!IsInputModeCompatibleWithField(field, variablesById, inputMode))
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorFeatureNotSupported,
                    Message: $"preferredInputMode '{inputMode}' in field '{field.FieldId}' is not compatible with the field's effective type.",
                    Path: $"$.fields[{fieldIndex}].preferredInputModes[{j}]",
                    Id: field.FieldId));
            }
        }
    }

    // Die bevorzugten Modi müssen zum wirksamen Feldtyp passen: Sprache und Tastatur
    // sind hier für alle Typen zulässig, Buchstabieren und wörtliche Eingabe nur für
    // Text oder Code. Zifferneingabe ist für Code, Zahlen und Text mit geeignetem
    // Formathinweis vorgesehen. Andere Kombinationen können beim Feldeintritt nicht angeboten werden.
    private static bool IsInputModeCompatibleWithField(
        FieldDefinition field,
        IReadOnlyDictionary<string, VariableDefinition> variablesById,
        string inputMode)
    {
        if (inputMode is InputModes.Voice or InputModes.Keyboard)
        {
            return true;
        }

        var valueTypeKind = variablesById.TryGetValue(field.VariableId, out var variable)
            && variable.ValueType.ValueKind == JsonValueKind.Object
            && variable.ValueType.TryGetProperty(ValueTypePropertyNames.Kind, out var kindElement)
            && kindElement.ValueKind == JsonValueKind.String
            ? kindElement.GetString()
            : null;
        var effectiveType = FieldInteractionSemantics.ResolveEffectiveInteractionType(field.CodeStyle, valueTypeKind);

        return inputMode switch
        {
            InputModes.Spelling or InputModes.Literal =>
                effectiveType is EffectiveFieldInteractionTypes.Text or EffectiveFieldInteractionTypes.Code,
            InputModes.Digits =>
                effectiveType is EffectiveFieldInteractionTypes.Code or EffectiveFieldInteractionTypes.Number
                || (effectiveType == EffectiveFieldInteractionTypes.Text
                    && variable is not null
                    && variable.ValueType.TryGetProperty(ValueTypePropertyNames.FormatHint, out var hintElement)
                    && hintElement.ValueKind == JsonValueKind.String
                    && DigitFriendlyFormatHints.Contains(hintElement.GetString()!)),
            _ => false
        };
    }

    private static void ValidateFieldConstraints(FieldDefinition field, int fieldIndex, List<PreflightIssue> errors)
    {
        if (field.FieldConstraints is null)
        {
            return;
        }

        for (var j = 0; j < field.FieldConstraints.Count; j++)
        {
            var constraint = field.FieldConstraints[j];
            if (constraint.Kind == ConstraintKind.Unknown
                || !SupportedConstraintKinds.Contains(constraint.Kind.ToWireToken()))
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorFeatureNotSupported,
                    Message: $"Unsupported constraint kind '{constraint.Kind}' (constraintId '{constraint.ConstraintId}').",
                    Path: $"$.fields[{fieldIndex}].fieldConstraints[{j}].kind",
                    Id: constraint.ConstraintId));
            }
        }
    }
}
