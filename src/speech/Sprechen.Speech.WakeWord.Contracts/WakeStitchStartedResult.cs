namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>Bewahrt das aktuelle Transkript als Teiläußerung, damit der nächste Zyklus die Aktivierungsform vervollständigen kann.</summary>
public sealed class WakeStitchStartedResult : WakeStitchResult
{
    public WakeStitchStartedResult(string transcriptText)
    {
        TranscriptText = transcriptText ?? throw new ArgumentNullException(nameof(transcriptText));
    }

    public string TranscriptText { get; }

    /// <inheritdoc />
    public override TResult Accept<TResult>(IWakeStitchResultVisitor<TResult> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        return visitor.VisitStarted(this);
    }
}
