using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Bildet das Sitzungsergebnis aus Dialogausgang und Zustand, damit der Aufrufer Abschluss und Unterbrechung unterscheiden kann.
/// </summary>
internal sealed class FormVoiceCommandSessionResultFactory
{
    public VoiceCommandDialogResult Create(
        FormVoiceCommandContext context,
        FormVoiceCommandSessionRunResult runResult,
        ImmutableDictionary<string, string> finalValues)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(runResult);
        ArgumentNullException.ThrowIfNull(finalValues);

        if (runResult.DialogResult is not null)
        {
            return runResult.DialogResult;
        }

        return runResult.Outcome switch
        {
            EvaluationOutcome.Submitted => VoiceCommandDialogResult.Submitted(context.Contract.ApplicationId, context.Contract.FormId, finalValues),
            EvaluationOutcome.Aborted when runResult.AbortReason == EvaluationAbortReason.Cancelled => VoiceCommandDialogResult.Cancelled(context.Contract.ApplicationId, context.Contract.FormId, finalValues),
            _ => VoiceCommandDialogResult.Abandoned(context.Contract.ApplicationId, context.Contract.FormId, finalValues)
        };
    }

    public VoiceCommandDialogResult CreatePaused(
        FormVoiceCommandContext context,
        ImmutableDictionary<string, string> finalValues)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(finalValues);

        return VoiceCommandDialogResult.Paused(context.Contract.ApplicationId, context.Contract.FormId, finalValues);
    }

    public FormSurfaceStage MapSurfaceStage(EvaluationOutcome outcome, EvaluationAbortReason? abortReason)
    {
        return outcome switch
        {
            EvaluationOutcome.Submitted => FormSurfaceStage.Submitted,
            EvaluationOutcome.Aborted when abortReason == EvaluationAbortReason.Cancelled => FormSurfaceStage.Cancelled,
            _ => FormSurfaceStage.Abandoned
        };
    }
}
