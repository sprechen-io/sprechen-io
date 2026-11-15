using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormFocus;

/// <summary>
/// Bündelt Fokusabfragen, gezielte Feldwechsel und die Prüfung verfügbarer Eingabemodi für den Formularablauf.
/// </summary>
internal sealed class CqrsFormFocusExecutor : ICqrsFormFocusExecutor
{
    private readonly GetFocusedFieldQueryHandler _getFocusedFieldHandler;
    private readonly SetFocusToFieldIdCommandHandler _setFocusToFieldIdHandler;
    private readonly SetFocusedFieldInputModeCommandHandler _setFocusedFieldInputModeHandler;
    private readonly FormMutableStateStore _mutableState;
    private readonly FormDialogDefinitionState _definitionState;
    private readonly FormFieldInputModeAvailabilityPolicy _inputModeAvailabilityPolicy;

    public CqrsFormFocusExecutor(
        FormFocusedFieldSnapshotBuilder snapshotBuilder,
        FormNavigator navigator,
        FormMutableStateStore mutableState,
        FormDialogDefinitionState definitionState,
        FormFieldInputModeAvailabilityPolicy inputModeAvailabilityPolicy)
    {
        ArgumentNullException.ThrowIfNull(snapshotBuilder);
        ArgumentNullException.ThrowIfNull(navigator);
        ArgumentNullException.ThrowIfNull(mutableState);
        ArgumentNullException.ThrowIfNull(definitionState);
        ArgumentNullException.ThrowIfNull(inputModeAvailabilityPolicy);

        _mutableState = mutableState;
        _definitionState = definitionState;
        _inputModeAvailabilityPolicy = inputModeAvailabilityPolicy;
        _getFocusedFieldHandler = new GetFocusedFieldQueryHandler(snapshotBuilder);
        _setFocusToFieldIdHandler = new SetFocusToFieldIdCommandHandler(navigator);
        _setFocusedFieldInputModeHandler = new SetFocusedFieldInputModeCommandHandler(
            mutableState,
            definitionState,
            inputModeAvailabilityPolicy);
    }

    public Task<FormFieldSnapshot> GetFocusedFieldAsync()
        => _getFocusedFieldHandler.HandleAsync(new GetFocusedFieldQuery());

    public Task<bool> SetFocusToFieldIdAsync(string fieldId)
    {
        if (string.IsNullOrWhiteSpace(fieldId))
        {
            return Task.FromResult(false);
        }

        return _setFocusToFieldIdHandler.HandleAsync(new SetFocusToFieldIdCommand(FieldId.From(fieldId)));
    }

    public Task<bool> SetFocusedFieldInputModeAsync(string inputMode)
        => _setFocusedFieldInputModeHandler.HandleAsync(new SetFocusedFieldInputModeCommand(inputMode));

    public Task<FormFieldInputModeAvailability> GetFocusedFieldInputModeAvailabilityAsync(string inputMode)
    {
        if (_mutableState.FocusedFieldId is not { } focusedFieldId)
        {
            // Ohne fokussiertes Feld fehlt das Eingabeziel. Die Rückmeldekennung beschreibt
            // diesen Zustand statt einen ungeeigneten Feldtyp.
            return Task.FromResult(
                FormFieldInputModeAvailability.Denied(FormFieldInputModeUnavailabilityReason.FieldNotEditable));
        }

        if (!InputModeExtensions.TryFromWireValue(inputMode, out var requestedInputMode))
        {
            throw new ArgumentException("The input mode token is not part of the modeled input-mode vocabulary.", nameof(inputMode));
        }

        var (field, variable) = _definitionState.ResolveFieldVariable(focusedFieldId);
        return Task.FromResult(_inputModeAvailabilityPolicy.Evaluate(field, variable, requestedInputMode));
    }

    public Task RecordDeniedFieldTurnAnnouncementAsync()
    {
        if (_mutableState.FocusedFieldId is { } focusedFieldId)
        {
            _inputModeAvailabilityPolicy.RecordDeniedFieldAnnouncement(focusedFieldId);
        }

        return Task.CompletedTask;
    }

    public Task<bool> HasDeniedFieldTurnAnnouncementAsync()
        => Task.FromResult(
            _mutableState.FocusedFieldId is { } focusedFieldId
            && _inputModeAvailabilityPolicy.HasDeniedFieldAnnouncement(focusedFieldId));
}
