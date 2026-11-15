using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Entscheidet für einen vorgegebenen Bedingungsbereich, ob ein Verstoß die betrachtete Feldgruppe blockiert. Formularweite Bereiche gelten dabei für jede Gruppe.
/// </summary>
internal static class FormFieldGroupCompletionScopePolicy
{
    public static bool BlocksCandidateFieldGroup(
        string scope,
        string candidateFieldGroupId,
        string? scopedFieldGroupId)
    {
        if (string.Equals(scope, ConstraintScopes.Form, StringComparison.Ordinal))
        {
            return true;
        }

        return string.Equals(scope, ConstraintScopes.FieldGroup, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(scopedFieldGroupId)
            && string.Equals(scopedFieldGroupId, candidateFieldGroupId, StringComparison.Ordinal);
    }
}
