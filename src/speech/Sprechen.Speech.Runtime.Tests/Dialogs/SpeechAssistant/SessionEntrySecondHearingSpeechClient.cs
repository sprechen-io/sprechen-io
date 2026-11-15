using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Leitet die Erkennung derselben Aufnahme unter einer anderen Sprache in eine
/// separate Queue. Wiederholungen in derselben Sprache bleiben beim inneren Double,
/// damit die Zweiterkennung kein Transkript des nächsten Sprachangebots verbraucht.
/// Eine leere Zweiterkennungsqueue wirft NoSpeechDetectedException und liefert kein Abbruchwort.
/// </summary>
internal sealed class SessionEntrySecondHearingSpeechClient : ISpeechRecognitionClient
{
    private readonly QueuedSpeechInputClient _inner;
    private readonly Dictionary<CapturedSpeechAudio, string?> _firstHearingLanguages = new(ReferenceEqualityComparer.Instance);
    private readonly Queue<Func<SpeechTranscript>> _secondHearings = new();

    public SessionEntrySecondHearingSpeechClient(QueuedSpeechInputClient inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <summary>
    /// Sprachen der versuchten Zweiterkennungen in Aufrufreihenfolge, auch bei einem Fehler.
    /// </summary>
    public List<string?> SecondHearingLanguages { get; } = [];

    /// <summary>
    /// Erkennungshinweise der versuchten Zweiterkennungen in Aufrufreihenfolge.
    /// </summary>
    public List<SpeechKeyterms?> SecondHearingKeyterms { get; } = [];

    public void EnqueueSecondHearing(string text)
        => _secondHearings.Enqueue(() => new SpeechTranscript(text, 90));

    public void EnqueueSecondHearingException(Exception exception)
        => _secondHearings.Enqueue(() => throw exception);

    public Task<SpeechTranscript> SpeechToTextAsync(
        CapturedSpeechAudio utterance,
        string? languageCode,
        RecognitionProfile recognitionProfile,
        CancellationToken cancellationToken,
        SpeechKeyterms? keyterms = null)
    {
        ArgumentNullException.ThrowIfNull(utterance);

        // Gleiche Aufnahme unter anderer Sprache bedeutet Zweiterkennung. Wiederholung in
        // derselben Sprache bleibt dagegen beim inneren Double, damit dessen Fehlerpfad weiter geprüft wird.
        if (!_firstHearingLanguages.TryGetValue(utterance, out var firstLanguage))
        {
            _firstHearingLanguages[utterance] = languageCode;
            return _inner.SpeechToTextAsync(utterance, languageCode, recognitionProfile, cancellationToken, keyterms);
        }

        if (string.Equals(firstLanguage, languageCode, StringComparison.OrdinalIgnoreCase))
        {
            return _inner.SpeechToTextAsync(utterance, languageCode, recognitionProfile, cancellationToken, keyterms);
        }

        SecondHearingLanguages.Add(languageCode);
        SecondHearingKeyterms.Add(keyterms);

        if (_secondHearings.Count == 0)
        {
            throw new NoSpeechDetectedException();
        }

        return Task.FromResult(_secondHearings.Dequeue().Invoke());
    }
}
