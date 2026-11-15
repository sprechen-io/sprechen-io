namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Behält aus Code-Eingaben die als Buchstaben oder Ziffern erkannten Zeichen bei
/// und entfernt alle übrigen Zeichen.
/// </summary>
internal static class FormCodeTranscriptNormalizer
{
    public static string Normalize(string transcript)
    {
        var trimmed = transcript.Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        var builder = new char[trimmed.Length];
        var count = 0;
        foreach (var ch in trimmed)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder[count++] = ch;
            }
        }

        return new string(builder, 0, count);
    }
}
