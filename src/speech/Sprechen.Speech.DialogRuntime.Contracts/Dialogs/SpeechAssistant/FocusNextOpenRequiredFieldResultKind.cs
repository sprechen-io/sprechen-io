namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet Bewegung, Rücksprung zum Anfang, bereits fokussiertes Einzelziel
/// und fehlendes Ziel bei der Navigation zu offenen Pflichtfeldern.
/// </summary>
public enum FocusNextOpenRequiredFieldResultKind
{
    /// <summary>Der Fokus wechselte vorwärts zum nächsten offenen Pflichtfeld in Dokumentreihenfolge.</summary>
    Moved,

    /// <summary>Kein offenes Pflichtfeld folgte dem aktuellen Fokus, deshalb sprang er zum ersten offenen Pflichtfeld.</summary>
    WrappedToStart,

    /// <summary>Der aktuelle Fokus ist das einzige offene Pflichtfeld. Er blieb stehen.</summary>
    AlreadyOnSoleTarget,

    /// <summary>Es gibt kein offenes Pflichtfeld. Der Fokus blieb stehen.</summary>
    NoOpenRequiredField,
}
