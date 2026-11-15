using Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.Infrastructure;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormStatus;

public sealed class GetDetailedFormStatusQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_EmptyForm_ReturnsSnapshotWithCurrentFocus()
    {
        var interpreter = FormDialogRuntimeTestFixture.CreateInterpreter();
        var handler = new GetDetailedFormStatusQueryHandler(
            interpreter.Services.StatusProjectionService,
            interpreter.Services.ValidationService,
            interpreter.Services.VisibilityService,
            interpreter.MutableState);

        var result = await handler.HandleAsync(new GetDetailedFormStatusQuery());

        Assert.NotNull(result);
        Assert.Contains(result.Fields, field => field.FieldId == "nameField");
    }

    [Fact]
    public async Task HandleAsync_WithoutFocusedField_ReturnsVisibleFieldStatuses()
    {
        var interpreter = FormDialogRuntimeTestFixture.CreateInterpreter();
        Assert.True(interpreter.MutableState.SetFocusedField(null));
        var handler = new GetDetailedFormStatusQueryHandler(
            interpreter.Services.StatusProjectionService,
            interpreter.Services.ValidationService,
            interpreter.Services.VisibilityService,
            interpreter.MutableState);

        var result = await handler.HandleAsync(new GetDetailedFormStatusQuery());

        Assert.Contains(result.Fields, field => field.FieldId == "nameField");
        Assert.Contains(result.Fields, field => field.FieldId == "cityField");
        Assert.Null(await interpreter.GetFocusedFieldIdAsync());
    }
}
