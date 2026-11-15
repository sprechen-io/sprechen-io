using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormPrivacy;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormPrivacy;

public sealed class IsVoiceInputAllowedQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_PublicVariable_AllowsVoiceInput()
    {
        var handler = new IsVoiceInputAllowedQueryHandler(FormPrivacyTestFixture.CreatePolicyWithVariables());

        var result = await handler.HandleAsync(new IsVoiceInputAllowedQuery("publicVar"));

        Assert.True(result.Allowed);
        Assert.False(string.IsNullOrEmpty(result.SilentInputFallback));
    }

    [Fact]
    public async Task HandleAsync_SecretVariable_ForbidsVoiceInput()
    {
        var handler = new IsVoiceInputAllowedQueryHandler(FormPrivacyTestFixture.CreatePolicyWithVariables());

        var result = await handler.HandleAsync(new IsVoiceInputAllowedQuery("secretVar"));

        Assert.False(result.Allowed);
    }

    [Fact]
    public async Task HandleAsync_UnknownVariable_FailsClosed()
    {
        var handler = new IsVoiceInputAllowedQueryHandler(FormPrivacyTestFixture.CreateEmptyPolicy());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(new IsVoiceInputAllowedQuery("unknown")));
    }
}
