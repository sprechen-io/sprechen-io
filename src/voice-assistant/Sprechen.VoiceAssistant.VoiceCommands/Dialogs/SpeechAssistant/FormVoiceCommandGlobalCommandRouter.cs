namespace Sprechen.VoiceAssistant.VoiceCommands.Dialogs.SpeechAssistant;

/// <summary>
/// Leitet einen globalen Formularbefehl an die erste zuständige registrierte
/// Verarbeitungsfunktion weiter und gibt deren Dialogergebnis zurück.
/// </summary>
internal sealed class FormVoiceCommandGlobalCommandRouter : IFormVoiceCommandGlobalCommandRouter
{
    private readonly IReadOnlyList<IFormVoiceCommandGlobalCommandHandler> _handlers;

    public FormVoiceCommandGlobalCommandRouter(IEnumerable<IFormVoiceCommandGlobalCommandHandler> handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);
        _handlers = handlers.ToList();
    }

    public Task<VoiceCommandDialogResult?> TryHandleAsync(
        CommandId commandId,
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken)
    {
        foreach (var handler in _handlers)
        {
            if (handler.CanHandle(commandId))
            {
                return handler.HandleAsync(context, cancellationToken);
            }
        }

        return Task.FromResult<VoiceCommandDialogResult?>(null);
    }
}


