using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet Transkript, fehlende Wörter, Erkennungsfehler und Ablehnung
/// eines Transkriptionszyklus für deren unterschiedliche Weiterbehandlung.
/// </summary>
/// <remarks>
/// <para>Der private Konstruktor und die Fabrikmethoden koppeln ein Transkript
/// an <see cref="WakeTranscriptionDisposition.Recognized"/>. Die öffentliche
/// Eigenschaft bleibt direkt lesbar, und <see cref="TryGetRecognized"/> macht
/// ihre Nicht-Null-Zusage zusätzlich für die Nullable-Analyse nutzbar.</para>
/// <para>Ein Referenztyp vermeidet den impliziten Standardzustand eines Werttyps,
/// der die erste Enum-Ausprägung mit einem fehlenden Transkript kombinieren
/// könnte. Der Standardwert dieser Klasse ist stattdessen <c>null</c> und
/// wird an entsprechend geprüften Aufrufstellen von der Nullable-Analyse erfasst.</para>
/// </remarks>
internal sealed record WakeTranscriptionCycleOutcome
{
    private WakeTranscriptionCycleOutcome(SpeechTranscript? transcript, WakeTranscriptionDisposition disposition)
    {
        Transcript = transcript;
        Disposition = disposition;
    }

    /// <summary>
    /// Ergebnisart für die Auswahl des weiteren Ablaufs.
    /// </summary>
    public WakeTranscriptionDisposition Disposition { get; }

    /// <summary>
    /// Transkript, das die Fabrikmethoden ausschließlich für
    /// <see cref="WakeTranscriptionDisposition.Recognized"/> belegen.
    /// <see cref="TryGetRecognized"/> verknüpft Zugriff und Nicht-Null-Zusage
    /// für die Nullable-Analyse.
    /// </summary>
    public SpeechTranscript? Transcript { get; }

    /// <summary>
    /// Liefert nur für <see cref="WakeTranscriptionDisposition.Recognized"/>
    /// <c>true</c> mit einem nicht nullwertigen Transkript.
    /// </summary>
    public bool TryGetRecognized(out SpeechTranscript transcript)
    {
        if (Disposition == WakeTranscriptionDisposition.Recognized && Transcript is { } recognized)
        {
            transcript = recognized;
            return true;
        }

        transcript = null!;
        return false;
    }

    /// <summary>
    /// Erstellt ein Ergebnis mit Transkript zur anschließenden lexikalischen Auswertung.
    /// </summary>
    public static WakeTranscriptionCycleOutcome Recognized(SpeechTranscript transcript)
    {
        ArgumentNullException.ThrowIfNull(transcript);
        return new WakeTranscriptionCycleOutcome(transcript, WakeTranscriptionDisposition.Recognized);
    }

    /// <summary>
    /// Ergebnis ohne erkannte Wörter. Der Ablauf kann ohne Fehlerwartezeit fortfahren.
    /// </summary>
    public static WakeTranscriptionCycleOutcome NoSpeech { get; }
        = new(transcript: null, WakeTranscriptionDisposition.NoSpeech);

    /// <summary>
    /// Erkennungsfehler, dessen Wiederholung mit einem eigenen Fehlerzähler verzögert wird.
    /// </summary>
    public static WakeTranscriptionCycleOutcome Faulted { get; }
        = new(transcript: null, WakeTranscriptionDisposition.Faulted);

    /// <summary>
    /// Erstellt eine Ablehnung für den aktuellen Zuhörlauf. Die Phase hat das zugehörige Ergebnis
    /// bereits veröffentlicht.
    /// </summary>
    public static WakeTranscriptionCycleOutcome Rejected { get; }
        = new(transcript: null, WakeTranscriptionDisposition.Rejected);
}
