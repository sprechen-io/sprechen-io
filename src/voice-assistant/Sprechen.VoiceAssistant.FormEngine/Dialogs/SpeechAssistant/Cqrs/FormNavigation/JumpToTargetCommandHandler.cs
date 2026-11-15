using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;

/// <summary>
/// Löst das Ziel über den <see cref="FormJumpTargetResolver"/> auf und versucht den
/// passenden Feld- oder Gruppenwechsel über den <see cref="FormNavigator"/>. Ein aufgelöstes
/// Ziel ohne erfolgreichen Fokusversuch wird als <see cref="FormJumpExecutionKind.AudibleOnlyTarget"/>
/// mit seiner Auflösung weitergegeben. Nur die fehlende Auflösung ergibt keinen Treffer.
/// </summary>
internal sealed class JumpToTargetCommandHandler : ICommandHandler<JumpToTargetCommand, FormJumpExecution>
{
    private readonly FormJumpTargetResolver _jumpTargetResolver;
    private readonly FormNavigator _navigator;

    public JumpToTargetCommandHandler(
        FormJumpTargetResolver jumpTargetResolver,
        FormNavigator navigator)
    {
        _jumpTargetResolver = jumpTargetResolver ?? throw new ArgumentNullException(nameof(jumpTargetResolver));
        _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
    }

    public Task<FormJumpExecution> HandleAsync(JumpToTargetCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!_jumpTargetResolver.TryResolve(command.Target, out var resolution) || resolution is null)
        {
            return Task.FromResult(FormJumpExecution.NotFound());
        }

        var focused = resolution.Kind switch
        {
            FormJumpTargetKind.Field => _navigator.TrySetFocus(resolution.TargetId),
            FormJumpTargetKind.FieldGroup => _navigator.TryNavigateToFieldGroup(resolution.TargetId),
            _ => false
        };

        return Task.FromResult(focused
            ? FormJumpExecution.Focused()
            : FormJumpExecution.AudibleOnly(resolution));
    }
}
