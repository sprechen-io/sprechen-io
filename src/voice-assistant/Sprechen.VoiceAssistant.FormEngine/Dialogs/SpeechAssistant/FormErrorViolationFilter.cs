using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Filtert Verstöße nach Fehlerschwere, damit blockierende Fehler getrennt von anderen Validierungshinweisen ausgewertet werden können.
/// </summary>
internal sealed class FormErrorViolationFilter
{
    public ImmutableArray<FormConstraintViolation> Filter(ImmutableArray<FormConstraintViolation> violations)
        => violations
            .Where(IsErrorViolation)
            .ToImmutableArray();

    public bool IsErrorViolation(FormConstraintViolation violation)
        => string.Equals(violation.Severity, SeverityLevels.Error, StringComparison.OrdinalIgnoreCase);
}
