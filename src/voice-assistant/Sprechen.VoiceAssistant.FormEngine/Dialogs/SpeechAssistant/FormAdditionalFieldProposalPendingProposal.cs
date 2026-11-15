using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Bewahrt das vorbereitete Angebot mit Ziel, Wert und Formularzustand intern auf. Der Aufrufer erhält nur eine Kennung und kann diese Angebotsdaten beim Bestätigen nicht ersetzen.
/// </summary>
/// <param name="ProposalId">Kennung, die der Aufrufer beim Bestätigen zurückgibt.</param>
/// <param name="FieldId">Zielfeld des Angebots.</param>
/// <param name="VariableId">Variable des Zielfelds.</param>
/// <param name="ValueSegment">Transkriptabschnitt, der bei der Übernahme erneut für das Feld ausgewertet wird.</param>
/// <param name="CanonicalValue">Geparste Speicherform bei der Vorbereitung.</param>
/// <param name="StateRevision">Formularzustand bei der Vorbereitung des Angebots.</param>
internal sealed record FormAdditionalFieldProposalPendingProposal(
    string ProposalId,
    FieldId FieldId,
    string VariableId,
    string ValueSegment,
    string CanonicalValue,
    long StateRevision);
