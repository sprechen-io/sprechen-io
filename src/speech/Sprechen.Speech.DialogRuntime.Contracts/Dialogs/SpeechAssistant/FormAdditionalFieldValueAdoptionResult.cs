namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ergebnis einer Übernahme mit dem Zustandsstand, den sie veröffentlicht hat. Aufrufer
/// bilden aus Angebotskennung und Stand die Veröffentlichungsmarke, damit Ansage und
/// Telemetrie je Ausgang genau einmal laufen.
/// </summary>
/// <param name="ProposalId">Undurchsichtige Kennung des übernommenen oder abgelehnten Angebots.</param>
/// <param name="CommitRevision">Veröffentlichter Zustandsstand, ohne Übernahme <c>0</c>.</param>
/// <param name="Status">Ausgang der Übernahme.</param>
/// <param name="BlockingViolation">Die Bedingung, an der die Übernahme scheiterte, sonst <c>null</c>.
/// Damit kann der Dialog die modellierte Meldung samt Abhilfe sprechen.</param>
public sealed record FormAdditionalFieldValueAdoptionResult(
    string ProposalId,
    long CommitRevision,
    FormAdditionalFieldValueAdoptionStatus Status,
    FormConstraintViolation? BlockingViolation = null);
