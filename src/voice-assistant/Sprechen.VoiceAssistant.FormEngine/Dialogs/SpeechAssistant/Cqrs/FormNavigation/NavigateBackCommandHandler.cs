using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;

/// <summary>
/// Leitet den Wechsel zum vorherigen verfügbaren Feld an die gemeinsame
/// Navigationsoperation weiter, die den Fokus setzt und das Ereignis anhängt.
/// </summary>
internal sealed class NavigateBackCommandHandler : FormNavigationCommandHandlerBase<NavigateBackCommand>
{
    public NavigateBackCommandHandler(INavigableFormState state, IEventStore eventStore)
        : base(state, eventStore)
    {
    }

    protected override Task<NavigationResult> MoveAsync(
        NavigateBackCommand command,
        CancellationToken cancellationToken)
    {
        return FormNavigationOperations.MoveBackAsync(
            State,
            EventStore,
            command.SessionId,
            cancellationToken);
    }
}

