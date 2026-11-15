namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Unterscheidet bei <see cref="EvaluationOutcome.Aborted"/> einen ausdrücklichen
/// Nutzerabbruch (<see cref="Cancelled"/>) von einem Sitzungsende ohne ausdrücklichen
/// Abbruch (<see cref="Abandoned"/>), etwa durch Timeout oder Wegnavigieren.
/// </summary>
public enum EvaluationAbortReason
{
    Cancelled,
    Abandoned
}
