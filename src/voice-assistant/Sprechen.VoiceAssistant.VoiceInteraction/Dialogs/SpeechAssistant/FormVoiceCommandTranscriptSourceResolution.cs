namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet die ursprüngliche Transkriptquelle mit dem bereinigten Befehlstext.
/// </summary>
internal sealed record FormVoiceCommandTranscriptSourceResolution(
    FormVoiceCommandTranscriptSource Source,
    string CommandTranscript);
