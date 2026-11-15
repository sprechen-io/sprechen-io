namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Zählt abgeschlossene Elemente und Gesamtzahl einer Fortschrittsachse
/// für die Auswertungstelemetrie. Beide Werte sind als nichtnegative Zähler vorgesehen.
/// </summary>
public sealed record EvaluationFormProgressCounter(int Completed, int Total);
