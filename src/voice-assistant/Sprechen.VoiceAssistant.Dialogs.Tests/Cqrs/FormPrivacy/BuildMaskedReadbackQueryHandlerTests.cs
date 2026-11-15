using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormPrivacy;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormPrivacy;

public sealed class BuildMaskedReadbackQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_UnknownVariable_FailsClosed()
    {
        var handler = new BuildMaskedReadbackQueryHandler(FormPrivacyTestFixture.CreateEmptyPolicy());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(new BuildMaskedReadbackQuery("unknown", "  some-value  ", "en")));
    }

    [Fact]
    public async Task HandleAsync_KnownPublicVariable_ReturnsResolvedReadback()
    {
        var handler = new BuildMaskedReadbackQueryHandler(FormPrivacyTestFixture.CreatePolicyWithVariables());

        var result = await handler.HandleAsync(new BuildMaskedReadbackQuery("publicVar", "12345", "en"));

        Assert.NotNull(result);
    }
}
