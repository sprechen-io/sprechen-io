using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class WakeWordProviderTests
{
    [Fact]
    public async Task GetWakeWordInventoryAsync_WhenOverridePresent_ReturnsCanonicalOnlyOverrideInventory()
    {
        var voiceSettings = new Mock<IVoiceActivationSettings>(MockBehavior.Strict);
        voiceSettings
            .Setup(settings => settings.GetWakeWordActivationFormsOverrideAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string[]>(new[] { "alpha", "beta" }));

        var provider = new WakeWordProvider(
            voiceSettings.Object,
            NullLogger<WakeWordProvider>.Instance);

        var result = await provider.GetWakeWordInventoryAsync("de", CancellationToken.None);

        Assert.Equal(["alpha", "beta"], result.ActivationForms);
        Assert.Empty(result.RecognitionOnlyActivationForms);
    }

    [Fact]
    public async Task GetWakeWordInventoryAsync_WhenOverrideLoadFails_FallsBackToModelWakeWords()
    {
        var voiceSettings = new Mock<IVoiceActivationSettings>(MockBehavior.Strict);
        voiceSettings
            .Setup(settings => settings.GetWakeWordActivationFormsOverrideAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromException<string[]>(new InvalidOperationException("boom")));

        var provider = new WakeWordProvider(
            voiceSettings.Object,
            NullLogger<WakeWordProvider>.Instance);

        var result = await provider.GetWakeWordInventoryAsync("de", CancellationToken.None);

        Assert.Equal(VoiceInterfaceModelCatalog.WakeWord.GetActivationFormsForLanguage("de"), result.ActivationForms);
    }

    [Fact]
    public async Task GetWakeWordInventoryAsync_WhenOverrideMissing_ReturnsModeledInventories()
    {
        var voiceSettings = new Mock<IVoiceActivationSettings>(MockBehavior.Strict);
        voiceSettings
            .Setup(settings => settings.GetWakeWordActivationFormsOverrideAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string[]>(Array.Empty<string>()));

        var provider = new WakeWordProvider(
            voiceSettings.Object,
            NullLogger<WakeWordProvider>.Instance);

        var result = await provider.GetWakeWordInventoryAsync("de", CancellationToken.None);

        Assert.Equal(VoiceInterfaceModelCatalog.WakeWord.GetActivationFormsForLanguage("de"), result.ActivationForms);
        Assert.Equal(
            VoiceInterfaceModelCatalog.WakeWord.GetRecognitionOnlyActivationFormsForLanguage("de"),
            result.RecognitionOnlyActivationForms);
    }

    [Fact]
    public async Task GetWakeWordInventoryAsync_WhenOverrideMissing_ReturnsModelFallback()
    {
        var voiceSettings = new Mock<IVoiceActivationSettings>(MockBehavior.Strict);
        voiceSettings
            .Setup(settings => settings.GetWakeWordActivationFormsOverrideAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string[]>(Array.Empty<string>()));

        var provider = new WakeWordProvider(
            voiceSettings.Object,
            NullLogger<WakeWordProvider>.Instance);

        var result = await provider.GetWakeWordInventoryAsync("de", CancellationToken.None);

        Assert.Equal(VoiceInterfaceModelCatalog.WakeWord.GetActivationFormsForLanguage("de"), result.ActivationForms);
    }

    [Fact]
    public async Task GetWakeWordInventoryAsync_WhenRequestedLanguageUnsupported_FallsBackToModeledDefaultLanguage()
    {
        var voiceSettings = new Mock<IVoiceActivationSettings>(MockBehavior.Strict);
        voiceSettings
            .Setup(settings => settings.GetWakeWordActivationFormsOverrideAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string[]>(Array.Empty<string>()));

        var provider = new WakeWordProvider(
            voiceSettings.Object,
            NullLogger<WakeWordProvider>.Instance);

        var result = await provider.GetWakeWordInventoryAsync("zz", CancellationToken.None);

        Assert.Equal(VoiceInterfaceModelCatalog.WakeWord.GetActivationFormsForLanguage(VoiceInterfaceModelCatalog.DefaultLanguage), result.ActivationForms);
    }
}
