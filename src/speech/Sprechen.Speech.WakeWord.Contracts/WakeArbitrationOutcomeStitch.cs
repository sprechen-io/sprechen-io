namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Aktivierung durch das Zusammenfügen mehrerer Aufnahmezyklen. Das Ergebnis
/// trägt das verbundene Transkript, den abgetrennten Befehl und die lexikalische Auswertung.
/// </summary>
public sealed record WakeArbitrationOutcomeStitch : WakeArbitrationOutcome
{
    public WakeArbitrationOutcomeStitch(
        string stitchedTranscriptText,
        string commandText,
        WakeWordActivationEvaluation evaluation)
    {
        StitchedTranscriptText = stitchedTranscriptText
            ?? throw new ArgumentNullException(nameof(stitchedTranscriptText));
        CommandText = commandText ?? throw new ArgumentNullException(nameof(commandText));
        Evaluation = evaluation ?? throw new ArgumentNullException(nameof(evaluation));
    }

    public string StitchedTranscriptText { get; }

    public string CommandText { get; }

    public WakeWordActivationEvaluation Evaluation { get; }
}
