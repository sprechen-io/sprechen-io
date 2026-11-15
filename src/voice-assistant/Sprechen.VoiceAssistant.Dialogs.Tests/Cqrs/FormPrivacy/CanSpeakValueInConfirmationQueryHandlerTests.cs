using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormPrivacy;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormPrivacy;

public sealed class CanSpeakValueInConfirmationQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_PublicVariable_CanSpeakRegardlessOfOptIn()
    {
        var handler = new CanSpeakValueInConfirmationQueryHandler(FormPrivacyTestFixture.CreatePolicyWithVariables());

        var result = await handler.HandleAsync(new CanSpeakValueInConfirmationQuery("publicVar", ExplicitOptIn: false));

        Assert.True(result);
    }

    [Fact]
    public async Task HandleAsync_SecretVariable_NeverSpeaks()
    {
        var handler = new CanSpeakValueInConfirmationQueryHandler(FormPrivacyTestFixture.CreatePolicyWithVariables());

        var result = await handler.HandleAsync(new CanSpeakValueInConfirmationQuery("secretVar", ExplicitOptIn: true));

        Assert.False(result);
    }

    [Fact]
    public async Task HandleAsync_SensitiveVariable_RequiresExplicitOptIn()
    {
        var handler = new CanSpeakValueInConfirmationQueryHandler(FormPrivacyTestFixture.CreatePolicyWithVariables());

        var resultWithoutOptIn = await handler.HandleAsync(new CanSpeakValueInConfirmationQuery("sensitiveVar", ExplicitOptIn: false));
        var resultWithOptIn = await handler.HandleAsync(new CanSpeakValueInConfirmationQuery("sensitiveVar", ExplicitOptIn: true));

        Assert.False(resultWithoutOptIn);
        Assert.True(resultWithOptIn);
    }
}
