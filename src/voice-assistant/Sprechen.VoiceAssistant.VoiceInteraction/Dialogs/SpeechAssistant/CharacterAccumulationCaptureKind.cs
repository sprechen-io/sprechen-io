namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet Aufnahmeergebnisse beim schrittweisen Sammeln von Werten
/// als Grundlage für die nächste Ablaufentscheidung.
/// </summary>
/// <remarks>
/// Die Aufnahmeart beschreibt das übergebene Ergebnis.
/// <see cref="CharacterAccumulationTurnOutcome"/> beschreibt die anschließende Ablaufentscheidung.
/// </remarks>
internal enum CharacterAccumulationCaptureKind
{
    /// <summary>
    /// Kennzeichnet die als nicht verfügbar eingestufte Erkennung. Der Nullwert der
    /// Aufzählung verhindert, dass Standardinitialisierung als Stille eingestuft wird.
    /// </summary>
    RecognizerUnavailable = 0,

    /// <summary>
    /// Kennzeichnet einen vom Aufrufer als Stille eingestuften Aufnahmeausgang.
    /// </summary>
    Silence = 1,

    /// <summary>
    /// Kennzeichnet einen Aufnahmeausgang mit übergebenem Transkript.
    /// </summary>
    Transcribed = 2,
}
