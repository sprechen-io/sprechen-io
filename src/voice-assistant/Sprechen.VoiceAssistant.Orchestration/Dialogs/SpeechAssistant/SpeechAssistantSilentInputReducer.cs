using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Steuert den Übergang zur stillen Bearbeitung und übernimmt die beim Pausieren
/// erhaltenen Formularwerte. Transaktionsentscheidungen delegiert er an den zuständigen Reducer.
/// </summary>
internal static class SpeechAssistantSilentInputReducer
{
    internal static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>? TryReduce(
        SpeechAssistantState state,
        SpeechAssistantEvent @event)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(@event);

        return @event switch
        {
            SpeechAssistantSilentInputReadySpokenEvent spoken => CompleteReady(state, spoken),
            SpeechAssistantSilentInputReadyPlaybackFailedEvent failed => FailReady(state, failed),
            SpeechAssistantSilentInputEnterRequestedEvent enter => Enter(state, enter.Expected),
            SpeechAssistantSilentInputSubmitRequestedEvent submit =>
                SpeechAssistantSilentInputCommitReducer.Submit(state, submit.Expected, submit.Value),
            SpeechAssistantSilentInputClearRequestedEvent clear =>
                SpeechAssistantSilentInputCommitReducer.Clear(state, clear.Expected),
            SpeechAssistantSilentInputCancelRequestedEvent cancel =>
                SpeechAssistantSilentInputCommitReducer.Cancel(state, cancel.Expected),
            SpeechAssistantSilentInputTransactionCompletedEvent completed =>
                SpeechAssistantSilentInputCommitReducer.CompleteTransaction(state, completed),
            SpeechAssistantSilentInputAcceptedEvent =>
                StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithoutEffects(state),
            _ => null
        };
    }

    internal static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> MergePausedValues(
        SpeechAssistantState state,
        VoiceCommandDialogResult result)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(result);

        var updatedSessions = SpeechAssistantApplicationSessionStateProjector.UpdateApplicationSessions(state, result);
        if (!result.IsSilentInputRequested || result.SilentInputEligibility is null)
        {
            return StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithoutEffects(
                state with { ApplicationSessions = updatedSessions });
        }

        var originRevision = state.SurfaceRevision;
        var nextRevision = checked(originRevision + 1);
        var next = state with
        {
            ApplicationSessions = updatedSessions,
            SurfaceRevision = nextRevision,
            SessionId = state.SessionId ?? Guid.NewGuid().ToString("N"),
            SilentInput = new SpeechAssistantFormSurfaceInteractionState(
                SpeechAssistantSilentInputPhase.Transitioning,
                result.SilentInputEligibility.FieldId,
                result.SilentInputEligibility.ControlKind,
                originRevision,
                RepairKind: null)
        };
        return WithReady(next);
    }

    private static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> CompleteReady(
        SpeechAssistantState state,
        SpeechAssistantSilentInputReadySpokenEvent spoken)
    {
        ArgumentNullException.ThrowIfNull(spoken);

        if (state.SilentInput is not { Phase: SpeechAssistantSilentInputPhase.Transitioning } current
            || spoken.ExpectedSurfaceRevision != state.SurfaceRevision)
        {
            return StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithoutEffects(state);
        }

        return WithPublication(
            state with
            {
                SurfaceRevision = checked(state.SurfaceRevision + 1),
                SilentInput = current with
                {
                    Phase = SpeechAssistantSilentInputPhase.Editing
                }
            });
    }

    private static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> Enter(
        SpeechAssistantState state,
        SpeechAssistantSilentInputSnapshotIdentity expected)
    {
        if (!SpeechAssistantSilentInputCommitReducer.MatchesIdentity(state, expected)
            || state.SilentInput is not { Phase: SpeechAssistantSilentInputPhase.Available } current)
        {
            return StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithoutEffects(state);
        }

        return WithReady(
            state with
            {
                SurfaceRevision = checked(state.SurfaceRevision + 1),
                SilentInput = current with { Phase = SpeechAssistantSilentInputPhase.Transitioning }
            });
    }

    private static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> FailReady(
        SpeechAssistantState state,
        SpeechAssistantSilentInputReadyPlaybackFailedEvent failed)
    {
        ArgumentNullException.ThrowIfNull(failed);
        if (state.SilentInput is not { Phase: SpeechAssistantSilentInputPhase.Transitioning } current
            || failed.ExpectedSurfaceRevision != state.SurfaceRevision)
        {
            return StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithoutEffects(state);
        }

        return WithPublication(
            state with
            {
                SurfaceRevision = checked(state.SurfaceRevision + 1),
                SilentInput = current with { Phase = SpeechAssistantSilentInputPhase.Available }
            });
    }

    private static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> WithPublication(
        SpeechAssistantState next)
        => StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
            next,
            new PublishSilentInputSurfaceEffect(next));

    private static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> WithReady(
        SpeechAssistantState next)
        => StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
            next,
            new PublishSilentInputSurfaceEffect(next),
            new SpeakSilentInputReadyEffect(next.SurfaceRevision, next.SelectedIsoCode ?? "de"));
}
