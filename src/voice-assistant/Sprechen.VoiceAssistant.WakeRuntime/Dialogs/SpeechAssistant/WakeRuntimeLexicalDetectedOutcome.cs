namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Übergibt Transkript, möglichen Folgebefehl und verfügbare Konfidenz
/// an den Assistentenablauf, der daraus das Aktivierungsereignis bildet.
/// </summary>
public sealed record WakeRuntimeLexicalDetectedOutcome : WakeRuntimeDetectedOutcome
{
    public WakeRuntimeLexicalDetectedOutcome(
        string transcript,
        string? commandText,
        string correlationId)
        : this(transcript, commandText, correlationId, confidencePercent: null)
    {
    }

    public WakeRuntimeLexicalDetectedOutcome(
        string transcript,
        string? commandText,
        string correlationId,
        int? confidencePercent)
        : base(correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        Transcript = transcript.Trim();
        CommandText = NormalizeOptionalText(commandText);
        ConfidencePercent = confidencePercent;
    }

    public string Transcript { get; }

    public string CommandText { get; }

    public int? ConfidencePercent { get; }

    private static string NormalizeOptionalText(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
