namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Führt eine Formularsitzung mit Spracheingabe aus einem geladenen Kontext aus.
/// </summary>
public interface IFormVoiceCommandSessionRunner
{
    Task<VoiceCommandDialogResult> RunAsync(FormVoiceCommandContext context, CancellationToken cancellationToken);
}

