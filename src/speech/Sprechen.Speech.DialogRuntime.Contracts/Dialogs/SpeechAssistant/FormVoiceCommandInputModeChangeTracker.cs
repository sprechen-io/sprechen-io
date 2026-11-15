using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Wendet einen Eingabemodus an und protokolliert für die Evaluation nur eine tatsächlich beobachtete Änderung.
/// </summary>
public static class FormVoiceCommandInputModeChangeTracker
{
    public static async Task<bool> TryApplyAndTrackAsync(
        IFormDialogRuntime interpreter,
        IEvaluationFormTelemetry evaluationTelemetry,
        EvaluationSession session,
        InputMode targetInputMode,
        string trigger)
    {
        ArgumentNullException.ThrowIfNull(interpreter);
        ArgumentNullException.ThrowIfNull(evaluationTelemetry);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(trigger);

        var focusedField = await interpreter.GetFocusedFieldAsync().ConfigureAwait(false);
        var fromMode = focusedField.InputMode;

        if (!await interpreter.TrySetFocusedFieldInputModeAsync(targetInputMode.ToWireValue()).ConfigureAwait(false))
        {
            return false;
        }

        var updatedField = await interpreter.GetFocusedFieldAsync().ConfigureAwait(false);
        if (fromMode != updatedField.InputMode)
        {
            evaluationTelemetry.TrackInputModeChanged(
                session,
                updatedField.FieldId,
                fromMode.ToWireValue(),
                updatedField.InputMode.ToWireValue(),
                trigger);
        }

        return true;
    }
}
