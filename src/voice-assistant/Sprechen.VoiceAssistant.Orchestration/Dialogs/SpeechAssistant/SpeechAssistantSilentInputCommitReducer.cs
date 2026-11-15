using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft den Bezug stiller Eingabeaufträge, verfolgt deren laufende Transaktion und übernimmt
/// passende Ergebnisse, damit verspätete Rückmeldungen keinen neueren Bearbeitungsstand
/// überschreiben.
/// </summary>
internal static class SpeechAssistantSilentInputCommitReducer
{
    internal static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> Submit(
        SpeechAssistantState state,
        SpeechAssistantSilentInputSnapshotIdentity expected,
        string value)
    {
        if (!CanCommit(state, expected, out var current))
        {
            return StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithoutEffects(state);
        }

        return BeginTransaction(
            state,
            current,
            FormSilentInputTransactionRequest.Replace(
                state.SelectedIsoCode!,
                expected.ApplicationId,
                expected.FormId,
                expected.FieldId,
                current.ControlKind!.Value,
                value,
                SpeechAssistantApplicationSessionStateProjector.GetApplicationSession(state, expected.ApplicationId),
                SpeechAssistantApplicationSessionStateProjector.GetInitialValuesByVariableId(
                    state,
                    expected.ApplicationId,
                    expected.FormId)));
    }

    internal static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> Clear(
        SpeechAssistantState state,
        SpeechAssistantSilentInputSnapshotIdentity expected)
    {
        if (!CanCommit(state, expected, out var current))
        {
            return StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithoutEffects(state);
        }

        return BeginTransaction(
            state,
            current,
            FormSilentInputTransactionRequest.Clear(
                state.SelectedIsoCode!,
                expected.ApplicationId,
                expected.FormId,
                expected.FieldId,
                current.ControlKind!.Value,
                SpeechAssistantApplicationSessionStateProjector.GetApplicationSession(state, expected.ApplicationId),
                SpeechAssistantApplicationSessionStateProjector.GetInitialValuesByVariableId(
                    state,
                    expected.ApplicationId,
                    expected.FormId)));
    }

    internal static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> Cancel(
        SpeechAssistantState state,
        SpeechAssistantSilentInputSnapshotIdentity expected)
    {
        if (!MatchesIdentity(state, expected)
            || state.SilentInput is not { Phase: SpeechAssistantSilentInputPhase.Editing or SpeechAssistantSilentInputPhase.Repair } current)
        {
            return StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithoutEffects(state);
        }

        return WithPublication(
            state with
            {
                SurfaceRevision = checked(state.SurfaceRevision + 1),
                SilentInputOperationId = null,
                SilentInput = current with
                {
                    Phase = SpeechAssistantSilentInputPhase.Available,
                    RepairKind = null
                }
            });
    }

