namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ausgang einer Übernahme. Nur <see cref="Applied"/> bedeutet, dass der Formularzustand
/// den Wert trägt. Jeder andere Wert lässt den Zustand unverändert.
/// </summary>
public enum FormAdditionalFieldValueAdoptionStatus
{
    /// <summary>Der Wert steht im Formular und der Zustandsstand ist genau einmal gestiegen.</summary>
    Applied,

    /// <summary>Das Ziel nahm den Wert nicht mehr an, etwa weil sich der Zustand seit dem Angebot geändert hat.</summary>
    Unavailable,

    /// <summary>Der genannte Wert ergibt keinen Wert des gebundenen Feldtyps.</summary>
    Invalid,

    /// <summary>Der Wert ist lesbar, verletzt aber eine Bedingung des Formulars.</summary>
    BlockedByValidation
}
