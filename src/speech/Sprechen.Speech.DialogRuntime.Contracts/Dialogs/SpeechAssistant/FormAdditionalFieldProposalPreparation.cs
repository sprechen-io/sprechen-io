namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ergebnis der Vorbereitung: der Zustand des Angebots und, nur bei
/// <see cref="FormAdditionalFieldProposalPreparationStatus.Prepared"/>, der sprechbare
/// Angebotstext samt Einmalberechtigung. Jeder andere Zustand trägt beides nicht, damit
/// kein Angebot ohne geprüftes Ziel gesprochen wird.
/// </summary>
/// <param name="Status">Ausgang der Vorbereitung.</param>
/// <param name="SpokenOffer">Datenschutzgerechter Werttext für das Angebot, sonst <c>null</c>.</param>
/// <param name="CommitToken">Einmalberechtigung für Übernahme oder Verwerfen, sonst <c>null</c>.</param>
public sealed record FormAdditionalFieldProposalPreparation(
    FormAdditionalFieldProposalPreparationStatus Status,
    string? SpokenOffer,
    FormAdditionalFieldProposalCommitToken? CommitToken);
