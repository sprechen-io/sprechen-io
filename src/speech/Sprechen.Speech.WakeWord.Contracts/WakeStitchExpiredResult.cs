namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>Kennzeichnet eine nach Ablauf des Zeitfensters verworfene Teiläußerung, damit sie keine spätere Aktivierung mehr ergänzt.</summary>
public sealed class WakeStitchExpiredResult : WakeStitchResult
{
    public WakeStitchExpiredResult(string previousTranscriptText)
    {
        PreviousTranscriptText = previousTranscriptText ?? throw new ArgumentNullException(nameof(previousTranscriptText));
    }

    public string PreviousTranscriptText { get; }

    /// <inheritdoc />
    public override TResult Accept<TResult>(IWakeStitchResultVisitor<TResult> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        return visitor.VisitExpired(this);
    }
}
