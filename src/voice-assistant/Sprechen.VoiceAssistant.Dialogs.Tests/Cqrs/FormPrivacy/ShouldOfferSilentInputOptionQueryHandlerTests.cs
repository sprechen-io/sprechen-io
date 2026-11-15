using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormPrivacy;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormPrivacy;

public sealed class ShouldOfferSilentInputOptionQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_SensitiveVariable_OffersSilentInput()
    {
        var handler = new ShouldOfferSilentInputOptionQueryHandler(FormPrivacyTestFixture.CreatePolicyWithVariables());

        var result = await handler.HandleAsync(new ShouldOfferSilentInputOptionQuery("sensitiveVar"));

        Assert.True(result.Allowed);
    }

    [Fact]
    public async Task HandleAsync_PublicVariable_DoesNotOfferSilentInput()
    {
        var handler = new ShouldOfferSilentInputOptionQueryHandler(FormPrivacyTestFixture.CreatePolicyWithVariables());

        var result = await handler.HandleAsync(new ShouldOfferSilentInputOptionQuery("publicVar"));

        Assert.False(result.Allowed);
    }
}
