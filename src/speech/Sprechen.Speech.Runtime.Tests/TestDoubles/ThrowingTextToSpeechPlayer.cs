using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

/// <summary>
/// Zählt Sprachausgabeversuche und lässt jeden scheitern, um den Rückfall
/// auf den Fehlversuchston bei gescheiterter gesprochener Hilfe zu prüfen.
/// </summary>
internal sealed class ThrowingTextToSpeechPlayer : ITextToSpeechPlayer
{
    private int _attemptCount;

    public int AttemptCount => Volatile.Read(ref _attemptCount);

    public Task SpeakAsync(string text, string languageCode, CancellationToken cancellationToken)
        => SpeakAsync(text, languageCode, speechRate: 1.0d, SpokenAnnouncementKind.Prompt, cancellationToken);

    public Task SpeakAsync(string text, string languageCode, double speechRate, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _attemptCount);
        throw new InvalidOperationException("Simulated TTS playback failure.");
    }

    public Task SpeakAsync(
        string text,
        string languageCode,
        double speechRate,
        SpokenAnnouncementKind announcementKind,
        CancellationToken cancellationToken)
        => SpeakAsync(text, languageCode, speechRate, cancellationToken);
}
