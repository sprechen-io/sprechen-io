using Sprechen.Common.Cqrs;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormEditing;
using Xunit;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormEditing.Events;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormEditing;

public sealed class SetFieldValueCommandHandlerTests
{
    #region SetValue Tests

    [Fact]
    public async Task HandleAsync_ValidField_SetsValue()
    {
        var state = new TestFormState(["field1"]);
        state.SetFieldMapping("field1", "var1");
        var eventStore = new InMemoryEventStore();
        var handler = new SetFieldValueCommandHandler(state, eventStore);

        var result = await handler.HandleAsync(new SetFieldValueCommand("session-1", FieldId.From("field1"), "new-value"));

        Assert.True(result.Success);
        Assert.Equal("new-value", state.GetValue("var1"));
    }

    [Fact]
    public async Task HandleAsync_UnknownField_ReturnsFalse()
    {
        var state = new TestFormState(["field1"]);
        var eventStore = new InMemoryEventStore();
        var handler = new SetFieldValueCommandHandler(state, eventStore);

        var result = await handler.HandleAsync(new SetFieldValueCommand("session-1", FieldId.From("unknown"), "value"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task HandleAsync_RecordsPreviousValue()
    {
        var state = new TestFormState(["field1"]);
        state.SetFieldMapping("field1", "var1");
        state.SetValueDirect("var1", "old-value");
        var eventStore = new InMemoryEventStore();
        var handler = new SetFieldValueCommandHandler(state, eventStore);

        var result = await handler.HandleAsync(new SetFieldValueCommand("session-1", FieldId.From("field1"), "new-value"));

        Assert.True(result.Success);
        Assert.Equal("old-value", result.PreviousValue);
        Assert.Equal("new-value", result.NewValue);
    }

    [Fact]
    public async Task HandleAsync_Success_RecordsFieldValueChangedEvent()
    {
        var state = new TestFormState(["field1"]);
        state.SetFieldMapping("field1", "var1");
        state.SetValueDirect("var1", "old-value");
        var eventStore = new InMemoryEventStore();
        var handler = new SetFieldValueCommandHandler(state, eventStore);

        await handler.HandleAsync(new SetFieldValueCommand("session-1", FieldId.From("field1"), "new-value"));

        var events = await eventStore.GetEventsAsync("session-1");
        Assert.Single(events);
        var valueEvent = Assert.IsType<FieldValueChangedEvent>(events[0]);
        Assert.Equal("field1", valueEvent.FieldId);
        Assert.Equal("var1", valueEvent.VariableId);
        Assert.Equal("old-value", valueEvent.PreviousValue);
        Assert.Equal("new-value", valueEvent.NewValue);
    }

    [Fact]
    public async Task HandleAsync_Failure_DoesNotRecordEvent()
    {
        var state = new TestFormState(["field1"]);
        var eventStore = new InMemoryEventStore();
        var handler = new SetFieldValueCommandHandler(state, eventStore);

        await handler.HandleAsync(new SetFieldValueCommand("session-1", FieldId.From("unknown"), "value"));

        var events = await eventStore.GetEventsAsync("session-1");
        Assert.Empty(events);
    }

    [Fact]
    public async Task HandleAsync_EmptyValue_SetsEmpty()
    {
        var state = new TestFormState(["field1"]);
        state.SetFieldMapping("field1", "var1");
        state.SetValueDirect("var1", "old-value");
        var eventStore = new InMemoryEventStore();
        var handler = new SetFieldValueCommandHandler(state, eventStore);

        var result = await handler.HandleAsync(new SetFieldValueCommand("session-1", FieldId.From("field1"), ""));

        Assert.True(result.Success);
        Assert.Equal("", state.GetValue("var1"));
    }

    #endregion

}



