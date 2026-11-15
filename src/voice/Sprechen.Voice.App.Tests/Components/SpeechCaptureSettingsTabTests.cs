using Bunit;
using Moq;
using Sprechen.Web.SpeechComponents;

namespace Sprechen.Voice.App.Tests.Components;

public sealed class SpeechCaptureSettingsTabTests : TestContext
{
    [Fact]
    public void RendersCurrentValueAndPersistsChanges()
    {
        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>();
        settings.Setup(s => s.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(3000));
        settings.Setup(s => s.SetSilenceDebounceMsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        Services.AddSingleton(settings.Object);
        Services.AddSingleton(Mock.Of<ISpeechCaptureAmplitudeThresholdSettings>());

        var cut = Render<SpeechCaptureSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("3000 ms", cut.Markup, StringComparison.Ordinal);
        });

        cut.Find("input[type=range]").Change("3500");

        settings.Verify(s => s.SetSilenceDebounceMsAsync(3500, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void OnSilenceDebounceChangedAsync_WhenValueIsInvalid_ShowsError()
    {
        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>();
        settings.Setup(s => s.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(3000));

        Services.AddSingleton(settings.Object);
        Services.AddSingleton(Mock.Of<ISpeechCaptureAmplitudeThresholdSettings>());

        var cut = Render<SpeechCaptureSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("3000 ms", cut.Markup, StringComparison.Ordinal);
        });

        cut.Find("input[type=range]").Change("not-a-number");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Value is invalid.", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void WhenLoadFails_ShowsError()
    {
        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>();
        settings.Setup(s => s.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("load failed"));

        Services.AddSingleton(settings.Object);
        Services.AddSingleton(Mock.Of<ISpeechCaptureAmplitudeThresholdSettings>());

        var cut = Render<SpeechCaptureSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("load failed", cut.Find(".alert").TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void OnSilenceDebounceChangedAsync_WhenPersistFails_ShowsError()
    {
        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>();
        settings.Setup(s => s.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(3000));
        settings.Setup(s => s.SetSilenceDebounceMsAsync(3500, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("persist failed"));

        Services.AddSingleton(settings.Object);
        Services.AddSingleton(Mock.Of<ISpeechCaptureAmplitudeThresholdSettings>());

        var cut = Render<SpeechCaptureSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("3000 ms", cut.Markup, StringComparison.Ordinal);
        });

        cut.Find("input[type=range]").Change("3500");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("persist failed", cut.Find(".alert").TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ResetAsync_WhenInvoked_ResetsValue()
    {
        var call = 0;
        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>();
        settings.Setup(s => s.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(() => new ValueTask<int>(call++ == 0 ? 3500 : 3000));
        settings.Setup(s => s.ResetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        Services.AddSingleton(settings.Object);
        Services.AddSingleton(Mock.Of<ISpeechCaptureAmplitudeThresholdSettings>());

        var cut = Render<SpeechCaptureSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("3500 ms", cut.Markup, StringComparison.Ordinal);
        });

        cut.Find("button.btn.btn-outline-secondary").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("3000 ms", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ResetAsync_WhenResetFails_ShowsError()
    {
        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>();
        settings.Setup(s => s.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(3500));
        settings.Setup(s => s.ResetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("reset failed"));

        Services.AddSingleton(settings.Object);
        Services.AddSingleton(Mock.Of<ISpeechCaptureAmplitudeThresholdSettings>());

        var cut = Render<SpeechCaptureSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("3500 ms", cut.Markup, StringComparison.Ordinal);
        });

        cut.Find("button.btn.btn-outline-secondary").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("reset failed", cut.Find(".alert").TextContent, StringComparison.Ordinal);
        });
    }
    [Fact]
    public void RendersTheModeledFloor_WhenTheSettingWasNeverWritten()
    {
        // Der Wert 0 steht für eine ungesetzte Einstellung und liegt außerhalb des wählbaren Bereichs.
        // Deshalb zeigt der Regler stattdessen seinen konfigurierten Mindestwert.
        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>();
        settings.SetupGet(s => s.SilenceDebounceMsMin).Returns(500);
        settings.SetupGet(s => s.SilenceDebounceMsMax).Returns(6000);
        settings.SetupGet(s => s.SilenceDebounceMsStep).Returns(100);
        settings.SetupGet(s => s.SilenceDebounceMsMid).Returns(3000);
        settings.Setup(s => s.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(0));

        Services.AddSingleton(settings.Object);
        Services.AddSingleton(Mock.Of<ISpeechCaptureAmplitudeThresholdSettings>());

        var cut = Render<SpeechCaptureSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            // Der Mindestwert steht auch in der Skalenbeschriftung; deshalb direkt den Eingabewert prüfen.
            Assert.Equal("500", cut.Find("input.form-range").GetAttribute("value"));
        });
    }

