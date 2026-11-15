
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class NoOpTextToSpeechPlayer : ITextToSpeechPlayer
{
    public Task SpeakAsync(string text, string languageCode, CancellationToken cancellationToken)
        => SpeakAsync(text, languageCode, speechRate: 1.0d, SpokenAnnouncementKind.Prompt, cancellationToken);

    public Task SpeakAsync(string text, string languageCode, double speechRate, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task SpeakAsync(
        string text,
        string languageCode,
        double speechRate,
        SpokenAnnouncementKind announcementKind,
        CancellationToken cancellationToken)
        => Task.CompletedTask;
}


