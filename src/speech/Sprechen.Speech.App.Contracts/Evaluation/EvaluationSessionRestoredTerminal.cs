namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Bei der Sitzungsinitialisierung wiederhergestellter Aufgabenabschluss.
/// Die Anwendung wartet auf die Annahme des ursprünglichen unveränderlichen Umschlags
/// und schreibt sie fest, statt die Aufgabe erneut zu starten.
/// </summary>
public sealed record EvaluationSessionRestoredTerminal(
    Task<EvaluationSessionAcceptance> Acceptance,
    ParticipantTaskTerminationClassification TerminationClassification)
{
    public Task<EvaluationSessionAcceptance> Acceptance { get; } =
        Acceptance ?? throw new ArgumentNullException(nameof(Acceptance));
}
