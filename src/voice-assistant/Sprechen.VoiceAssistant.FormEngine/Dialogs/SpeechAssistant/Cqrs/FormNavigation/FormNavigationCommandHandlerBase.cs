using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;

/// <summary>
/// Hält Navigationszustand und Ereignisspeicher bereit und leitet nichtleere Befehle an die konkrete Navigationsoperation weiter.
/// </summary>
internal abstract class FormNavigationCommandHandlerBase<TCommand> : ICommandHandler<TCommand, NavigationResult>
    where TCommand : class, ICommand<NavigationResult>
{
    private readonly INavigableFormState _state;
    private readonly IEventStore _eventStore;

    protected FormNavigationCommandHandlerBase(INavigableFormState state, IEventStore eventStore)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
    }

    protected INavigableFormState State => _state;

    protected IEventStore EventStore => _eventStore;

    public Task<NavigationResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return MoveAsync(command, cancellationToken);
    }

    protected abstract Task<NavigationResult> MoveAsync(
        TCommand command,
        CancellationToken cancellationToken);
}

