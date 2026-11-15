namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Leitet Sprachausgaben an den Wiedergabeablauf weiter und löst den Mikrofonsperrzustand früh auf,
/// damit dessen Beobachter Wiedergabeänderungen schon vor der ersten Sprachausgabe abonnieren.
/// </summary>
internal sealed class TextToSpeechPlayer(
    TextToSpeechPlaybackRunner playbackRunner,
    IMicGateState micGateState) : ITextToSpeechPlayer
{
    private readonly TextToSpeechPlaybackRunner _playbackRunner = playbackRunner ?? throw new ArgumentNullException(nameof(playbackRunner));

    // Der Mikrofonsperrzustand wird zusammen mit dem TTS-Player aufgelöst, damit er
    // Änderungen des Wiedergabezustands vor der ersten Sprachausgabe abonniert.
    // Die Referenz wird danach bewusst nicht gelesen.
    private readonly IMicGateState _micGateState = micGateState
        ?? throw new ArgumentNullException(nameof(micGateState));

    public Task SpeakAsync(string text, string languageCode, CancellationToken cancellationToken)
        => SpeakAsync(text, languageCode, speechRate: 1.0d, cancellationToken);

    public Task SpeakAsync(string text, string languageCode, double speechRate, CancellationToken cancellationToken)
        => _playbackRunner.SpeakAsync(text, languageCode, speechRate, SpokenAnnouncementKind.Prompt, cancellationToken);

    public Task SpeakAsync(
        string text,
        string languageCode,
        double speechRate,
        SpokenAnnouncementKind announcementKind,
        CancellationToken cancellationToken)
        => _playbackRunner.SpeakAsync(text, languageCode, speechRate, announcementKind, cancellationToken);
}
