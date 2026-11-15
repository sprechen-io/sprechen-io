using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Variables;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft die Auflösbarkeit der Datenschutzregeln wiederverwendbarer Variablen
/// und meldet unzulässige Kombinationen.
/// </summary>
internal sealed class ReusableVariableSensitiveDataPolicyValidator
{
    private readonly EffectiveSensitiveDataPolicyResolver _policyResolver = new();

    public void Validate(List<PreflightIssue> errors, ReusableVariableDefinition definition, int definitionIndex)
    {
        EffectiveSensitiveDataPolicyPreflightMapper.TryAddResolvedOrIssue(
            _policyResolver,
            definition.PrivacyClass,
            definition.SensitiveDataPolicy,
            errors,
            PreflightCodes.ErrorSettingsInvalid,
            definition.VariableDefinitionId,
            $"$.definitions[{definitionIndex}].privacyClass",
            $"$.definitions[{definitionIndex}].sensitiveDataPolicy.voiceInputPolicy",
            $"$.definitions[{definitionIndex}].sensitiveDataPolicy.readbackPolicy",
            $"$.definitions[{definitionIndex}].sensitiveDataPolicy.silentInputFallback",
            $"Reusable variable '{definition.VariableDefinitionId}'");
    }
}
