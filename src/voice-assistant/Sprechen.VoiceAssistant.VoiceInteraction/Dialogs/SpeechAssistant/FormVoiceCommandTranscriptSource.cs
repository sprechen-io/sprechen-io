namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält Transkripttexte und erkannte Befehlspräfixe eines gesprochenen Eingabeschritts
/// zusammen, damit Eingabe- und Befehlsinterpretation dieselbe Quelle verwenden.
/// </summary>
public sealed record FormVoiceCommandTranscriptSource(
    string TrimmedTranscript,
    string InputTranscript,
    bool HasExplicitCommandPrefix);
