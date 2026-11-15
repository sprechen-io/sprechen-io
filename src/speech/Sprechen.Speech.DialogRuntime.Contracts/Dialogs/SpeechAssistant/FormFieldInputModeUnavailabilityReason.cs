namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Grund, warum ein Eingabemodus für ein Feld nicht verfügbar ist.
/// </summary>
public enum FormFieldInputModeUnavailabilityReason
{
    /// <summary>Das Feld ist nicht nutzereditierbar (deklariert oder zur Laufzeit nur lesbar).</summary>
    FieldNotEditable,

    /// <summary>Die Spracheingabe-Politik des Feldes verbietet Mikrofonerfassung.</summary>
    VoiceInputForbidden,

    /// <summary>Der wirksame Feldtyp unterstützt in diesem Modus keinen gültigen Wert.</summary>
    UnsupportedForFieldType,

    /// <summary>Die Datenschutzklasse oder Vorlesepolitik des Feldes sperrt den Modus.</summary>
    BlockedByPrivacyPolicy
}