    internal static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> CompleteTransaction(
        SpeechAssistantState state,
        SpeechAssistantSilentInputTransactionCompletedEvent completed)
    {
        ArgumentNullException.ThrowIfNull(completed);
        if (state.SilentInput is not { Phase: SpeechAssistantSilentInputPhase.Pending } current
            || !string.Equals(state.SilentInputOperationId, completed.OperationId, StringComparison.Ordinal))
        {
            return StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithoutEffects(state);
        }

        if (completed.Result.Kind == FormSilentInputResultKind.Rejected)
        {
            return WithPublication(
                state with
                {
                    SurfaceRevision = checked(state.SurfaceRevision + 1),
                    SilentInputOperationId = null,
                    SilentInputPendingIntent = null,
                    SilentInput = current with
                    {
                        Phase = SpeechAssistantSilentInputPhase.Repair,
                        RepairKind = completed.Result.RepairKind
                    }
                });
        }

        var nextPhase = completed.Result.Eligibility is null
            ? SpeechAssistantSilentInputPhase.Unavailable
            : SpeechAssistantSilentInputPhase.Available;
        var applicationId = state.SelectedApplicationId;
        var sessions = completed.Result.Kind == FormSilentInputResultKind.Applied
            && completed.Result.ApplicationSession is not null
            && !string.IsNullOrWhiteSpace(applicationId)
            ? state.ApplicationSessions.SetItem(applicationId.Trim(), completed.Result.ApplicationSession)
            : state.ApplicationSessions;

        var publishedRevision = checked(state.SurfaceRevision + 1);
        var next = state with
        {
            ApplicationSessions = sessions,
            SurfaceRevision = publishedRevision,
            SilentInputOperationId = null,
            SilentInputPendingIntent = null,
            SilentInput = current with
            {
                Phase = nextPhase,
                FieldId = completed.Result.Eligibility?.FieldId,
                ControlKind = completed.Result.Eligibility?.ControlKind,
                RepairKind = null
            }
        };

        if (completed.Result.Kind != FormSilentInputResultKind.Applied
            || state.SilentInputPendingIntent is null
            || string.IsNullOrWhiteSpace(current.FieldId))
        {
            return WithPublication(next);
        }

        var acceptedIntent = state.SilentInputPendingIntent == FormSilentInputIntentKind.Clear
            ? FormSilentInputAcceptedIntentKind.Clear
            : completed.Result.CanonicalSame
                ? FormSilentInputAcceptedIntentKind.Same
                : FormSilentInputAcceptedIntentKind.Replace;

        return StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
            next,
            new PublishSilentInputSurfaceEffect(next),
            new DispatchSilentInputAcceptedEffect(
                new SpeechAssistantSilentInputAcceptedEvent(
                    state.CorrelationId ?? string.Empty,
                    state.SessionId ?? string.Empty,
                    current.OriginSurfaceRevision,
                    publishedRevision,
                    state.SelectedApplicationId ?? string.Empty,
                    state.SelectedFormId ?? string.Empty,
                    current.FieldId,
                    completed.OperationId,
                    acceptedIntent)));
    }

    internal static bool MatchesIdentity(
        SpeechAssistantState state,
        SpeechAssistantSilentInputSnapshotIdentity expected)
    {
        ArgumentNullException.ThrowIfNull(expected);
        return string.Equals(state.CorrelationId, expected.CorrelationId, StringComparison.Ordinal)
            && string.Equals(state.SessionId ?? string.Empty, expected.SessionId, StringComparison.Ordinal)
            && state.SurfaceRevision == expected.SurfaceRevision
            && string.Equals(state.SelectedApplicationId, expected.ApplicationId, StringComparison.Ordinal)
            && string.Equals(state.SelectedFormId, expected.FormId, StringComparison.Ordinal)
            && string.Equals(state.SilentInput?.FieldId, expected.FieldId, StringComparison.Ordinal);
    }

    private static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> BeginTransaction(
        SpeechAssistantState state,
        SpeechAssistantFormSurfaceInteractionState current,
        FormSilentInputTransactionRequest request)
    {
        var operationId = Guid.NewGuid().ToString("N");
        var next = state with
        {
            SurfaceRevision = checked(state.SurfaceRevision + 1),
            SilentInputOperationId = operationId,
            SilentInputPendingIntent = request.IntentKind,
            SilentInput = current with { Phase = SpeechAssistantSilentInputPhase.Pending }
        };
        return StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
            next,
            new PublishSilentInputSurfaceEffect(next),
            new ExecuteFormSilentInputTransactionEffect(operationId, request));
    }

    private static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> WithPublication(
        SpeechAssistantState next)
        => StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
            next,
            new PublishSilentInputSurfaceEffect(next));

    private static bool CanCommit(
        SpeechAssistantState state,
        SpeechAssistantSilentInputSnapshotIdentity expected,
        out SpeechAssistantFormSurfaceInteractionState current)
    {
        current = state.SilentInput!;
        return MatchesIdentity(state, expected)
            && state.SilentInput is { Phase: SpeechAssistantSilentInputPhase.Editing or SpeechAssistantSilentInputPhase.Repair, ControlKind: not null };
    }
}
