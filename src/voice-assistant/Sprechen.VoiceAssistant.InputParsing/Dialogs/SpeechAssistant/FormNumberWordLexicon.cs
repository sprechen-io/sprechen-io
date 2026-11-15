namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet gesprochene Zahlwörter den Einern, Zahlen von 10 bis 19, Zehnern und Größenordnungen zu.
/// Die Regeln für zusammengesetzte Zahlen liegen in den Sprachparsern.
/// </summary>
internal sealed class FormNumberWordLexicon
{
    public Dictionary<string, int> Units { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, int> Teens { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, int> Tens { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, long> Scales { get; init; } = new(StringComparer.Ordinal);
}
