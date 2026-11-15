using System.Collections.Concurrent;

namespace Sprechen.Speech.TestDoubles;

/// <summary>
/// Zeichnet Sprachausgabeaufrufe mit Sprache, Rate und Zeitpunkt auf. Tests können
/// auf eine Aufrufzahl warten und optional Meldungen als <c>tts:{languageCode}:{text}</c> empfangen.
/// </summary>
public sealed class RecordingTextToSpeechPlayer : ITextToSpeechPlayer
{
    private readonly ConcurrentQueue<(string Text, string LanguageCode, double SpeechRate)> _calls = new();
    private readonly ConcurrentQueue<DateTimeOffset> _callTimestamps = new();
    private readonly ConcurrentQueue<SpokenAnnouncementKind> _announcementKinds = new();
    private readonly TaskCompletionSource _firstCall = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Action<string>? _recordFeedback;

    public RecordingTextToSpeechPlayer(Action<string>? recordFeedback = null)
    {
        _recordFeedback = recordFeedback;
    }

    public IReadOnlyCollection<(string Text, string LanguageCode, double SpeechRate)> Calls => _calls.ToArray();

    public IReadOnlyList<DateTimeOffset> CallTimestamps => _callTimestamps.ToArray();

    /// <summary>
    /// Die deklarierten Kategorien in der Reihenfolge ihrer Aufzeichnung.
    /// </summary>
    public IReadOnlyList<SpokenAnnouncementKind> AnnouncementKinds => _announcementKinds.ToArray();

    public Task SpeakAsync(string text, string languageCode, CancellationToken cancellationToken)
        => SpeakAsync(text, languageCode, speechRate: 1.0d, cancellationToken);

    public Task SpeakAsync(string text, string languageCode, double speechRate, CancellationToken cancellationToken)
        => SpeakAsync(text, languageCode, speechRate, SpokenAnnouncementKind.Prompt, cancellationToken);

    public Task SpeakAsync(
        string text,
        string languageCode,
        double speechRate,
        SpokenAnnouncementKind announcementKind,
        CancellationToken cancellationToken)
    {
        // Der Test soll die deklarierte Kategorie unabhängig vom Ausgabetext prüfen können.
        _announcementKinds.Enqueue(announcementKind);
        _calls.Enqueue((text, languageCode, speechRate));
        _callTimestamps.Enqueue(DateTimeOffset.UtcNow);
        _firstCall.TrySetResult();
        _recordFeedback?.Invoke($"tts:{languageCode}:{text}");
        return Task.CompletedTask;
    }

    public Task WaitForFirstCallAsync(TimeSpan timeout)
    {
        return _firstCall.Task.WaitAsync(timeout);
    }

    public async Task WaitForCallCountAsync(int expectedCount, TimeSpan timeout)
    {
        if (expectedCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedCount), expectedCount, "Expected count must be positive.");
        }

        using var cts = new CancellationTokenSource(timeout);

        while (!cts.IsCancellationRequested)
        {
            if (_calls.Count >= expectedCount)
            {
                return;
            }

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10), cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                break;
            }
        }

        throw new TimeoutException($"Expected at least {expectedCount} TTS call(s), but only observed {_calls.Count}.");
    }
}
