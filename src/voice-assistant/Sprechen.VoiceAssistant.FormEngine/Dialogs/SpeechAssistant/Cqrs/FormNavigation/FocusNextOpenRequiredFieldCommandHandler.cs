using Sprechen.Common.Cqrs;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;

/// <summary>
/// Ermittelt offene Pflichtfelder mit der gemeinsamen Auswahlregel und übergibt den Fokuswechsel an die zuständige Navigation.
/// </summary>
internal sealed class FocusNextOpenRequiredFieldCommandHandler
    : ICommandHandler<FocusNextOpenRequiredFieldCommand, FocusNextOpenRequiredFieldResult>
{
    private readonly FormOpenRequiredFieldSelector _selector;
    private readonly FormOpenRequiredFieldFocusPolicy _focusPolicy;
    private readonly FormDialogValidator _validator;
    private readonly FormFieldVisibilityPolicy _visibilityPolicy;
    private readonly FormNavigator _navigator;
    private readonly FormMutableStateStore _mutableState;

    public FocusNextOpenRequiredFieldCommandHandler(
        FormOpenRequiredFieldNavigationDependencies dependencies,
        FormDialogValidator validator,
        FormFieldVisibilityPolicy visibilityPolicy,
        FormNavigator navigator,
        FormMutableStateStore mutableState)
    {
        ArgumentNullException.ThrowIfNull(dependencies);
        _selector = dependencies.Selector;
        _focusPolicy = dependencies.FocusPolicy;
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _visibilityPolicy = visibilityPolicy ?? throw new ArgumentNullException(nameof(visibilityPolicy));
        _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        _mutableState = mutableState ?? throw new ArgumentNullException(nameof(mutableState));
    }

    public Task<FocusNextOpenRequiredFieldResult> HandleAsync(
        FocusNextOpenRequiredFieldCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var violations = _validator.ValidateAll(_visibilityPolicy.IsFieldEffectivelyVisible);
        var openRequiredFieldIds = _selector.SelectOpenRequiredFieldIds(
            _mutableState.ValuesByVariableId,
            violations,
            _visibilityPolicy.IsFieldEffectivelyVisible);

        var result = _focusPolicy.TryFocusNextOpenRequired(
            _mutableState.FocusedFieldId?.Value,
            openRequiredFieldIds,
            _navigator.TrySetFocus);

        return Task.FromResult(result);
    }
}
