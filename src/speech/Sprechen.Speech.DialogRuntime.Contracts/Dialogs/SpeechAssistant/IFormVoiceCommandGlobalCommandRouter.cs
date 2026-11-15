namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Leitet globale Formularbefehle an einen zuständigen Befehlshandler weiter.
/// </summary>
public interface IFormVoiceCommandGlobalCommandRouter
{
    Task<VoiceCommandDialogResult?> TryHandleAsync(
        CommandId commandId,
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken);
}


