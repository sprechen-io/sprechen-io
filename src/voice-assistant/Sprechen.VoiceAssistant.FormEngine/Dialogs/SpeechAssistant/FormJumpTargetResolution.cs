namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Art und Kennung eines aufgelösten Sprungziels für die anschließende Navigation.
/// </summary>
internal sealed record FormJumpTargetResolution(FormJumpTargetKind Kind, string TargetId);
