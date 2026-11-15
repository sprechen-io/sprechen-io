using Sprechen.Speech.ModelRuntime.Services;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Liest die modellierten Antwortzeitfenster für Erstkontakt, wiederholten Einstieg und weitere Sprachfragen. Zum gewählten Fenster kommt der modellierte Zuschlag für das Abwarten der Wiedergabe hinzu.
/// </summary>
internal static class SessionEntryAnswerWindows
{
    public static TimeSpan FirstContact { get; } =
        Read(profile => profile.FirstContactInitialSilenceTimeoutMs);

    public static TimeSpan Repeat { get; } =
        Read(profile => profile.RepeatInitialSilenceTimeoutMs);

    public static TimeSpan LanguageQuestion { get; } =
        Read(profile => profile.LanguageQuestionInitialSilenceTimeoutMs);

    /// <summary>
    /// Wählt für die erste Sprachfrage das Erstkontakt- oder Wiederholungsfenster.
    /// </summary>
    public static TimeSpan ForOffer(bool firstContact) => firstContact ? FirstContact : Repeat;

    private static TimeSpan Read(
        Func<Sprechen.Mda.Contracts.V1.SpeechCapture.SpeechCaptureSessionEntryProfile, int> select)
    {
        // Das Pflichtprofil kommt aus dem Katalogleser. Hier steht kein eigener Ersatzwert, der von
        // der Modellvorgabe abweichen könnte.
        var profile = SpeechCaptureProfileCatalogReader.GetSessionEntryProfile();

        // Den modellierten Zuschlag ergänzen, weil die Aufnahme das Abwarten der Wiedergabe aus
        // demselben Zeitbudget bestreitet. Die Summe ist das angeforderte Budget.
        return TimeSpan.FromMilliseconds(select(profile) + profile.PlaybackSettleAllowanceMs);
    }
}
