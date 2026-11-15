namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Trägt das Ergebnis einer lexikalischen oder akustischen Aktivierungsworterkennung
/// zum Reducer und hält Herkunft und zugehörige Konfidenzwerte unterscheidbar.
/// </summary>
public sealed record SpeechAssistantWakeWordDetectedEvent : SpeechAssistantEvent
{
    public SpeechAssistantWakeWordDetectedEvent(string transcript)
        : this(transcript, commandText: null, correlationId: null, cursor: default)
    {
    }

    public SpeechAssistantWakeWordDetectedEvent(string transcript, string? commandText)
        : this(transcript, commandText, correlationId: null, cursor: default)
    {
    }

    public SpeechAssistantWakeWordDetectedEvent(string transcript, string? commandText, string? correlationId)
        : this(transcript, commandText, correlationId, cursor: default)
    {
    }

    public SpeechAssistantWakeWordDetectedEvent(
        string transcript,
        string? commandText,
        string? correlationId,
        SampleCursor cursor)
        : this(
            transcript,
            commandText,
            correlationId,
            cursor,
            SpeechAssistantWakeWordEvidenceSource.LexicalTranscript,
            detectorId: null,
            confidenceScore: null,
            confidencePercent: null)
    {
    }

    private SpeechAssistantWakeWordDetectedEvent(
        string transcript,
        string? commandText,
        string? correlationId,
        SampleCursor cursor,
        SpeechAssistantWakeWordEvidenceSource source,
        string? detectorId,
        double? confidenceScore,
        int? confidencePercent)
    {
        Transcript = NormalizeTranscript(transcript, source, nameof(transcript));
        CommandText = NormalizeOptionalText(commandText);
        CorrelationId = NormalizeOptionalText(correlationId);
        Cursor = cursor;
        Source = source;
        DetectorId = NormalizeOptionalText(detectorId);
        ConfidenceScore = confidenceScore;
        ConfidencePercent = confidencePercent;
    }

    public static SpeechAssistantWakeWordDetectedEvent FromLexicalEvidence(
        string transcript,
        string? commandText,
        string? correlationId,
        int? confidencePercent)
        => new(
            transcript,
            commandText,
            correlationId,
            cursor: default,
            SpeechAssistantWakeWordEvidenceSource.LexicalTranscript,
            detectorId: null,
            confidenceScore: null,
            confidencePercent);

    public static SpeechAssistantWakeWordDetectedEvent FromAcousticEvidence(
        AcousticWakeWordEvidence evidence,
        string? correlationId)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        return new SpeechAssistantWakeWordDetectedEvent(
            transcript: string.Empty,
            commandText: null,
            correlationId,
            evidence.HandoffCursor,
            SpeechAssistantWakeWordEvidenceSource.AcousticDetector,
            evidence.DetectorId,
            evidence.ConfidenceScore,
            confidencePercent: null);
    }

    public string Transcript { get; }

    public string CommandText { get; }

    public string CorrelationId { get; }

    public SpeechAssistantWakeWordEvidenceSource Source { get; }

    public string DetectorId { get; }

    public double? ConfidenceScore { get; }

    /// <summary>
    /// Trägt den optionalen Prozentwert der lexikalischen Erkennung für einen vorliegenden
    /// Katalogbefehl. Der akustische Detektorwert bleibt getrennt in ConfidenceScore.
    /// </summary>
    public int? ConfidencePercent { get; }

    /// <summary>
    /// Bewahrt bei akustischer Aktivierung den übergebenen Ringpufferzeiger für Diagnose und
    /// Beobachtung. Der Aktivierungsübergang fordert den Katalogdialog ohne diesen Zeiger an. Die
    /// Aufnahme verwaltet ihre eigene Übergabe nach Wiedergaben. Die lexikalische Fabrik verwendet hier
    /// den Standardwert.
    /// </summary>
    public SampleCursor Cursor { get; }

    public string NormalizedTranscript => Transcript;

    public bool HasCommandText => !string.IsNullOrWhiteSpace(CommandText);

    private static string NormalizeTranscript(
        string value,
        SpeechAssistantWakeWordEvidenceSource source,
        string parameterName)
    {
        if (source == SpeechAssistantWakeWordEvidenceSource.AcousticDetector)
        {
            return NormalizeOptionalText(value);
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Transcript is required.", parameterName);
        }

        return value.Trim();
    }

    private static string NormalizeOptionalText(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
