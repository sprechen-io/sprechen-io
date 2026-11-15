namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt das Transkriptlexikon als austauschbare Datenquelle
/// für die Konstruktion der Eingabeparser bereit.
/// </summary>
internal interface IFormTranscriptLexiconProvider
{
    FormTranscriptLexicon GetLexicon();
}
