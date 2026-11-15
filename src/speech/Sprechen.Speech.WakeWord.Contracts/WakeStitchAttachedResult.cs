namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>Verbindet den aktuellen Zyklus mit einer vorherigen Teiläußerung, damit die vollständige Aktivierung samt Befehl weitergegeben werden kann.</summary>
public sealed class WakeStitchAttachedResult : WakeStitchResult
{
    public WakeStitchAttachedResult(string stitchedTranscriptText, string commandText, WakeWordActivationEvaluation evaluation)
    {
        StitchedTranscriptText = stitchedTranscriptText ?? throw new ArgumentNullException(nameof(stitchedTranscriptText));
        CommandText = commandText ?? throw new ArgumentNullException(nameof(commandText));
        Evaluation = evaluation ?? throw new ArgumentNullException(nameof(evaluation));
    }

    public string StitchedTranscriptText { get; }
    public string CommandText { get; }
    public WakeWordActivationEvaluation Evaluation { get; }

    /// <inheritdoc />
    public override TResult Accept<TResult>(IWakeStitchResultVisitor<TResult> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        return visitor.VisitAttached(this);
    }
}
