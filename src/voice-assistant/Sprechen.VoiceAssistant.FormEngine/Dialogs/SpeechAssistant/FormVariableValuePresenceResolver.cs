namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Bestimmt anhand eines aufgelösten Formularwerts, ob eine Angabe vorhanden ist. Leere Texte und Mengen bleiben dabei unbelegt.
/// </summary>
internal sealed class FormVariableValuePresenceResolver
{
    private readonly Func<string, FormValue?> getVariableValue;

    public FormVariableValuePresenceResolver(Func<string, FormValue?> getVariableValue)
    {
        this.getVariableValue = getVariableValue ?? throw new ArgumentNullException(nameof(getVariableValue));
    }

    public bool IsVariableValueSet(string variableId)
    {
        var value = getVariableValue(variableId);
        if (value is null)
        {
            return false;
        }

        return value.Value.Kind switch
        {
            FormValueKind.String => !string.IsNullOrWhiteSpace(value.Value.String),
            FormValueKind.Set => value.Value.Set.Length > 0,
            _ => true
        };
    }
}
