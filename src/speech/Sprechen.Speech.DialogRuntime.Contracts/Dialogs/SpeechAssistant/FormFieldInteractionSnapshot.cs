namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Interaktionsvorgaben eines Feldes mit Eingabemodus, Bearbeitbarkeit, Werttyp
/// und Regel zum Vorlesen in der Bestätigung. Optionale Grenzen steuern die Mehrfachauswahl.
/// </summary>
public sealed record FormFieldInteractionSnapshot(
    bool CodeStyle,
    InputMode InputMode,
    string Editability,
    string ValueTypeKind,
    bool SpeakValueInConfirmation)
{
    /// <summary>Mindestzahl ausgewählter Optionen, sofern vorgegeben.</summary>
    public int? MinSelected { get; init; }

    /// <summary>Höchstzahl ausgewählter Optionen. Null bedeutet keine vorgegebene Grenze.</summary>
    public int? MaxSelected { get; init; }
}
