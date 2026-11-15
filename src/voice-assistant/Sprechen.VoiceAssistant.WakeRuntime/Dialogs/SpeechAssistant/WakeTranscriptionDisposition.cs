namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet vier Ergebnisse eines Transkriptionszyklus für die weitere Verarbeitung.
/// </summary>
/// <remarks>
/// Ein fehlendes Transkript unterscheidet allein nicht zwischen regulärem
/// Ausbleiben von Wörtern und gescheiterter Erkennung. Die benannten Zustände
/// ermöglichen eine eigene Warte- und Zählerregel für Erkennungsfehler, ohne
/// reguläre Leerlaufzyklen ebenso zu verzögern. Wie
/// <see cref="WakeCaptureCycleOutcome"/> hält dieser Typ Fehler und
/// regulären Ablauf auseinander. Mehrere unabhängige Boolesche Flags
/// würden zusätzliche, vom Aufrufer zu deutende Kombinationen erzeugen.
/// </remarks>
internal enum WakeTranscriptionDisposition
{
    /// <summary>
    /// Ein Transkript liegt zur Auswertung vor. Nur dieser Zustand trägt eines.
    /// </summary>
    Recognized,

    /// <summary>
    /// Keine Wörter oder Ergebnis ohne Sprache. Der Ablauf setzt ohne
    /// Fehlerwartezeit fort.
    /// </summary>
    NoSpeech,

    /// <summary>
    /// Die Erkennung wurde ohne Abbruchanforderung des Aufrufers abgebrochen.
    /// Wiederholungen werden separat gezählt und verzögert. Die genaue Ursache
    /// bleibt offen. Eine Mikrofonfehlermeldung wird dadurch nicht ausgelöst.
    /// </summary>
    Faulted,

    /// <summary>
    /// Beendet den Zuhörlauf bei voller Warteschlange oder ausgeschöpftem
    /// Wiederholungsbudget. Das Ergebnis wurde bereits gemeldet. Der weitere
    /// Assistentenablauf liegt außerhalb dieser Phase.
    /// </summary>
    Rejected,
}
