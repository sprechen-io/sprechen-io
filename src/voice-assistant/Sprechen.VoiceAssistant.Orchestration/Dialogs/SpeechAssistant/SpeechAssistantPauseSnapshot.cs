namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Sichert Phase und ausgewählten Dialogkontext für das Fortsetzen nach einer Pause. Formularwerte
/// bleiben im gesonderten Anwendungsspeicher des Zustands.
/// </summary>
public sealed record SpeechAssistantPauseSnapshot(
    SpeechAssistantPhase Phase,
    SpeechAssistantDialogKind? ActiveDialog,
    string? CorrelationId,
    string? SelectedIsoCode,
    string? SelectedApplicationId,
    string? SelectedFormId)
{
    internal static SpeechAssistantPauseSnapshot Capture(SpeechAssistantState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return new SpeechAssistantPauseSnapshot(
            state.Phase,
            state.ActiveDialog,
            state.CorrelationId,
            state.SelectedIsoCode,
            state.SelectedApplicationId,
            state.SelectedFormId);
    }
}
