namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt die Erkennung zusätzlich genannter Feldwerte und die Entscheidung über deren Angebot und Übernahme für den Formularablauf.
/// </summary>
/// <param name="Extractor">Erkennt Feldnamen und zugehörige Wertsegmente im Transkript.</param>
/// <param name="Policy">Steuert Vorbereitung, Übernahme und Verwerfen eines Angebots.</param>
internal sealed record FormAdditionalFieldProposalDependencies(
    IMultiSlotValueExtractor Extractor,
    FormAdditionalFieldProposalPolicy Policy);
