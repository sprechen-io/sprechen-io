namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Gibt Ansagetexte als Sprache aus. Eine Überladung übernimmt zusätzlich
/// die vom Aufrufer bestimmte <see cref="SpokenAnnouncementKind"/> für die Aufzeichnung.
/// </summary>
public interface ITextToSpeechPlayer
{
    Task SpeakAsync(string text, string languageCode, double speechRate, CancellationToken cancellationToken);

    Task SpeakAsync(string text, string languageCode, CancellationToken cancellationToken);

    /// <summary>
    /// Spricht eine Ansage und zeichnet ihre vom Aufrufer angegebene Art auf.
    /// Die kürzeren Überladungen klassifizieren die Ansage nicht. Der fachliche
    /// Aufrufer muss die Kategorie dort mitgeben, wo sie bekannt ist.
    /// </summary>
    Task SpeakAsync(
        string text,
        string languageCode,
        double speechRate,
        SpokenAnnouncementKind announcementKind,
        CancellationToken cancellationToken);
}
