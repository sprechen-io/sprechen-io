using Sprechen.Common.Cqrs;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;
using Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormStatus.GetFormStatusQueryHandlerTestsSupport;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormStatus;

public sealed class GetFormStatusQueryHandlerTests
{
    #region GetStatus Tests

    [Fact]
    public async Task HandleAsync_WithFocus_ReturnsStatus()
    {
        var state = new TestFormState(
            currentFieldId: "field2",
            currentFieldGroupId: "section1",
            totalFields: 5,
            totalFieldGroups: 2,
            openRequiredCount: 3,
            validationErrorCount: 1);
        var handler = new GetFormStatusQueryHandler(state);

        var result = await handler.HandleAsync(new GetFormStatusQuery());

        Assert.Equal("field2", result.CurrentFieldId);
        Assert.Equal("section1", result.CurrentFieldGroupId);
        Assert.Equal(5, result.TotalFields);
        Assert.Equal(2, result.TotalFieldGroups);
        Assert.Equal(3, result.OpenRequiredCount);
        Assert.Equal(1, result.ValidationErrorCount);
    }

    [Fact]
    public async Task HandleAsync_NoFocus_ReturnsNullFieldId()
    {
        var state = new TestFormState(
            currentFieldId: null,
            currentFieldGroupId: null,
            totalFields: 3,
            totalFieldGroups: 1,
            openRequiredCount: 0,
            validationErrorCount: 0);
        var handler = new GetFormStatusQueryHandler(state);

        var result = await handler.HandleAsync(new GetFormStatusQuery());

        Assert.Null(result.CurrentFieldId);
        Assert.Null(result.CurrentFieldGroupId);
    }

    [Fact]
    public async Task HandleAsync_IncludesFieldIndex()
    {
        var state = new TestFormState(
            currentFieldId: "field3",
            currentFieldGroupId: "section1",
            totalFields: 5,
            totalFieldGroups: 2,
            openRequiredCount: 0,
            validationErrorCount: 0,
            currentFieldIndex: 2);
        var handler = new GetFormStatusQueryHandler(state);

        var result = await handler.HandleAsync(new GetFormStatusQuery());

        Assert.Equal(2, result.CurrentFieldIndex);
    }

    [Fact]
    public async Task HandleAsync_IncludesFieldGroupIndex()
    {
        var state = new TestFormState(
            currentFieldId: "field1",
            currentFieldGroupId: "section2",
            totalFields: 5,
            totalFieldGroups: 3,
            openRequiredCount: 0,
            validationErrorCount: 0,
            currentFieldGroupIndex: 1);
        var handler = new GetFormStatusQueryHandler(state);

        var result = await handler.HandleAsync(new GetFormStatusQuery());

        Assert.Equal(1, result.CurrentFieldGroupIndex);
    }

    #endregion

}



