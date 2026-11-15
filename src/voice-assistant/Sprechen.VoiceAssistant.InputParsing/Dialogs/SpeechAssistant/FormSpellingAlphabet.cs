namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Enthält die modellierten Sprechwörter eines benannten Buchstabieralphabets
/// für die Zuordnung kanonischer Buchstaben zur Ausgabe.
/// </summary>
internal sealed class FormSpellingAlphabet
{
    public Dictionary<string, string> LetterWords { get; init; } = new(StringComparer.Ordinal);
}
