using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormPrivacy;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormPrivacy;

public sealed class RequiresReadbackOptInQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_SensitiveVariable_RequiresReadbackOptIn()
    {
        var handler = new RequiresReadbackOptInQueryHandler(FormPrivacyTestFixture.CreatePolicyWithVariables());

        var result = await handler.HandleAsync(new RequiresReadbackOptInQuery("sensitiveVar"));

        Assert.True(result);
    }

    [Fact]
    public async Task HandleAsync_PublicVariable_DoesNotRequireOptIn()
    {
        var handler = new RequiresReadbackOptInQueryHandler(FormPrivacyTestFixture.CreatePolicyWithVariables());

        var result = await handler.HandleAsync(new RequiresReadbackOptInQuery("publicVar"));

        Assert.False(result);
    }
}
