namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Kapselt die Erkennung und Bearbeitung unterstützter globaler Befehle für die Auswahl durch den Befehlsrouter.
/// </summary>
public interface IFormVoiceCommandGlobalCommandHandler
{
    bool CanHandle(CommandId commandId);

    Task<VoiceCommandDialogResult?> HandleAsync(
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken);
}


