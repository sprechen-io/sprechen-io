using Sprechen.Common.Cqrs;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;

/// <summary>
/// Erstellt den ausführlichen Formularstatus aus Werten und der Validierung sichtbarer Felder. Ein fokussiertes Feld wird dafür nicht benötigt.
/// </summary>
internal sealed class GetDetailedFormStatusQueryHandler : IQueryHandler<GetDetailedFormStatusQuery, FormDetailedStatusSnapshot>
{
    private readonly FormStatusProjectionBuilder _statusProjectionBuilder;
    private readonly FormDialogValidator _validator;
    private readonly FormFieldVisibilityPolicy _visibilityPolicy;
    private readonly FormMutableStateStore _mutableState;

    public GetDetailedFormStatusQueryHandler(
        FormStatusProjectionBuilder statusProjectionBuilder,
        FormDialogValidator validator,
        FormFieldVisibilityPolicy visibilityPolicy,
        FormMutableStateStore mutableState)
    {
        _statusProjectionBuilder = statusProjectionBuilder ?? throw new ArgumentNullException(nameof(statusProjectionBuilder));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _visibilityPolicy = visibilityPolicy ?? throw new ArgumentNullException(nameof(visibilityPolicy));
        _mutableState = mutableState ?? throw new ArgumentNullException(nameof(mutableState));
    }

    public Task<FormDetailedStatusSnapshot> HandleAsync(GetDetailedFormStatusQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var violations = _validator.ValidateAll(_visibilityPolicy.IsFieldEffectivelyVisible);
        var snapshot = _statusProjectionBuilder.BuildDetailedStatusSnapshot(
            _mutableState.ValuesByVariableId,
            violations,
            _visibilityPolicy.IsFieldEffectivelyVisible);
        return Task.FromResult(snapshot);
    }
}
