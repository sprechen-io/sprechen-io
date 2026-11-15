namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Beschreibt einen Zyklus ohne Aktivierung samt lexikalischer Auswertung und
/// optionaler Verknüpfungsphase. Bei Started wird die vorgemerkte Teiläußerung
/// für den folgenden Zyklus mitgegeben.
/// </summary>
public sealed record WakeArbitrationOutcomeMiss : WakeArbitrationOutcome
{
    public WakeArbitrationOutcomeMiss(
        WakeWordActivationEvaluation evaluation,
        string? stitchStartedTranscriptText)
        : this(
            evaluation,
            stitchStartedTranscriptText is null ? null : WakeWordStitchPhase.Started,
            stitchStartedTranscriptText?.Length ?? 0,
            stitchStartedTranscriptText)
    {
    }

    public WakeArbitrationOutcomeMiss(
        WakeWordActivationEvaluation evaluation,
        WakeWordStitchPhase? stitchPhase,
        int stitchTranscriptLength,
        string? stitchStartedTranscriptText)
    {
        Evaluation = evaluation ?? throw new ArgumentNullException(nameof(evaluation));
        if (stitchTranscriptLength < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stitchTranscriptLength),
                stitchTranscriptLength,
                "Stitch transcript length must be non-negative.");
        }

        if (stitchPhase == WakeWordStitchPhase.Started
            && string.IsNullOrWhiteSpace(stitchStartedTranscriptText))
        {
            throw new ArgumentException(
                "WakeWordStitchPhase.Started requires the carried transcript text.",
                nameof(stitchStartedTranscriptText));
        }

        if (stitchPhase is not null
            and not WakeWordStitchPhase.Started
            && stitchStartedTranscriptText is not null)
        {
            throw new ArgumentException(
                "Only WakeWordStitchPhase.Started may carry the started-transcript text.",
                nameof(stitchStartedTranscriptText));
        }

        StitchPhase = stitchPhase;
        StitchTranscriptLength = stitchTranscriptLength;
        StitchStartedTranscriptText = stitchStartedTranscriptText;
    }

    public WakeWordActivationEvaluation Evaluation { get; }

    public WakeWordStitchPhase? StitchPhase { get; }

    public int StitchTranscriptLength { get; }

    public string? StitchStartedTranscriptText { get; }

    public bool StitchStarted => StitchPhase == WakeWordStitchPhase.Started;
}
