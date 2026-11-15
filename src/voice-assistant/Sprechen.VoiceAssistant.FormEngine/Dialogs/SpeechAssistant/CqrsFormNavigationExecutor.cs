using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;
using Sprechen.Common.Cqrs;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Navigation, Zielsprünge und den Fokus auf Validierungsverstöße beziehungsweise offene Pflichtfelder über ihre jeweiligen Handler.
/// </summary>
internal sealed class CqrsFormNavigationExecutor : ICqrsFormNavigationExecutor
{
    private readonly NavigateNextCommandHandler _navigateNextHandler;
    private readonly NavigateBackCommandHandler _navigateBackHandler;
    private readonly NavigateToFieldGroupCommandHandler _navigateToFieldGroupHandler;
    private readonly JumpToTargetCommandHandler _jumpToTargetHandler;
    private readonly FocusNextErrorCommandHandler _focusNextErrorHandler;
    private readonly FocusNextOpenRequiredFieldCommandHandler _focusNextOpenRequiredFieldHandler;

    public CqrsFormNavigationExecutor(
        INavigableFormState navigationState,
        IFieldGroupNavigableFormState fieldGroupState,
        IEventStore eventStore,
        FormJumpTargetResolver jumpTargetResolver,
        FormValidationErrorFocusPolicy errorFocusPolicy,
        FormOpenRequiredFieldNavigationDependencies openRequiredNavigation,
        FormFocusCommandDependencies focusCommands)
    {
        ArgumentNullException.ThrowIfNull(navigationState);
        ArgumentNullException.ThrowIfNull(fieldGroupState);
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(jumpTargetResolver);
        ArgumentNullException.ThrowIfNull(errorFocusPolicy);
        ArgumentNullException.ThrowIfNull(openRequiredNavigation);
        ArgumentNullException.ThrowIfNull(focusCommands);

        _navigateNextHandler = new NavigateNextCommandHandler(navigationState, eventStore);
        _navigateBackHandler = new NavigateBackCommandHandler(navigationState, eventStore);
        _navigateToFieldGroupHandler = new NavigateToFieldGroupCommandHandler(fieldGroupState, eventStore);
        _jumpToTargetHandler = new JumpToTargetCommandHandler(jumpTargetResolver, focusCommands.Navigator);
        _focusNextErrorHandler = new FocusNextErrorCommandHandler(
            errorFocusPolicy, focusCommands.Validator, focusCommands.VisibilityPolicy, focusCommands.Navigator, focusCommands.MutableState);
        _focusNextOpenRequiredFieldHandler = new FocusNextOpenRequiredFieldCommandHandler(
            openRequiredNavigation, focusCommands.Validator, focusCommands.VisibilityPolicy, focusCommands.Navigator, focusCommands.MutableState);
    }

    public Task<NavigationResult> NavigateNextAsync(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return _navigateNextHandler.HandleAsync(new NavigateNextCommand(sessionId));
    }

    public Task<NavigationResult> NavigateBackAsync(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return _navigateBackHandler.HandleAsync(new NavigateBackCommand(sessionId));
    }

    public Task<NavigationResult> NavigateToFieldGroupAsync(string sessionId, string fieldGroupId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldGroupId);
        return _navigateToFieldGroupHandler.HandleAsync(new NavigateToFieldGroupCommand(sessionId, FieldGroupId.From(fieldGroupId)));
    }

    public Task<FormJumpExecution> JumpToTargetAsync(FormJumpTarget target)
        => _jumpToTargetHandler.HandleAsync(new JumpToTargetCommand(target));

    public Task<FormConstraintViolation?> FocusNextErrorAsync()
        => _focusNextErrorHandler.HandleAsync(new FocusNextErrorCommand());

    public Task<FocusNextOpenRequiredFieldResult> FocusNextOpenRequiredFieldAsync()
        => _focusNextOpenRequiredFieldHandler.HandleAsync(new FocusNextOpenRequiredFieldCommand());
}

