using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft die Arten globaler Formularbedingungen gegen den unterstützten Modellumfang.
/// </summary>
internal sealed class FormGlobalConstraintSupportedSubsetValidator
{
    private static readonly IReadOnlySet<string> SupportedConstraintKinds = ReferenceImplementationSupportProfile.SupportedFormContractConstraintKinds;

    public void Validate(FormContract contract, List<PreflightIssue> errors)
    {
        for (var i = 0; i < contract.Constraints.Count; i++)
        {
            var constraint = contract.Constraints[i];
            if (constraint.Kind == ConstraintKind.Unknown
                || !SupportedConstraintKinds.Contains(constraint.Kind.ToWireToken()))
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorFeatureNotSupported,
                    Message: $"Unsupported constraint kind '{constraint.Kind}' (constraintId '{constraint.ConstraintId}').",
                    Path: $"$.constraints[{i}].kind",
                    Id: constraint.ConstraintId));
            }
        }
    }
}
