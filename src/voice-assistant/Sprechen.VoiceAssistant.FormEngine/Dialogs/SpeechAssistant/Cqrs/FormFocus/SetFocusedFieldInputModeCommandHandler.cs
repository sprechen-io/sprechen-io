using Sprechen.Common.Cqrs;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormFocus;

/// <summary>
/// Prüft, ob der gewünschte Eingabemodus für das fokussierte Feld verfügbar ist, und speichert ihn als Feldvorgabe. Bei Ablehnung bleibt die bisherige Vorgabe erhalten.
/// </summary>
internal sealed class SetFocusedFieldInputModeCommandHandler : ICommandHandler<SetFocusedFieldInputModeCommand, bool>
{
    private readonly FormMutableStateStore _mutableState;
    private readonly FormDialogDefinitionState _definitionState;
    private readonly FormFieldInputModeAvailabilityPolicy _inputModeAvailabilityPolicy;

    public SetFocusedFieldInputModeCommandHandler(
        FormMutableStateStore mutableState,
        FormDialogDefinitionState definitionState,
        FormFieldInputModeAvailabilityPolicy inputModeAvailabilityPolicy)
    {
        _mutableState = mutableState ?? throw new ArgumentNullException(nameof(mutableState));
        _definitionState = definitionState ?? throw new ArgumentNullException(nameof(definitionState));
        _inputModeAvailabilityPolicy = inputModeAvailabilityPolicy ?? throw new ArgumentNullException(nameof(inputModeAvailabilityPolicy));
    }

    public Task<bool> HandleAsync(SetFocusedFieldInputModeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_mutableState.FocusedFieldId is not { } focusedFieldId)
        {
            return Task.FromResult(false);
        }

        if (!InputModes.TryNormalize(command.InputMode, out var normalizedInputMode)
            || !InputModeExtensions.TryFromWireValue(normalizedInputMode, out var requestedInputMode))
        {
            return Task.FromResult(false);
        }

        var (field, variable) = _definitionState.ResolveFieldVariable(focusedFieldId);
        var availability = _inputModeAvailabilityPolicy.Evaluate(field, variable, requestedInputMode);

        // Eine nur für den aktuellen Dialogschritt erteilte Einwilligung erlaubt keine dauerhafte Modusvorgabe. Sie wird gesondert beim Feldeinstieg behandelt.
        if (availability.Status is not FormFieldInputModeAvailabilityStatus.Available)
        {
            return Task.FromResult(false);
        }

        _mutableState.SetFieldInputModeOverride(focusedFieldId, normalizedInputMode);
        return Task.FromResult(true);
    }
}
