namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Meldet dem Startablauf die Sprache für die Fortsetzung oder den erkannten Abbruch.
/// Auch beim Abbruch bleibt die Sitzungssprache für die anschließende Bestätigung verfügbar.
/// </summary>
internal readonly record struct SessionEntryOutcome(string ContinueInIsoCode, bool Cancelled)
{
    /// <summary>
    /// Kennzeichnet die Fortsetzung in der aufgelösten oder angenommenen Sprache.
    /// </summary>
    internal static SessionEntryOutcome ContinueIn(string isoCode) => new(isoCode, Cancelled: false);

    /// <summary>
    /// Kennzeichnet den Abbruch am Einstieg mit der Sitzungssprache für die anschließende Bestätigung.
    /// </summary>
    internal static SessionEntryOutcome CancelledByTheParticipant(string sessionIsoCode)
        => new(sessionIsoCode, Cancelled: true);
}
