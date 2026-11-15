using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormEditing.Events;
using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormEditing;

/// <summary>
/// Setzt den zugeordneten Variablenwert und hängt anschließend ein Feldwertereignis an.
/// Die Zustandsänderung erfolgt vor Abschluss des Ereignisschreibens. Erfolg wird danach gemeldet.
/// </summary>
internal sealed class SetFieldValueCommandHandler : ICommandHandler<SetFieldValueCommand, SetFieldValueResult>
{
    private readonly IEditableFormState _state;
    private readonly IEventStore _eventStore;

    public SetFieldValueCommandHandler(IEditableFormState state, IEventStore eventStore)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
    }

    public async Task<SetFieldValueResult> HandleAsync(SetFieldValueCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.SessionId);
        var fieldId = command.FieldId;

        if (!_state.IsFieldKnown(fieldId))
        {
            return new SetFieldValueResult(Success: false);
        }

        if (!_state.TryGetVariableId(fieldId, out var variableId))
        {
            return new SetFieldValueResult(Success: false);
        }

        var previousValue = _state.GetCurrentValue(variableId);
        _state.SetValue(variableId, command.Value);

        var sequenceNumber = await _eventStore.GetCurrentSequenceNumberAsync(command.SessionId, cancellationToken).ConfigureAwait(false);

        var valueEvent = new FieldValueChangedEvent
        {
            AggregateId = command.SessionId,
            SequenceNumber = sequenceNumber + 1,
            FieldId = fieldId,
            VariableId = variableId,
            PreviousValue = previousValue,
            NewValue = command.Value
        };

        await _eventStore.AppendAsync(command.SessionId, [valueEvent], cancellationToken).ConfigureAwait(false);

        return new SetFieldValueResult(Success: true, PreviousValue: previousValue, NewValue: command.Value);
    }
}


