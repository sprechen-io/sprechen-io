using Sprechen.Common.Cqrs;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;
using Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormNavigation.NavigateToFieldGroupCommandHandlerTestsSupport;
using Xunit;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation.Events;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormNavigation;

public sealed class NavigateToFieldGroupCommandHandlerTests
{
    #region Navigate To Field Group Tests

    [Fact]
    public async Task HandleAsync_ValidFieldGroup_NavigatesToFirstField()
    {
        var state = new TestFormState(
            fieldGroups: new Dictionary<string, string[]>
            {
                ["section1"] = ["field1", "field2"],
                ["section2"] = ["field3", "field4"]
            },
            currentFocus: "field1");
        var eventStore = new InMemoryEventStore();
        var handler = new NavigateToFieldGroupCommandHandler(state, eventStore);

        var result = await handler.HandleAsync(new NavigateToFieldGroupCommand("session-1", FieldGroupId.From("section2")));

        Assert.True(result.Success);
        Assert.Equal("field3", state.CurrentFocusedFieldId);
    }

    [Fact]
    public async Task HandleAsync_UnknownFieldGroup_ReturnsFalse()
    {
        var state = new TestFormState(
            fieldGroups: new Dictionary<string, string[]>
            {
                ["section1"] = ["field1"]
            },
            currentFocus: "field1");
        var eventStore = new InMemoryEventStore();
        var handler = new NavigateToFieldGroupCommandHandler(state, eventStore);

        var result = await handler.HandleAsync(new NavigateToFieldGroupCommand("session-1", FieldGroupId.From("unknown")));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task HandleAsync_InvisibleFieldGroup_ReturnsFalse()
    {
        var state = new TestFormState(
            fieldGroups: new Dictionary<string, string[]>
            {
                ["section1"] = ["field1"],
                ["section2"] = ["field2"]
            },
            currentFocus: "field1",
            invisibleFieldGroups: ["section2"]);
        var eventStore = new InMemoryEventStore();
        var handler = new NavigateToFieldGroupCommandHandler(state, eventStore);

        var result = await handler.HandleAsync(new NavigateToFieldGroupCommand("session-1", FieldGroupId.From("section2")));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task HandleAsync_SkipsInvisibleFieldsInFieldGroup()
    {
        var state = new TestFormState(
            fieldGroups: new Dictionary<string, string[]>
            {
                ["section1"] = ["field1"],
                ["section2"] = ["field2", "field3"]
            },
            currentFocus: "field1",
            invisibleFields: ["field2"]);
        var eventStore = new InMemoryEventStore();
        var handler = new NavigateToFieldGroupCommandHandler(state, eventStore);

        var result = await handler.HandleAsync(new NavigateToFieldGroupCommand("session-1", FieldGroupId.From("section2")));

        Assert.True(result.Success);
        Assert.Equal("field3", state.CurrentFocusedFieldId);
    }

    [Fact]
    public async Task HandleAsync_Success_RecordsFieldGroupNavigatedEvent()
    {
        var state = new TestFormState(
            fieldGroups: new Dictionary<string, string[]>
            {
                ["section1"] = ["field1"],
                ["section2"] = ["field2"]
            },
            currentFocus: "field1");
        var eventStore = new InMemoryEventStore();
        var handler = new NavigateToFieldGroupCommandHandler(state, eventStore);

        await handler.HandleAsync(new NavigateToFieldGroupCommand("session-1", FieldGroupId.From("section2")));

        var events = await eventStore.GetEventsAsync("session-1");
        Assert.Single(events);
        var fieldGroupEvent = Assert.IsType<FieldGroupNavigatedEvent>(events[0]);
        Assert.Equal("section1", fieldGroupEvent.PreviousFieldGroupId);
        Assert.Equal("section2", fieldGroupEvent.NewFieldGroupId);
    }

    [Fact]
    public async Task HandleAsync_AllFieldsInvisible_ReturnsFalse()
    {
        var state = new TestFormState(
            fieldGroups: new Dictionary<string, string[]>
            {
                ["section1"] = ["field1"],
                ["section2"] = ["field2", "field3"]
            },
            currentFocus: "field1",
            invisibleFields: ["field2", "field3"]);
        var eventStore = new InMemoryEventStore();
        var handler = new NavigateToFieldGroupCommandHandler(state, eventStore);

        var result = await handler.HandleAsync(new NavigateToFieldGroupCommand("session-1", FieldGroupId.From("section2")));

        Assert.False(result.Success);
    }

    #endregion

}




