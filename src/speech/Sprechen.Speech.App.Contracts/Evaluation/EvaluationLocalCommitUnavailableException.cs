namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>Zeigt an, dass eine angenommene Evaluationsquittung lokal noch nicht dauerhaft als festgeschrieben markiert werden konnte.</summary>
public sealed class EvaluationLocalCommitUnavailableException(Exception innerException)
    : Exception("The accepted evaluation receipt could not yet be committed locally.", innerException);
