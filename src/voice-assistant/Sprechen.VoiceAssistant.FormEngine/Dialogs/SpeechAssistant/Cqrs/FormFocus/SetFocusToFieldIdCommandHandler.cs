using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormFocus;

/// <summary>
/// Leitet <see cref="SetFocusToFieldIdCommand"/> an die Fokusprüfung und den Wechselversuch
/// des <see cref="FormNavigator"/> weiter.
/// </summary>
internal sealed class SetFocusToFieldIdCommandHandler : ICommandHandler<SetFocusToFieldIdCommand, bool>
{
    private readonly FormNavigator _navigator;

    public SetFocusToFieldIdCommandHandler(FormNavigator navigator)
    {
        _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
    }

    public Task<bool> HandleAsync(SetFocusToFieldIdCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        return Task.FromResult(_navigator.TrySetFocus(command.FieldId));
    }
}
