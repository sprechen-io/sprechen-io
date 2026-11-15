using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält Auswertungsergebnis, Dialogergebnis, Oberflächenzustand und möglichen Abbruchgrund eines Sitzungslaufs für dessen Aufrufer zusammen.
/// </summary>
internal sealed record FormVoiceCommandSessionRunResult(
    EvaluationOutcome Outcome,
    VoiceCommandDialogResult? DialogResult,
    FormSurfaceStage SurfaceStage,
    EvaluationAbortReason? AbortReason = null)
{
    public static FormVoiceCommandSessionRunResult Abandoned()
        => new(EvaluationOutcome.Aborted, DialogResult: null, FormSurfaceStage.Abandoned, AbortReason: EvaluationAbortReason.Abandoned);
}
