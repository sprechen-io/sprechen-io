using Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.Infrastructure;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormNavigation;

public sealed class JumpToTargetCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ResolvesFieldLabel_FocusesField()
    {
        var interpreter = FormDialogRuntimeTestFixture.CreateInterpreter();
        var handler = new JumpToTargetCommandHandler(
            interpreter.Services.JumpTargetResolver,
            interpreter.Services.Navigator);

        var result = await handler.HandleAsync(new JumpToTargetCommand(FormJumpTarget.From("Stadt")));

        Assert.Equal(FormJumpExecutionKind.Focused, result.Kind);
        Assert.Equal("cityField", (await interpreter.GetFocusedFieldAsync()).FieldId);
    }

    [Fact]
    public async Task HandleAsync_UnknownTarget_ReturnsNotFound()
    {
        var interpreter = FormDialogRuntimeTestFixture.CreateInterpreter();
        var handler = new JumpToTargetCommandHandler(
            interpreter.Services.JumpTargetResolver,
            interpreter.Services.Navigator);

        var result = await handler.HandleAsync(new JumpToTargetCommand(FormJumpTarget.From("totally not a target")));

        Assert.Equal(FormJumpExecutionKind.NotFound, result.Kind);
    }

    [Fact]
    public async Task HandleAsync_BlankTarget_ReturnsNotFound()
    {
        var interpreter = FormDialogRuntimeTestFixture.CreateInterpreter();
        var handler = new JumpToTargetCommandHandler(
            interpreter.Services.JumpTargetResolver,
            interpreter.Services.Navigator);

        var result = await handler.HandleAsync(new JumpToTargetCommand(FormJumpTarget.From("   ")));

        Assert.Equal(FormJumpExecutionKind.NotFound, result.Kind);
    }
}
