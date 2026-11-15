using Bunit;
using Microsoft.JSInterop;
using Microsoft.Extensions.DependencyInjection;
using Sprechen.Speech.App.BrowserInfrastructure.Extensions;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechCaptureSettingsTests : TestContext
{
    [Fact]
    public async Task GetSilenceDebounceMsAsync_WhenCalled_ReturnsValueFromJs()
    {
        JSInterop.Setup<int>("speechCaptureSettings.getSilenceDebounceMs").SetResult(1234);

        ISpeechCaptureSilenceDebounceSettings settings = new SpeechCaptureSettings(JSInterop.JSRuntime);

        var ms = await settings.GetSilenceDebounceMsAsync(CancellationToken.None);

        Assert.Equal(1234, ms);
    }

    [Fact]
    public async Task SetSilenceDebounceMsAsync_WhenOutsideSupportedRange_Throws()
    {
        ISpeechCaptureSilenceDebounceSettings settings = new SpeechCaptureSettings(JSInterop.JSRuntime);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => settings.SetSilenceDebounceMsAsync(499, CancellationToken.None).AsTask());
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => settings.SetSilenceDebounceMsAsync(6001, CancellationToken.None).AsTask());
        Assert.Empty(JSInterop.Invocations);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(6000)]
    public async Task SetSilenceDebounceMsAsync_WhenValid_InvokesJs(int value)
    {
        JSInterop.SetupVoid("speechCaptureSettings.setSilenceDebounceMs", _ => true).SetVoidResult();

        ISpeechCaptureSilenceDebounceSettings settings = new SpeechCaptureSettings(JSInterop.JSRuntime);

        await settings.SetSilenceDebounceMsAsync(value, CancellationToken.None);

        var invocation = Assert.Single(JSInterop.Invocations);
        Assert.Equal("speechCaptureSettings.setSilenceDebounceMs", invocation.Identifier);
        Assert.Equal(value, Assert.IsType<int>(Assert.Single(invocation.Arguments)));
    }

    [Fact]
    public async Task ResetSilenceDebounceMsAsync_WhenCalled_InvokesJs()
    {
        JSInterop.SetupVoid("speechCaptureSettings.clearSilenceDebounceMs", _ => true).SetVoidResult();

        ISpeechCaptureSilenceDebounceSettings settings = new SpeechCaptureSettings(JSInterop.JSRuntime);

        await settings.ResetSilenceDebounceMsAsync(CancellationToken.None);

        var invocation = Assert.Single(JSInterop.Invocations);
        Assert.Equal("speechCaptureSettings.clearSilenceDebounceMs", invocation.Identifier);
        Assert.Empty(invocation.Arguments);
    }

    [Fact]
    public async Task GetSpeechAmplitudeThresholdAsync_WhenCalled_ReturnsValueFromJs()
    {
        JSInterop.Setup<int>("speechCaptureSettings.getSpeechAmplitudeThreshold").SetResult(200);

        ISpeechCaptureAmplitudeThresholdSettings settings = new SpeechCaptureSettings(JSInterop.JSRuntime);

        var value = await settings.GetSpeechAmplitudeThresholdAsync(CancellationToken.None);

        Assert.Equal(200, value);
    }

    [Fact]
    public async Task SetSpeechAmplitudeThresholdAsync_WhenOutsideSupportedRange_Throws()
    {
        ISpeechCaptureAmplitudeThresholdSettings settings = new SpeechCaptureSettings(JSInterop.JSRuntime);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => settings.SetSpeechAmplitudeThresholdAsync(49, CancellationToken.None).AsTask());
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => settings.SetSpeechAmplitudeThresholdAsync(2001, CancellationToken.None).AsTask());
        Assert.Empty(JSInterop.Invocations);
    }

    [Fact]
    public void SettingsPortsShareOneScopedBrowserAdapter()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime>(JSInterop.JSRuntime);
        services.AddSpeechBrowserInfrastructure();

        foreach (var type in new[]
        {
            typeof(SpeechCaptureSettings),
            typeof(ISpeechCaptureSilenceDebounceSettings),
            typeof(ISpeechCaptureAmplitudeThresholdSettings),
        })
        {
            Assert.Equal(ServiceLifetime.Scoped,
                Assert.Single(services, descriptor => descriptor.ServiceType == type).Lifetime);
        }

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        var adapter = first.ServiceProvider.GetRequiredService<SpeechCaptureSettings>();
        Assert.Same(adapter, first.ServiceProvider.GetRequiredService<ISpeechCaptureSilenceDebounceSettings>());
        Assert.Same(adapter, first.ServiceProvider.GetRequiredService<ISpeechCaptureAmplitudeThresholdSettings>());
        var secondAdapter = second.ServiceProvider.GetRequiredService<SpeechCaptureSettings>();
        Assert.NotSame(adapter, secondAdapter);
        Assert.Same(secondAdapter, second.ServiceProvider.GetRequiredService<ISpeechCaptureSilenceDebounceSettings>());
        Assert.Same(secondAdapter, second.ServiceProvider.GetRequiredService<ISpeechCaptureAmplitudeThresholdSettings>());
    }

    [Fact]
    public void SettingsPortsRetainTheirSupportedRanges()
    {
        var adapter = new SpeechCaptureSettings(JSInterop.JSRuntime);
        ISpeechCaptureSilenceDebounceSettings silence = adapter;
        ISpeechCaptureAmplitudeThresholdSettings amplitude = adapter;

        Assert.Equal((500, 6000, 100, 3000),
            (silence.SilenceDebounceMsMin, silence.SilenceDebounceMsMax,
                silence.SilenceDebounceMsStep, silence.SilenceDebounceMsMid));
        Assert.Equal((50, 2000, 10, 350),
            (amplitude.SpeechAmplitudeThresholdMin, amplitude.SpeechAmplitudeThresholdMax,
                amplitude.SpeechAmplitudeThresholdStep, amplitude.SpeechAmplitudeThresholdMid));
    }

    [Theory]
    [InlineData(50)]
    [InlineData(2000)]
    public async Task SetSpeechAmplitudeThresholdAsync_WhenValid_ForwardsExactValue(int value)
    {
        JSInterop.SetupVoid("speechCaptureSettings.setSpeechAmplitudeThreshold", _ => true).SetVoidResult();
        ISpeechCaptureAmplitudeThresholdSettings settings = new SpeechCaptureSettings(JSInterop.JSRuntime);

        await settings.SetSpeechAmplitudeThresholdAsync(value, CancellationToken.None);

        var invocation = Assert.Single(JSInterop.Invocations);
        Assert.Equal("speechCaptureSettings.setSpeechAmplitudeThreshold", invocation.Identifier);
        Assert.Equal(value, Assert.IsType<int>(Assert.Single(invocation.Arguments)));
    }

    [Fact]
    public async Task ResetSpeechAmplitudeThresholdAsync_InvokesOnlyItsClearOperation()
    {
        JSInterop.SetupVoid("speechCaptureSettings.clearSpeechAmplitudeThreshold", _ => true).SetVoidResult();
        ISpeechCaptureAmplitudeThresholdSettings settings = new SpeechCaptureSettings(JSInterop.JSRuntime);

        await settings.ResetSpeechAmplitudeThresholdAsync(CancellationToken.None);

        var invocation = Assert.Single(JSInterop.Invocations);
        Assert.Equal("speechCaptureSettings.clearSpeechAmplitudeThreshold", invocation.Identifier);
        Assert.Empty(invocation.Arguments);
    }
}
