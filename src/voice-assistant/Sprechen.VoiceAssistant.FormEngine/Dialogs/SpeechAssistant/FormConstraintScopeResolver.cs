using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Verwendet den ausdrücklich gesetzten bekannten Geltungsbereich einer Bedingung oder leitet ihn aus ihrem Formularziel beziehungsweise Feldbezug ab.
/// </summary>
internal static class FormConstraintScopeResolver
{
    public static string Resolve(ConstraintDefinition constraint)
    {
        ArgumentNullException.ThrowIfNull(constraint);

        if (constraint.Scope is { } scope and not ConstraintScope.Unknown)
        {
            return scope.ToWireToken();
        }

        if (string.Equals(constraint.Target.Kind, TargetKinds.Form, StringComparison.OrdinalIgnoreCase))
        {
            return ConstraintScopes.Form;
        }

        return ConstraintScopes.Field;
    }
}


