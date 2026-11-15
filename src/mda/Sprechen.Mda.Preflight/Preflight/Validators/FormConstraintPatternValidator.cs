using System.Text.RegularExpressions;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Musterbedingungen auf einen nicht leeren und syntaktisch gültigen
/// regulären Ausdruck.
/// </summary>
internal static class FormConstraintPatternValidator
{
    internal static void Validate(
        string? constraintId,
        string? kind,
        string? pattern,
        string path,
        List<PreflightIssue> errors)
    {
        if (!string.Equals(kind, ConstraintKinds.Pattern, StringComparison.Ordinal))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(pattern))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: $"Pattern constraint '{constraintId ?? "<missing>"}' must define a non-empty pattern.",
                Path: $"{path}.pattern",
                Id: constraintId));
            return;
        }

        try
        {
            _ = new Regex(pattern, RegexOptions.CultureInvariant);
        }
        catch (ArgumentException)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: $"Pattern constraint '{constraintId ?? "<missing>"}' contains an invalid regular expression.",
                Path: $"{path}.pattern",
                Id: constraintId));
        }
    }
}
