namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Enthält sprachbezogene Schlüsselwörter für Uhrzeiteingaben nach ihrer Rolle.
/// Der Uhrzeitparser verbindet sie zu Formen wie „halb drei“ oder „viertel vor drei“.
/// </summary>
internal sealed class FormTimeInputKeywords
{
    public string HalfHour { get; init; } = string.Empty;

    public string Quarter { get; init; } = string.Empty;

    public string Before { get; init; } = string.Empty;

    public string After { get; init; } = string.Empty;

    public string ClockMarker { get; init; } = string.Empty;
}
