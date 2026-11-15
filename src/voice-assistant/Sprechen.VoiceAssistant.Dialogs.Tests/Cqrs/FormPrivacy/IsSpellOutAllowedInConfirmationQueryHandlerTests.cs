using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormPrivacy;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormPrivacy;

public sealed class IsSpellOutAllowedInConfirmationQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_PublicVariable_AllowsSpellOut()
    {
        var handler = new IsSpellOutAllowedInConfirmationQueryHandler(FormPrivacyTestFixture.CreatePolicyWithVariables());

        var result = await handler.HandleAsync(new IsSpellOutAllowedInConfirmationQuery("publicVar"));

        Assert.True(result);
    }

    [Fact]
    public async Task HandleAsync_SensitiveVariable_DisallowsSpellOut()
    {
        var handler = new IsSpellOutAllowedInConfirmationQueryHandler(FormPrivacyTestFixture.CreatePolicyWithVariables());

        var result = await handler.HandleAsync(new IsSpellOutAllowedInConfirmationQuery("sensitiveVar"));

        Assert.False(result);
    }

    [Fact]
    public async Task HandleAsync_UnknownVariable_FailsClosed()
    {
        var handler = new IsSpellOutAllowedInConfirmationQueryHandler(FormPrivacyTestFixture.CreateEmptyPolicy());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(new IsSpellOutAllowedInConfirmationQuery("unknown")));
    }
}
