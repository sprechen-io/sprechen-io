namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Endzustand einer Auswertung: <see cref="Submitted"/> bezeichnet die ausdrückliche
/// Festschreibung, <see cref="Aborted"/> die übrigen Endzustände.
/// Den Abbruchgrund unterscheidet <see cref="EvaluationAbortReason"/>.
/// </summary>
public enum EvaluationOutcome
{
    Submitted,
    Aborted
}
