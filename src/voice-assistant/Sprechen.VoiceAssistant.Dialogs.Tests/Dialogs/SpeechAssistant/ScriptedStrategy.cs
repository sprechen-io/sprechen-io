namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Liefert vorgegebene Aufnahme- und Verarbeitungsergebnisse für isolierte Ablaufprüfungen.
/// Die Aufnahmen unterscheiden Transkript, Stille und nicht verfügbare Spracherkennung.
/// </summary>
internal sealed class ScriptedStrategy(
    IEnumerable<CharacterAccumulationTurnOutcome> outcomes,
    IEnumerable<CharacterAccumulationTurnCapture> captures,
    Func<int, bool> hasContent) : ICharacterAccumulationStrategy
{
    private readonly Queue<CharacterAccumulationTurnOutcome> _outcomes = new(outcomes);
    private readonly Queue<CharacterAccumulationTurnCapture> _captures = new(captures);
    private readonly Func<int, bool> _hasContent = hasContent;

    public List<string> ProcessedTranscripts { get; } = [];

    public int CaptureCount { get; private set; }

    public int RePromptCount { get; private set; }

    public string FinalizedValue { get; init; } = string.Empty;

    public int? AggregatedConfidencePercent { get; init; }

    public bool HasContent => _hasContent(CaptureCount);

    public string GetFinalizedValue() => FinalizedValue;

    public Task<CharacterAccumulationTurnOutcome> ProcessTurnAsync(
        CharacterAccumulationContext context,
        string transcript,
        CancellationToken cancellationToken)
    {
        ProcessedTranscripts.Add(transcript);
        return Task.FromResult(_outcomes.Dequeue());
    }

    public Task<CharacterAccumulationTurnCapture> CaptureNextTurnAsync(
        CharacterAccumulationContext context,
        CancellationToken cancellationToken)
    {
        CaptureCount++;
        return Task.FromResult(_captures.Dequeue());
    }

    public Task AnnounceContinueOrDoneAsync(
        CharacterAccumulationContext context,
        CancellationToken cancellationToken)
    {
        RePromptCount++;
        return Task.CompletedTask;
    }

    public int RecognizerUnavailableAnnouncementCount { get; private set; }

    /// <summary>
    /// Lässt auch die Fehleransage scheitern, um den Ablauf bei gleichzeitig
    /// nicht verfügbarer Erkennung und Sprachausgabe zu prüfen.
    /// </summary>
    public bool ThrowOnRecognizerUnavailableAnnouncement { get; init; }

    public Task AnnounceRecognizerUnavailableAsync(
        CharacterAccumulationContext context,
        CancellationToken cancellationToken)
    {
        RecognizerUnavailableAnnouncementCount++;

        return ThrowOnRecognizerUnavailableAnnouncement
            ? Task.FromException(new InvalidOperationException("tts down"))
            : Task.CompletedTask;
    }

}
