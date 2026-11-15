using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Bedingungsarten und Geltungsbereiche gegen den unterstützten Modellumfang
/// und warnt bei ungenauen Formularzielen feldübergreifender Bedingungen.
/// </summary>
internal static class FormConstraintSupportValidator
{
    private static readonly IReadOnlySet<string> SupportedConstraintKinds = ReferenceImplementationSupportProfile.SupportedFormContractConstraintKinds;
    private static readonly IReadOnlySet<string> SupportedConstraintScopes = ReferenceImplementationSupportProfile.SupportedConstraintScopes;

    internal static void ValidateTypedKindAndScope(
        ConstraintDefinition constraint,
        string path,
        List<PreflightIssue> errors)
    {
        ArgumentNullException.ThrowIfNull(constraint);

        if (constraint.Kind == ConstraintKind.Unknown
            || !SupportedConstraintKinds.Contains(constraint.Kind.ToWireToken()))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorFeatureNotSupported,
                Message: $"Unsupported constraint kind '{constraint.Kind}' (constraintId '{constraint.ConstraintId}').",
                Path: $"{path}.kind",
                Id: constraint.ConstraintId));
        }

        // Fehlender Geltungsbereich ist zulässig. Unknown und bekannte, aber
        // nicht unterstützte Werte werden dagegen gemeldet.
        if (constraint.Scope is not { } scope)
        {
            return;
        }

        if (scope != ConstraintScope.Unknown && SupportedConstraintScopes.Contains(scope.ToWireToken()))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorFeatureNotSupported,
            Message: $"Unsupported constraint scope '{scope}' (constraintId '{constraint.ConstraintId}').",
            Path: $"{path}.scope",
            Id: constraint.ConstraintId));
    }

    private static readonly IReadOnlySet<ConstraintKind> CrossFieldConstraintKinds =
        new HashSet<ConstraintKind> { ConstraintKind.CompareVariables, ConstraintKind.AllOrNone };

    internal static void WarnDuplicationProneTarget(
        ConstraintDefinition constraint,
        string path,
        List<PreflightIssue> warnings)
    {
        ArgumentNullException.ThrowIfNull(constraint);

        if (CrossFieldConstraintKinds.Contains(constraint.Kind)
            && string.Equals(constraint.Target.Kind, TargetKinds.Form, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(new PreflightIssue(
                Code: PreflightCodes.WarnCrossFieldFormTarget,
                Message: $"Cross-field constraint kind '{constraint.Kind}' targets the whole form (constraintId '{constraint.ConstraintId}'); anchor it on a field or variable so error focus targets a concrete field.",
                Path: $"{path}.target.kind",
                Id: constraint.ConstraintId));
        }
    }

    internal static string? ValidateJsonKind(JsonElement constraint, string path, List<PreflightIssue> errors)
    {
        var kind = PreflightJsonValueReader.TryGetString(constraint, "kind");
        if (kind is null || !SupportedConstraintKinds.Contains(kind))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorFeatureNotSupported,
                Message: $"Unsupported constraint kind '{kind ?? "<missing>"}'.",
                Path: $"{path}.kind"));
        }

        return kind;
    }

    internal static void ValidateJsonScope(JsonElement constraint, string path, List<PreflightIssue> errors)
    {
        if (!constraint.TryGetProperty("scope", out var scopeElement)
            || scopeElement.ValueKind != JsonValueKind.String)
        {
            return;
        }

        var scope = scopeElement.GetString();
        if (!string.IsNullOrWhiteSpace(scope) && SupportedConstraintScopes.Contains(scope.Trim()))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorFeatureNotSupported,
            Message: $"Unsupported constraint scope '{scope ?? "<missing>"}'.",
            Path: $"{path}.scope"));
    }
}
