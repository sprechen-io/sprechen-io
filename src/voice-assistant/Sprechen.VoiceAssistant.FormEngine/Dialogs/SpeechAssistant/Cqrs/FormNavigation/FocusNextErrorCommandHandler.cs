using Sprechen.Common.Cqrs;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;

/// <summary>
/// Validiert die sichtbaren Felder und verbindet die Reihenfolge der Validierungsverstöße
/// mit dem Navigationsversuch zum nächsten betroffenen Feld.
/// </summary>
internal sealed class FocusNextErrorCommandHandler : ICommandHandler<FocusNextErrorCommand, FormConstraintViolation?>
{
    private readonly FormValidationErrorFocusPolicy _errorFocusPolicy;
    private readonly FormDialogValidator _validator;
    private readonly FormFieldVisibilityPolicy _visibilityPolicy;
    private readonly FormNavigator _navigator;
    private readonly FormMutableStateStore _mutableState;

    public FocusNextErrorCommandHandler(
        FormValidationErrorFocusPolicy errorFocusPolicy,
        FormDialogValidator validator,
        FormFieldVisibilityPolicy visibilityPolicy,
        FormNavigator navigator,
        FormMutableStateStore mutableState)
    {
        _errorFocusPolicy = errorFocusPolicy ?? throw new ArgumentNullException(nameof(errorFocusPolicy));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _visibilityPolicy = visibilityPolicy ?? throw new ArgumentNullException(nameof(visibilityPolicy));
        _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        _mutableState = mutableState ?? throw new ArgumentNullException(nameof(mutableState));
    }

    public Task<FormConstraintViolation?> HandleAsync(FocusNextErrorCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var violations = _validator.ValidateAll(_visibilityPolicy.IsFieldEffectivelyVisible);
        var focused = _errorFocusPolicy.TryFocusNextError(
            _mutableState.FocusedFieldId?.Value,
            violations,
            _validator.OrderViolationsForField,
            _navigator.TrySetFocus);

        return Task.FromResult(focused);
    }
}
