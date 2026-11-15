namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Eine Auswertungssitzung mit ihrem Kanal, damit Studienbedingung und Telemetrie dieselbe Identität
/// tragen.
/// </summary>
/// <param name="SessionId">Pseudonymisierte Sitzungskennung.</param>
/// <param name="Channel">Interaktionskanal (voice/html) für die Studienbedingung.</param>
/// <param name="ApplicationId">Anwendungskennung, abgebildet auf Telemetrie <c>application_id</c>.</param>
/// <param name="FormId">Formularkennung, abgebildet auf Telemetrie <c>task_id</c>.</param>
public sealed record EvaluationSession(string SessionId, EvaluationChannel Channel, string ApplicationId, string FormId)
{
    /// <summary>
    /// Wird bei Wiederherstellung eines Aufgabenabschlusses gesetzt, der die
    /// aktive Aufgabenausführung bereits verlassen hatte. Der Aufrufer wartet auf diese Quittung
    /// und schreibt sie fest, statt die Aufgabe erneut auszuführen.
    /// </summary>
    public EvaluationSessionRestoredTerminal? RestoredTerminal { get; init; }
}

