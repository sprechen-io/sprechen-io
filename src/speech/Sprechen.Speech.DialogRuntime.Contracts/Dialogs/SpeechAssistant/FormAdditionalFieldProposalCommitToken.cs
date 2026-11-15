namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Einmalberechtigung, genau das vorbereitete Angebot zu übernehmen oder zu verwerfen.
/// Sie trägt nur eine undurchsichtige Kennung. Sitzung, Feld, Wert und Zustandsstand
/// bleiben beim Formularlauf, damit ein Aufrufer keine Bindung fälschen kann.
/// </summary>
/// <param name="ProposalId">Undurchsichtige Kennung des vorbereiteten Angebots.</param>
public sealed record FormAdditionalFieldProposalCommitToken(string ProposalId);
