using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Datenschutzregeln und dazu passende Bestätigungs- und Eingabemodi
/// der an Variablen gebundenen Felder.
/// </summary>
internal sealed class FormPrivacyValidator
{
    private static readonly HashSet<string> ForbiddenConfirmationStylesForSensitiveData = new(StringComparer.OrdinalIgnoreCase)
    {
        "repeatBack",
        "spellOut"
    };

    private static readonly HashSet<string> MicrophoneInputModes = new(StringComparer.Ordinal)
    {
        InputModes.Voice,
        InputModes.Spelling,
        InputModes.Digits,
        InputModes.Literal
    };

    private readonly EffectiveSensitiveDataPolicyResolver _policyResolver = new();

    public void Validate(FormContract contract, FormContractPreflightIndex index, List<PreflightIssue> errors)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(errors);
        ValidatePrivacyGuardrails(contract, index.VariablesById, errors);
    }

    private void ValidatePrivacyGuardrails(
        FormContract contract,
        IReadOnlyDictionary<string, VariableDefinition> variablesById,
        List<PreflightIssue> errors)
    {
        foreach (var variable in variablesById.Values)
        {
            if (!_policyResolver.TryResolve(
                    variable.PrivacyClass,
                    variable.SensitiveDataPolicy,
                    out _,
                    out var failure))
            {
                EffectiveSensitiveDataPolicyPreflightMapper.AddFailure(
                    errors,
                    failure,
                    PreflightCodes.ErrorPrivacyGuardrailViolation,
                    variable.VariableId,
                    "$.variables[*].privacyClass",
                    "$.variables[*].sensitiveDataPolicy.voiceInputPolicy",
                    "$.variables[*].sensitiveDataPolicy.readbackPolicy",
                    "$.variables[*].sensitiveDataPolicy.silentInputFallback",
                    $"Variable '{variable.VariableId}'",
                    variable.SensitiveDataPolicy);
            }

            // Bestätigungsstil und Mikrofoneingabe werden anhand der deklarierten
            // Datenschutzklasse geprüft, auch wenn die Auflösung einzelner Regelwerte
            // fehlgeschlagen ist. Diese Prüfungen benötigen keine aufgelösten Werte.
            if (variable.PrivacyClass is not (PrivacyClass.Sensitive or PrivacyClass.Secret))
            {
                continue;
            }

            var privacyWire = variable.PrivacyClass == PrivacyClass.Secret
                ? PrivacyClasses.Secret
                : PrivacyClasses.Sensitive;

            foreach (var field in contract.Fields.Where(f => string.Equals(f.VariableId, variable.VariableId, StringComparison.Ordinal)))
            {
                var confirmationStyle = field.Projections?.Voice?.ConfirmationStyle;
                if (!string.IsNullOrWhiteSpace(confirmationStyle)
                    && ForbiddenConfirmationStylesForSensitiveData.Contains(confirmationStyle.Trim()))
                {
                    errors.Add(new PreflightIssue(
                        Code: PreflightCodes.ErrorPrivacyGuardrailViolation,
                        Message: $"Field '{field.FieldId}' uses confirmationStyle '{confirmationStyle}', which is forbidden for privacyClass '{privacyWire}'.",
                        Path: "$.fields[*].projections.voice.confirmationStyle",
                        Id: field.FieldId));
                }

                if (field.PreferredInputModes is null)
                {
                    continue;
                }

                if (privacyWire == PrivacyClasses.Secret)
                {
                    var microphoneMode = field.PreferredInputModes.FirstOrDefault(mode =>
                        MicrophoneInputModes.Contains(mode));
                    if (microphoneMode is not null)
                    {
                        errors.Add(new PreflightIssue(
                            Code: PreflightCodes.ErrorPrivacyGuardrailViolation,
                            Message: $"Field '{field.FieldId}' is bound to privacyClass=secret but prefers microphone input mode '{microphoneMode}'.",
                            Path: "$.fields[*].preferredInputModes",
                            Id: field.FieldId));
                    }
                }

            }
        }
    }
}
