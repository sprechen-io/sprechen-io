namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält den zur Ansage bestimmten Wert und den Status seiner Aufbereitung zusammen.
/// </summary>
internal sealed record FormVoiceCommandUnderstoodValue(
    string Value,
    bool IncludesValue,
    FormSpokenValueProjectionStatus Status)
{
    public static FormVoiceCommandUnderstoodValue WithoutValue { get; } =
        new(string.Empty, IncludesValue: false, FormSpokenValueProjectionStatus.Empty);
}
