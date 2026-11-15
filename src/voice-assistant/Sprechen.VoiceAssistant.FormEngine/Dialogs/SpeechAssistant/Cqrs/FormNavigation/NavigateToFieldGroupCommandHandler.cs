using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation.Events;
using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;

/// <summary>
/// Leitet den Gruppenwechsel an die gemeinsame Navigationsoperation weiter, die das
/// erste verfügbare Feld der Gruppe fokussiert und das Gruppenereignis anhängt.
/// </summary>
internal sealed class NavigateToFieldGroupCommandHandler : ICommandHandler<NavigateToFieldGroupCommand, NavigationResult>
{
    private readonly IFieldGroupNavigableFormState _state;
    private readonly IEventStore _eventStore;

    public NavigateToFieldGroupCommandHandler(IFieldGroupNavigableFormState state, IEventStore eventStore)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
    }

    public async Task<NavigationResult> HandleAsync(NavigateToFieldGroupCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await FormNavigationOperations.NavigateToFieldGroupAsync(
                _state,
                _eventStore,
                command.SessionId,
                command.FieldGroupId,
                cancellationToken)
            .ConfigureAwait(false);
    }
}


