using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;

/// <summary>
/// Leitet den Wechsel zum nächsten verfügbaren Feld an die gemeinsame
/// Navigationsoperation weiter, die den Fokus setzt und das Ereignis anhängt.
/// </summary>
internal sealed class NavigateNextCommandHandler : FormNavigationCommandHandlerBase<NavigateNextCommand>
{
    public NavigateNextCommandHandler(INavigableFormState state, IEventStore eventStore)
        : base(state, eventStore)
    {
    }

    protected override Task<NavigationResult> MoveAsync(
        NavigateNextCommand command,
        CancellationToken cancellationToken)
    {
        return FormNavigationOperations.MoveNextAsync(
            State,
            EventStore,
            command.SessionId,
            cancellationToken);
    }
}

