using Sprechen.Mda.Contracts.V1.Variables;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft, ob die Werttypkennung einer wiederverwendbaren Variable
/// zum unterstützten Modellumfang gehört.
/// </summary>
internal sealed class ReusableVariableValueTypeValidator
{
    private static readonly IReadOnlySet<string> SupportedValueTypeKinds = ReferenceImplementationSupportProfile.SupportedValueTypeKinds;

    private readonly ReusableVariableValueTypeKindReader _kindReader = new();

    public void Validate(List<PreflightIssue> errors, ReusableVariableDefinition definition, int definitionIndex)
    {
        var kind = _kindReader.TryRead(definition.ValueType);
        if (!string.IsNullOrWhiteSpace(kind) && SupportedValueTypeKinds.Contains(kind))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorFeatureNotSupported,
            Message: $"Unsupported valueType kind '{kind ?? "<missing>"}' in reusable variable '{definition.VariableDefinitionId}'.",
            Path: $"$.definitions[{definitionIndex}].valueType",
            Id: definition.VariableDefinitionId));
    }
}
