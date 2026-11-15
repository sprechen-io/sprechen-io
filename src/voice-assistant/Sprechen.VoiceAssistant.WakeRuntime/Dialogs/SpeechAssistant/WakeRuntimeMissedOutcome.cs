namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Enthält das Ergebnis eines Erkennungsdurchlaufs ohne Aktivierung.
/// </summary>
public sealed record WakeRuntimeMissedOutcome : WakeRuntimeOutcome
{
    public WakeRuntimeMissedOutcome(
        WakeWordActivationEvaluation evaluation,
        string correlationId)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        Status = evaluation.Status;
        ConfidencePercent = evaluation.ConfidencePercent;
        CorrelationId = correlationId.Trim();
    }

    public WakeWordActivationStatus Status { get; }

    public int? ConfidencePercent { get; }

    public string CorrelationId { get; }
}
