namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ein Erfassungsversuch bei der Wiederholung einer Formulareingabe oder einer
/// Bestätigungsnachfrage. Die Wiederholungsschleife meldet jeden Versuch, damit
/// die gesprochene Rückmeldung auf den vorangehenden Fehler reagieren kann.
/// Der Konstruktor prüft Nummer und Budget. Verbraucher müssen trotzdem den
/// Wert <c>default</c> abweisen, weil dieser den Konstruktor umgeht.
/// </summary>
public readonly record struct FormVoiceCommandAttempt
{
    /// <param name="attemptNumber">Der 1-basierte Index dieses Versuchs im Budget.</param>
    /// <param name="attemptBudget">Die Gesamtzahl der Versuche in der ausführenden Schleife (Wiederholungen + 1).</param>
    /// <param name="precedingFailure">Warum der unmittelbar vorangehende Versuch scheiterte, oder <see cref="FormVoiceCommandAttemptFailure.None"/> für den ersten Versuch.</param>
    public FormVoiceCommandAttempt(
        int attemptNumber,
        int attemptBudget,
        FormVoiceCommandAttemptFailure precedingFailure)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(attemptBudget, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(attemptNumber, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(attemptNumber, attemptBudget);
        if (!Enum.IsDefined(precedingFailure))
        {
            throw new ArgumentOutOfRangeException(nameof(precedingFailure), precedingFailure, "Undefined attempt-failure kind.");
        }

        AttemptNumber = attemptNumber;
        AttemptBudget = attemptBudget;
        PrecedingFailure = precedingFailure;
    }

    public int AttemptNumber { get; }

    public int AttemptBudget { get; }

    public FormVoiceCommandAttemptFailure PrecedingFailure { get; }
}