    [Fact]
    public void RendersBothSettingsAndPersistsTheirChangesIndependently()
    {
        var (silence, amplitude) = AddSettings(3000, 400);
        var cut = Render<SpeechCaptureSettingsTab>();
        cut.WaitForAssertion(() =>
        {
            var sliders = cut.FindAll("input.form-range");
            Assert.Equal(2, sliders.Count);
            Assert.Equal("3000", sliders[0].GetAttribute("value"));
            Assert.Equal("500", sliders[0].GetAttribute("min"));
            Assert.Equal("6000", sliders[0].GetAttribute("max"));
            Assert.Equal("100", sliders[0].GetAttribute("step"));
            Assert.Equal("400", sliders[1].GetAttribute("value"));
            Assert.Equal("50", sliders[1].GetAttribute("min"));
            Assert.Equal("2000", sliders[1].GetAttribute("max"));
            Assert.Equal("10", sliders[1].GetAttribute("step"));
        });

        cut.FindAll("input.form-range")[0].Change("3500");
        silence.Verify(owner => owner.SetSilenceDebounceMsAsync(3500, It.IsAny<CancellationToken>()), Times.Once);
        amplitude.Verify(owner => owner.SetSpeechAmplitudeThresholdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        cut.FindAll("input.form-range")[1].Change("450");
        amplitude.Verify(owner => owner.SetSpeechAmplitudeThresholdAsync(450, It.IsAny<CancellationToken>()), Times.Once);
        silence.Verify(owner => owner.SetSilenceDebounceMsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void UnsetSettingsRenderSilenceMinimumAndAmplitudeMidpoint()
    {
        AddSettings(0, 0);
        var cut = Render<SpeechCaptureSettingsTab>();
        cut.WaitForAssertion(() =>
        {
            var sliders = cut.FindAll("input.form-range");
            Assert.Equal("500", sliders[0].GetAttribute("value"));
            Assert.Equal("350", sliders[1].GetAttribute("value"));
        });
    }

    [Fact]
    public void ResetClearsBothSettingsAndReloadsBothValues()
    {
        var (silence, amplitude) = AddSettings(3500, 800);
        silence.SetupSequence(owner => owner.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3500).ReturnsAsync(0);
        amplitude.SetupSequence(owner => owner.GetSpeechAmplitudeThresholdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(800).ReturnsAsync(0);
        var cut = Render<SpeechCaptureSettingsTab>();
        cut.WaitForAssertion(() => Assert.Equal("800", cut.FindAll("input.form-range")[1].GetAttribute("value")));

        cut.Find("button.btn-outline-secondary").Click();

        cut.WaitForAssertion(() =>
        {
            silence.Verify(owner => owner.ResetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()), Times.Once);
            amplitude.Verify(owner => owner.ResetSpeechAmplitudeThresholdAsync(It.IsAny<CancellationToken>()), Times.Once);
            silence.Verify(owner => owner.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
            amplitude.Verify(owner => owner.GetSpeechAmplitudeThresholdAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
            var sliders = cut.FindAll("input.form-range");
            Assert.Equal("500", sliders[0].GetAttribute("value"));
            Assert.Equal("350", sliders[1].GetAttribute("value"));
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AmplitudePortExceptionIsDisplayed(bool duringReset)
    {
        var (silence, amplitude) = AddSettings(3000, 400);
        var failure = new InvalidOperationException("amplitude port failed");
        if (duringReset)
        {
            amplitude.Setup(owner => owner.ResetSpeechAmplitudeThresholdAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(failure);
        }
        else
        {
            amplitude.Setup(owner => owner.SetSpeechAmplitudeThresholdAsync(450, It.IsAny<CancellationToken>()))
                .ThrowsAsync(failure);
        }

        var cut = Render<SpeechCaptureSettingsTab>();
        cut.WaitForAssertion(() => Assert.Equal("400", cut.FindAll("input.form-range")[1].GetAttribute("value")));
        if (duringReset)
        {
            cut.Find("button.btn-outline-secondary").Click();
        }
        else
        {
            cut.FindAll("input.form-range")[1].Change("450");
        }

        cut.WaitForAssertion(() => Assert.Equal("amplitude port failed", cut.Find(".alert").TextContent));
        if (duringReset)
        {
            silence.Verify(owner => owner.ResetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()), Times.Once);
            amplitude.Verify(owner => owner.ResetSpeechAmplitudeThresholdAsync(It.IsAny<CancellationToken>()), Times.Once);
            silence.Verify(owner => owner.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()), Times.Once);
            amplitude.Verify(owner => owner.GetSpeechAmplitudeThresholdAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    private (Mock<ISpeechCaptureSilenceDebounceSettings> Silence, Mock<ISpeechCaptureAmplitudeThresholdSettings> Amplitude)
        AddSettings(int silenceValue, int amplitudeValue)
    {
        var silence = new Mock<ISpeechCaptureSilenceDebounceSettings>();
        silence.SetupGet(owner => owner.SilenceDebounceMsMin).Returns(500);
        silence.SetupGet(owner => owner.SilenceDebounceMsMax).Returns(6000);
        silence.SetupGet(owner => owner.SilenceDebounceMsStep).Returns(100);
        silence.SetupGet(owner => owner.SilenceDebounceMsMid).Returns(3000);
        silence.Setup(owner => owner.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(silenceValue);
        silence.Setup(owner => owner.SetSilenceDebounceMsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        silence.Setup(owner => owner.ResetSilenceDebounceMsAsync(It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        var amplitude = new Mock<ISpeechCaptureAmplitudeThresholdSettings>();
        amplitude.SetupGet(owner => owner.SpeechAmplitudeThresholdMin).Returns(50);
        amplitude.SetupGet(owner => owner.SpeechAmplitudeThresholdMax).Returns(2000);
        amplitude.SetupGet(owner => owner.SpeechAmplitudeThresholdStep).Returns(10);
        amplitude.SetupGet(owner => owner.SpeechAmplitudeThresholdMid).Returns(350);
        amplitude.Setup(owner => owner.GetSpeechAmplitudeThresholdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(amplitudeValue);
        amplitude.Setup(owner => owner.SetSpeechAmplitudeThresholdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        amplitude.Setup(owner => owner.ResetSpeechAmplitudeThresholdAsync(It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        Services.AddSingleton(silence.Object);
        Services.AddSingleton(amplitude.Object);
        return (silence, amplitude);
    }
}
