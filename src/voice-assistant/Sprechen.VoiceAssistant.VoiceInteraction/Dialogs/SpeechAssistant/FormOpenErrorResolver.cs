using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Wählt je Feld den führenden Validierungsverstoß nach Schweregrad und
/// Bedingungskennung und fasst anschließend gleiche Verstoßidentitäten zusammen.
/// Die Projektion selbst filtert nicht ausschließlich auf den Schweregrad Fehler.
/// </summary>
internal static class FormOpenErrorResolver
{
    private static readonly FormOpenRulePrecedence OpenRulePrecedence = new();

    public static ImmutableArray<FormConstraintViolation> ResolveOpenErrors(
        ImmutableArray<FormConstraintViolation> violations)
    {
        // Schweregrad und Bedingungskennung bestimmen den führenden Verstoß je Feld.
        // Bei gleichem Rang gilt die ursprüngliche Reihenfolge.
        var openRuleByFieldId = new Dictionary<string, FormConstraintViolation>(StringComparer.Ordinal);
        foreach (var brokenRulesOfOneField in violations
            .Where(violation => !string.IsNullOrWhiteSpace(violation.FieldId))
            .GroupBy(violation => violation.FieldId, StringComparer.Ordinal))
        {
            if (OpenRulePrecedence.SelectOpen(brokenRulesOfOneField) is { } openRule)
            {
                openRuleByFieldId[brokenRulesOfOneField.Key] = openRule;
            }
        }

        var openErrors = ImmutableArray.CreateBuilder<FormConstraintViolation>();
        var claimedFieldIds = new HashSet<string>(StringComparer.Ordinal);
        var claimedKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var violation in violations)
        {
            if (!string.IsNullOrWhiteSpace(violation.FieldId))
            {
                if (!string.Equals(
                        openRuleByFieldId[violation.FieldId].ConstraintId,
                        violation.ConstraintId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (!claimedFieldIds.Add(violation.FieldId))
                {
                    continue;
                }
            }

            if (!claimedKeys.Add(BuildOpenErrorKey(violation)))
            {
                continue;
            }

            openErrors.Add(violation);
        }

        return openErrors.ToImmutable();
    }

    /// <summary>
    /// Bildet die Identität eines offenen Verstoßes aus Geltungsbereich, Code,
    /// Schweregrad und dem dazugehörigen Feld- oder Gruppenbezug.
    /// </summary>
    internal static string BuildOpenErrorKey(FormConstraintViolation violation)
    {
        // Feldbezogene Verstöße über das Feld unterscheiden. Bei anderen Bereichen
        // den Gruppenbezug verwenden, um gleiche Regelmeldungen zusammenzufassen.
        var ownedByOneField = string.Equals(violation.Scope, ConstraintScopes.Field, StringComparison.Ordinal);

        return string.Join(
            "|",
            violation.Scope,
            violation.Code,
            violation.Severity,
            ownedByOneField ? violation.FieldId : string.Empty,
            ownedByOneField ? string.Empty : violation.FieldGroupId ?? string.Empty);
    }
}
