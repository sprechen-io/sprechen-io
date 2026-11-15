using Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.Infrastructure;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormStatus;

public sealed class ValidateAllQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_NoConstraints_ReturnsEmptyViolations()
    {
        var interpreter = FormDialogRuntimeTestFixture.CreateInterpreter();
        var handler = new ValidateAllQueryHandler(
            interpreter.Services.ValidationService,
            interpreter.Services.VisibilityService);

        var result = await handler.HandleAsync(new ValidateAllQuery());

        Assert.Empty(result);
    }
}
