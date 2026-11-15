using Bunit;
using Microsoft.AspNetCore.Components;
using Moq;
using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;
using Sprechen.Web.SpeechComponents;
using Sprechen.Voice.App.Tests.TestDoubles;

namespace Sprechen.Voice.App.Tests.Components;

public sealed class AudioSettingsTabTests : TestContext
{
    [Fact]
    public void WhenVolumeChanges_UpdatesAudioPlayers()
    {
        var audioSettings = new RecordingAudioOutputSettings { Volume = 0.4f };
        Services.AddSingleton<IAudioOutputSettings>(audioSettings);
        Services.AddSingleton(Mock.Of<ITextToSpeechPlayer>());
        Services.AddSingleton(CreateSupportedLanguagesClient("de"));

        var cut = Render<AudioSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            var input = cut.Find("#audio-volume");
            Assert.Equal("40", input.GetAttribute("value"));
            Assert.False(input.HasAttribute("disabled"));

            var label = cut.Find("label[for='audio-volume']");
            Assert.Contains("Volume", label.TextContent, StringComparison.Ordinal);
            Assert.Contains("40%", label.TextContent, StringComparison.Ordinal);

            var ticks = cut.Find("div.d-flex.justify-content-between.text-body-secondary.small[aria-hidden='true']");
            Assert.Contains("0%", ticks.TextContent, StringComparison.Ordinal);
            Assert.Contains("50%", ticks.TextContent, StringComparison.Ordinal);
            Assert.Contains("100%", ticks.TextContent, StringComparison.Ordinal);
        });

        cut.Find("#audio-volume").TriggerEvent("oninput", new ChangeEventArgs { Value = "50" });

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(0.5f, audioSettings.SetVolumes);
        });
    }

    [Fact]
    public void WhenGetVolumeThrows_DefaultsTo100Percent()
    {
        Services.AddSingleton<IAudioOutputSettings>(new RecordingAudioOutputSettings
        {
            GetVolumeException = new InvalidOperationException("boom"),
        });
        Services.AddSingleton(Mock.Of<ITextToSpeechPlayer>());
        Services.AddSingleton(CreateSupportedLanguagesClient("de"));

        var cut = Render<AudioSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            var input = cut.Find("#audio-volume");
            Assert.Equal("100", input.GetAttribute("value"));

            Assert.Contains("boom", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void WhenVolumeInputIsInvalid_DoesNotInvokeSetVolume()
    {
        var audioSettings = new RecordingAudioOutputSettings { Volume = 0.4f };
        Services.AddSingleton<IAudioOutputSettings>(audioSettings);
        Services.AddSingleton(Mock.Of<ITextToSpeechPlayer>());
        Services.AddSingleton(CreateSupportedLanguagesClient("de"));

        var cut = Render<AudioSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            var input = cut.Find("#audio-volume");
            Assert.Equal("40", input.GetAttribute("value"));
        });

        cut.Find("#audio-volume").TriggerEvent("oninput", new ChangeEventArgs { Value = "not-a-number" });

        cut.WaitForAssertion(() =>
        {
            var label = cut.Find("label[for='audio-volume']");
            Assert.Contains("40%", label.TextContent, StringComparison.Ordinal);
            Assert.Empty(audioSettings.SetVolumes);
        });
    }

    [Fact]
    public void WhenSetVolumeThrows_ShowsError()
    {
        Services.AddSingleton<IAudioOutputSettings>(new RecordingAudioOutputSettings
        {
            Volume = 0.4f,
            SetVolumeException = new InvalidOperationException("boom"),
        });
        Services.AddSingleton(Mock.Of<ITextToSpeechPlayer>());
        Services.AddSingleton(CreateSupportedLanguagesClient("de"));

        var cut = Render<AudioSettingsTab>();

        cut.Find("#audio-volume").TriggerEvent("oninput", new ChangeEventArgs { Value = "50" });

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("boom", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void WhenVolumeTestStarts_InvokesTextToSpeech()
    {
        Services.AddSingleton<IAudioOutputSettings>(new RecordingAudioOutputSettings { Volume = 0.4f });
        Services.AddSingleton(CreateSupportedLanguagesClient("de"));

        var tts = new Mock<ITextToSpeechPlayer>();
        tts.Setup(p => p.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string _, string __, CancellationToken token) => Task.Delay(TimeSpan.FromDays(1), token));
        Services.AddSingleton(tts.Object);

        var cut = Render<AudioSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            var button = cut.Find("#audio-volume-test");
            Assert.Contains("Start volume test", button.TextContent, StringComparison.Ordinal);
        });

        cut.Find("#audio-volume-test").Click();

        cut.WaitForAssertion(() =>
        {
            var button = cut.Find("#audio-volume-test");
            Assert.Contains("Stop volume test", button.TextContent, StringComparison.Ordinal);
        });

        cut.WaitForAssertion(() =>
        {
            tts.Verify(p => p.SpeakAsync(It.IsAny<string>(), "de", It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        });

        cut.Find("#audio-volume-test").Click();

        cut.WaitForAssertion(() =>
        {
            var button = cut.Find("#audio-volume-test");
            Assert.Contains("Start volume test", button.TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void WhenVolumeTestSupportedLanguagesMissing_ShowsErrorAndStopsTest()
    {
        Services.AddSingleton<IAudioOutputSettings>(new RecordingAudioOutputSettings { Volume = 0.5f });
        Services.AddSingleton(CreateSupportedLanguagesClient());
        Services.AddSingleton(Mock.Of<ITextToSpeechPlayer>());

        var cut = Render<AudioSettingsTab>();

        cut.Find("#audio-volume-test").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No supported languages", cut.Markup, StringComparison.OrdinalIgnoreCase);

            var button = cut.Find("#audio-volume-test");
            Assert.Contains("Start volume test", button.TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void WhenVolumeTestLanguageIsUnsupported_UsesDefaultModeledText()
    {
        Services.AddSingleton<IAudioOutputSettings>(new RecordingAudioOutputSettings { Volume = 0.5f });
        Services.AddSingleton(CreateSupportedLanguagesClient("pt"));

        var tts = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        tts.Setup(player => player.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string _, string __, CancellationToken token) => Task.Delay(TimeSpan.FromDays(1), token));
        tts.Setup(player => player.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns((string _, string __, double ___, CancellationToken token) => Task.Delay(TimeSpan.FromDays(1), token));
        Services.AddSingleton(tts.Object);

        var cut = Render<AudioSettingsTab>();

        cut.Find("#audio-volume-test").Click();

        cut.WaitForAssertion(() =>
        {
            var button = cut.Find("#audio-volume-test");
            Assert.Contains("Stop volume test", button.TextContent, StringComparison.Ordinal);
        });

        cut.WaitForAssertion(() =>
        {
            tts.Verify(
                player => player.SpeakAsync(
                    ResolveResourceText(VoiceInterfaceResourceIds.SettingsAudioVolumeTest, VoiceInterfaceModelCatalog.DefaultLanguage),
                    "pt",
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        });

        cut.Find("#audio-volume-test").Click();

        cut.WaitForAssertion(() =>
        {
            var button = cut.Find("#audio-volume-test");
            Assert.Contains("Start volume test", button.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("missing text", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void WhenSupportedLanguagesClientThrows_ShowsErrorAndStopsTest()
    {
        Services.AddSingleton<IAudioOutputSettings>(new RecordingAudioOutputSettings { Volume = 0.5f });

        var supportedLanguagesClient = new Mock<ISupportedLanguagesClient>();
        supportedLanguagesClient
            .Setup(client => client.GetAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        Services.AddSingleton(supportedLanguagesClient.Object);

        Services.AddSingleton(Mock.Of<ITextToSpeechPlayer>());

        var cut = Render<AudioSettingsTab>();

        cut.Find("#audio-volume-test").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("boom", cut.Markup, StringComparison.OrdinalIgnoreCase);

            var button = cut.Find("#audio-volume-test");
            Assert.Contains("Start volume test", button.TextContent, StringComparison.Ordinal);
        });
    }

    private static ISupportedLanguagesClient CreateSupportedLanguagesClient(params string[] isoCodes)
    {
        ArgumentNullException.ThrowIfNull(isoCodes);

        var client = new Mock<ISupportedLanguagesClient>();
        client
            .Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupportedLanguages(
                isoCodes.FirstOrDefault() ?? VoiceInterfaceModelCatalog.DefaultLanguage,
                isoCodes.Select(isoCode => new SupportedLanguage(isoCode, isoCode)).ToArray()));

        return client.Object;
    }

    private static string ResolveResourceText(string resourceId, string isoCode)
    {
        Assert.True(VoiceInterfaceModelCatalog.Resources.TryGetResourceText(resourceId, isoCode, out var text));
        return text;
    }
}
