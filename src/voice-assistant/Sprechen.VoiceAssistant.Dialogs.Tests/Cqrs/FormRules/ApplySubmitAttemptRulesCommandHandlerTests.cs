using Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.Infrastructure;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormRules;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormRules;

public sealed class ApplySubmitAttemptRulesCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_NoRules_ReturnsEmptyPromptIds()
    {
        var interpreter = FormDialogRuntimeTestFixture.CreateInterpreter();
        var handler = new ApplySubmitAttemptRulesCommandHandler(
            interpreter.Services.RuleOrchestrationService,
            interpreter.Services.Navigator);

        var result = await handler.HandleAsync(new ApplySubmitAttemptRulesCommand());

        Assert.Empty(result);
    }
}
