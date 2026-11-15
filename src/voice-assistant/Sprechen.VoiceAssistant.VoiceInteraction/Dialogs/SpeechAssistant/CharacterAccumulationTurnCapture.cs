namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält Aufnahmeergebnis, gegebenenfalls Transkript und gemeldeten Vertrauenswert
/// eines Schritts der Werteingabe zusammen.
/// </summary>
/// <remarks>
/// Die Erzeugungsmethoden unterscheiden nicht verfügbare Erkennung, als Stille
/// eingestufte Ergebnisse und nichtleere Transkripte. Über den Abschluss
/// des Puffers entscheidet der Ablauf.
/// </remarks>
internal readonly record struct CharacterAccumulationTurnCapture
{
    private CharacterAccumulationTurnCapture(string? transcript, CharacterAccumulationCaptureKind kind, int? sttConfidencePercent)
    {
        Transcript = transcript;
        Kind = kind;
        SttConfidencePercent = sttConfidencePercent;
    }

    /// <summary>
    /// Liefert die Aufnahmeart, nach der die weiteren Ergebnisfelder eingeordnet werden.
    /// </summary>
    public CharacterAccumulationCaptureKind Kind { get; }

    /// <summary>
    /// Liefert das Transkript bei <see cref="CharacterAccumulationCaptureKind.Transcribed"/>.
    /// Bei den anderen Ergebnissen bleibt es <c>null</c>.
    /// </summary>
    public string? Transcript { get; }

    /// <summary>
    /// Liefert die vom Aufrufer übergebene Erkennungssicherheit oder null ohne Angabe.
    /// </summary>
    public int? SttConfidencePercent { get; }

    /// <summary>
    /// Erzeugt ein Transkriptergebnis aus nichtleerem Text ohne Angabe der Erkennungssicherheit.
    /// </summary>
    public static CharacterAccumulationTurnCapture Transcribed(string transcript)
        => Transcribed(transcript, sttConfidencePercent: null);

    /// <summary>
    /// Erzeugt ein Transkriptergebnis aus nichtleerem Text mit der übergebenen Erkennungssicherheit.
    /// </summary>
    public static CharacterAccumulationTurnCapture Transcribed(string transcript, int? sttConfidencePercent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        return new CharacterAccumulationTurnCapture(transcript, CharacterAccumulationCaptureKind.Transcribed, sttConfidencePercent);
    }

    /// <summary>
    /// Erzeugt ein als Stille eingestuftes Ergebnis. Die Abschlussentscheidung bleibt beim Ablauf.
    /// </summary>
    public static CharacterAccumulationTurnCapture Silence { get; }
        = new(transcript: null, CharacterAccumulationCaptureKind.Silence, sttConfidencePercent: null);

    /// <summary>
    /// Erzeugt ein Ergebnis für nicht verfügbare Erkennung ohne Transkript oder Erkennungssicherheit.
    /// </summary>
    public static CharacterAccumulationTurnCapture RecognizerUnavailable { get; }
        = new(transcript: null, CharacterAccumulationCaptureKind.RecognizerUnavailable, sttConfidencePercent: null);
}
