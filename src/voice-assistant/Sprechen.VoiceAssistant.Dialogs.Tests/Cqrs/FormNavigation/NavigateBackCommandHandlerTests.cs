using Sprechen.Common.Cqrs;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;
using Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormNavigation.NavigateBackCommandHandlerTestsSupport;
using Xunit;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation.Events;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormNavigation;

public sealed class NavigateBackCommandHandlerTests
{
    #region Navigate Back Tests

    [Fact]
    public async Task HandleAsync_WithAvailablePreviousField_MovesBack()
    {
        var state = new TestFormState(["field1", "field2", "field3"], currentFocus: "field3");
        var eventStore = new InMemoryEventStore();
        var handler = new NavigateBackCommandHandler(state, eventStore);

        var result = await handler.HandleAsync(new NavigateBackCommand("session-1"));

        Assert.True(result.Success);
        Assert.Equal("field2", state.CurrentFocusedFieldId);
    }

    [Fact]
    public async Task HandleAsync_AtFirstField_ReturnsFalse()
    {
        var state = new TestFormState(["field1", "field2"], currentFocus: "field1");
        var eventStore = new InMemoryEventStore();
        var handler = new NavigateBackCommandHandler(state, eventStore);

        var result = await handler.HandleAsync(new NavigateBackCommand("session-1"));

        Assert.False(result.Success);
        Assert.Equal("field1", state.CurrentFocusedFieldId);
    }

    [Fact]
    public async Task HandleAsync_NoFocus_ReturnsFalse()
    {
        var state = new TestFormState(["field1", "field2"], currentFocus: null);
        var eventStore = new InMemoryEventStore();
        var handler = new NavigateBackCommandHandler(state, eventStore);

        var result = await handler.HandleAsync(new NavigateBackCommand("session-1"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task HandleAsync_SkipsInvisibleFields()
    {
        var state = new TestFormState(["field1", "field2", "field3"], currentFocus: "field3", invisibleFields: ["field2"]);
        var eventStore = new InMemoryEventStore();
        var handler = new NavigateBackCommandHandler(state, eventStore);

        var result = await handler.HandleAsync(new NavigateBackCommand("session-1"));

        Assert.True(result.Success);
        Assert.Equal("field1", state.CurrentFocusedFieldId);
    }

    [Fact]
    public async Task HandleAsync_Success_RecordsFocusMovedEvent()
    {
        var state = new TestFormState(["field1", "field2"], currentFocus: "field2");
        var eventStore = new InMemoryEventStore();
        var handler = new NavigateBackCommandHandler(state, eventStore);

        await handler.HandleAsync(new NavigateBackCommand("session-1"));

        var events = await eventStore.GetEventsAsync("session-1");
        Assert.Single(events);
        var focusEvent = Assert.IsType<FocusMovedEvent>(events[0]);
        Assert.Equal("field2", focusEvent.PreviousFieldId);
        Assert.Equal("field1", focusEvent.NewFieldId);
        Assert.Equal(FocusMovedReason.Back, focusEvent.Reason);
    }

    #endregion

}



