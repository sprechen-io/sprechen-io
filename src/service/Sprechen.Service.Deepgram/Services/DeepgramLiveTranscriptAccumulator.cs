namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Kombiniert abschließende und vorläufige Transkriptteile und unterdrückt Ausgaben
/// mit unverändertem Gesamttext und unveränderter Erkennungssicherheit.
/// </summary>
internal sealed class DeepgramLiveTranscriptAccumulator
{
    private string _committed = string.Empty;
    private int? _committedConfidence;
    private string? _interim;
    private int? _interimConfidence;
    private string? _lastEmitted;
    private int? _lastEmittedConfidence;

    public SpeechTranscript? Apply(
        string transcript,
        int? confidencePercent,
        bool isFinal)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return null;
        }

        if (isFinal)
        {
            _committed = AppendCommitted(_committed, transcript);
            _committedConfidence = confidencePercent;
            _interim = null;
            _interimConfidence = null;
        }
        else
        {
            _interim = transcript.Trim();
            _interimConfidence = confidencePercent;
        }

        return TryCreateNextEmission();
    }

    private SpeechTranscript? TryCreateNextEmission()
    {
        var current = ComposeTranscript(_committed, _interim);
        var currentConfidence = _interimConfidence ?? _committedConfidence;
        if (string.IsNullOrWhiteSpace(current)
            || (string.Equals(current, _lastEmitted, StringComparison.Ordinal)
                && Nullable.Equals(currentConfidence, _lastEmittedConfidence)))
        {
            return null;
        }

        _lastEmitted = current;
        _lastEmittedConfidence = currentConfidence;
        return new SpeechTranscript(current, currentConfidence);
    }

    private static string AppendCommitted(string committed, string segment)
    {
        if (string.IsNullOrWhiteSpace(segment))
        {
            return committed;
        }

        segment = segment.Trim();
        if (string.IsNullOrWhiteSpace(committed))
        {
            return segment;
        }

        return $"{committed} {segment}";
    }

    private static string ComposeTranscript(string committed, string? interim)
    {
        var interimText = string.IsNullOrWhiteSpace(interim) ? null : interim.Trim();

        if (string.IsNullOrWhiteSpace(committed))
        {
            return interimText ?? string.Empty;
        }

        return interimText is null ? committed : $"{committed} {interimText}";
    }
}
