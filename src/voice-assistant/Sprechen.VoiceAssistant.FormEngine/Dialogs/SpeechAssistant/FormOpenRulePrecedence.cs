using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet Validierungsverstöße nach absteigendem Schweregrad und anschließend nach Bedingungskennung.
/// Bei gleichem Schweregrad und gleicher Kennung bleibt die Eingabereihenfolge erhalten.
/// </summary>
public sealed class FormOpenRulePrecedence
{
    private static readonly FormConstraintSeverityRanker SeverityRanker = new();

    public ImmutableArray<FormConstraintViolation> Order(IEnumerable<FormConstraintViolation> brokenRules)
    {
        ArgumentNullException.ThrowIfNull(brokenRules);

        return brokenRules
            .OrderByDescending(brokenRule => SeverityRanker.Rank(brokenRule.Severity))
            .ThenBy(brokenRule => brokenRule.ConstraintId, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    /// <summary>
    /// Liefert den ersten Verstoß der Rangfolge oder null bei leerer Folge.
    /// Die übrigen Bedingungen werden dadurch nicht von ihrer Prüfung ausgeschlossen.
    /// </summary>
    public FormConstraintViolation? SelectOpen(IEnumerable<FormConstraintViolation> brokenRules)
        => Order(brokenRules).FirstOrDefault();
}
