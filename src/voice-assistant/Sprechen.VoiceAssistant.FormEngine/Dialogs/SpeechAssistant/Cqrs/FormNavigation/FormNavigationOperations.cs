using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation.Events;
using Sprechen.Common.Cqrs;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;

/// <summary>
/// Führt benachbarte Feldwechsel und Gruppensprünge aus und hängt bei erfolgreicher Navigation die zugehörigen Ereignisse an. Die Fokusänderung erfolgt vor Abschluss des Ereignisschreibens.
/// </summary>
internal static class FormNavigationOperations
{
    public static Task<NavigationResult> MoveNextAsync(
        INavigableFormState state,
        IEventStore eventStore,
        string sessionId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        return MoveAdjacentAsync(
            state,
            eventStore,
            eventStore.GetCurrentSequenceNumberAsync(sessionId, cancellationToken),
            sessionId,
            moveBack: false,
            reason: FocusMovedReason.Next,
            cancellationToken: cancellationToken);
    }

    public static Task<NavigationResult> MoveBackAsync(
        INavigableFormState state,
        IEventStore eventStore,
        string sessionId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        return MoveAdjacentAsync(
            state,
            eventStore,
            eventStore.GetCurrentSequenceNumberAsync(sessionId, cancellationToken),
            sessionId,
            moveBack: true,
            reason: FocusMovedReason.Back,
            cancellationToken: cancellationToken);
    }

    public static Task<NavigationResult> NavigateToFieldGroupAsync(
        IFieldGroupNavigableFormState state,
        IEventStore eventStore,
        string sessionId,
        FieldGroupId fieldGroupId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        return NavigateToFieldGroupCoreAsync(
            state,
            eventStore.GetCurrentSequenceNumberAsync(sessionId, cancellationToken),
            sessionId,
            fieldGroupId,
            eventStore.AppendAsync,
            cancellationToken);
    }

    private static async Task<NavigationResult> MoveAdjacentAsync(
        INavigableFormState state,
        IEventStore eventStore,
        Task<long> getSequenceNumber,
        string sessionId,
        bool moveBack,
        FocusMovedReason reason,
        CancellationToken cancellationToken)
    {
        if (!TryMoveAdjacent(state, moveBack, out var previousFieldId, out var targetFieldId))
        {
            return new NavigationResult(Success: false);
        }

        var sequenceNumber = await getSequenceNumber.ConfigureAwait(false);
        var focusEvent = new FocusMovedEvent
        {
            AggregateId = sessionId,
            SequenceNumber = sequenceNumber + 1,
            PreviousFieldId = previousFieldId,
            NewFieldId = targetFieldId!.Value,
            Reason = reason
        };

        await eventStore.AppendAsync(sessionId, [focusEvent], cancellationToken).ConfigureAwait(false);
        return new NavigationResult(Success: true, PreviousFieldId: previousFieldId, NewFieldId: targetFieldId);
    }

    private static async Task<NavigationResult> NavigateToFieldGroupCoreAsync(
        IFieldGroupNavigableFormState state,
        Task<long> getSequenceNumber,
        string sessionId,
        FieldGroupId fieldGroupId,
        Func<string, IEnumerable<IDomainEvent>, CancellationToken, Task> appendAsync,
        CancellationToken cancellationToken)
    {
        var previousFieldGroupId = state.GetCurrentFieldGroupId();
        if (!TryMoveToFieldGroup(state, fieldGroupId, out var previousFieldId, out var targetFieldId))
        {
            return new NavigationResult(Success: false);
        }

        var sequenceNumber = await getSequenceNumber.ConfigureAwait(false);
        var fieldGroupEvent = new FieldGroupNavigatedEvent
        {
            AggregateId = sessionId,
            SequenceNumber = sequenceNumber + 1,
            PreviousFieldGroupId = previousFieldGroupId,
            NewFieldGroupId = fieldGroupId
        };

        await appendAsync(sessionId, [fieldGroupEvent], cancellationToken).ConfigureAwait(false);
        return new NavigationResult(Success: true, PreviousFieldId: previousFieldId, NewFieldId: targetFieldId);
    }

    private static bool TryMoveAdjacent(
        INavigableFormState state,
        bool moveBack,
        out FieldId? previousFieldId,
        out FieldId? targetFieldId)
    {
        if (moveBack)
        {
            return FocusNavigationTransitions.TryMoveBack(
                state.CurrentFocusedFieldId,
                state.OrderedFieldIds,
                state.IsFieldInputAvailable,
                state.TryGetFieldIndex,
                state.SetCurrentFocus,
                out previousFieldId,
                out targetFieldId);
        }

        return FocusNavigationTransitions.TryMoveNext(
            state.CurrentFocusedFieldId,
            state.OrderedFieldIds,
            state.IsFieldInputAvailable,
            state.TryGetFieldIndex,
            state.SetCurrentFocus,
            out previousFieldId,
            out targetFieldId);
    }

    private static bool TryMoveToFieldGroup(
        IFieldGroupNavigableFormState state,
        FieldGroupId fieldGroupId,
        out FieldId? previousFieldId,
        out FieldId? targetFieldId)
    {
        previousFieldId = state.CurrentFocusedFieldId;

        if (!state.IsFieldGroupKnown(fieldGroupId))
        {
            targetFieldId = null;
            return false;
        }

        if (!state.IsFieldGroupVisible(fieldGroupId))
        {
            targetFieldId = null;
            return false;
        }

        FieldId? candidate = null;
        foreach (var fieldId in state.GetFieldsInFieldGroup(fieldGroupId))
        {
            var typedFieldId = FieldId.From(fieldId);
            if (state.IsFieldInputAvailable(typedFieldId))
            {
                candidate = typedFieldId;
                break;
            }
        }

        if (candidate is not { } target)
        {
            targetFieldId = null;
            return false;
        }

        targetFieldId = target;
        state.SetCurrentFocus(target);
        return true;
    }
}
