using Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.Infrastructure;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormNavigation;

public sealed class FocusNextErrorCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_NoConstraints_ReturnsNull()
    {
        var interpreter = FormDialogRuntimeTestFixture.CreateInterpreter();
        var handler = new FocusNextErrorCommandHandler(
            interpreter.ValidationErrorFocusService,
            interpreter.Services.ValidationService,
            interpreter.Services.VisibilityService,
            interpreter.Services.Navigator,
            interpreter.MutableState);

        var result = await handler.HandleAsync(new FocusNextErrorCommand());

        Assert.Null(result);
    }
}
