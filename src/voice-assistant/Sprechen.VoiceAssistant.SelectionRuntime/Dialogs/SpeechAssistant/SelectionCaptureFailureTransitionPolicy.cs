namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Bestimmt die Grenze aufeinanderfolgender eingeordneter Aufnahmefehler für Auswahlzüge. Auch bei
/// einem konfigurierten Wert von null endet der Ablauf erst am ersten Fehler. Fehlende Eingabe hat
/// einen gesonderten Zähler.
/// </summary>
public sealed class SelectionCaptureFailureTransitionPolicy
{
    private readonly int _effectiveFailureBoundary;

    public SelectionCaptureFailureTransitionPolicy(int maximumRetries)
    {
        if (maximumRetries < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumRetries),
                maximumRetries,
                "The recoverable capture retry budget cannot be negative.");
        }

        _effectiveFailureBoundary = Math.Max(maximumRetries, 1);
    }

    public SelectionCaptureFailureTransitionDecision Evaluate(
        string failureCode,
        int currentFailureCount,
        SelectionDialogPhase phase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(failureCode);

        // Unbekannte Fehlercodes müssen vor der Budgetentscheidung scheitern. Der Zähler behandelt
        // klassifizierte Aufnahmefehler. Fehlende Eingabe läuft über den gesonderten Rückmeldeweg.
        SelectionCaptureFaultAttributionMap.EnsureAttributed(failureCode);

        if (phase is not (SelectionDialogPhase.Listening or SelectionDialogPhase.Confirming))
        {
            throw new SelectionCandidateStateException(
                "A capture failure can only be evaluated in the listening or confirming phase.");
        }

        if (currentFailureCount < 0 || currentFailureCount >= _effectiveFailureBoundary)
        {
            throw new SelectionCandidateStateException(
                "The consecutive capture-failure count is outside the active execution budget.");
        }

        return new SelectionCaptureFailureTransitionDecision(
            currentFailureCount + 1 >= _effectiveFailureBoundary
                ? SelectionCaptureFailureTransitionKind.Terminal
                : SelectionCaptureFailureTransitionKind.Retry,
            currentFailureCount);
    }
}
