using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet das bereinigte Antworttranskript mit seiner Aufnahme, damit eine nicht angenommene Antwort
/// gegebenenfalls unter dem Modell der Sitzungssprache erneut auf Abbruch geprüft werden kann. Ohne
/// verwendbares Transkript oder Aufnahme ist diese zweite Prüfung nicht freigegeben.
/// </summary>
internal readonly record struct SessionEntryAnswer(string? Transcript, CapturedSpeechAudio? Utterance)
{
    /// <summary>
    /// Kennzeichnet ein Ergebnis ohne verwendbares Transkript und ohne mitgeführte Aufnahme.
    /// </summary>
    internal static SessionEntryAnswer Silence => new(null, null);

    /// <summary>
    /// Prüft, ob sowohl ein nicht leeres Transkript als auch die zugehörige Aufnahme vorliegen.
    /// </summary>
    internal bool HasSpeech => !string.IsNullOrWhiteSpace(Transcript) && Utterance is not null;
}
