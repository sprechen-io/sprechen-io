using Bunit;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class AudioOutputSettingsTests : TestContext
{
    [Fact]
    public void IAudioOutputSettings_ExposesOnlyVolumeMembers()
    {
        var methods = typeof(IAudioOutputSettings)
            .GetMethods()
            .OrderBy(static method => method.Name, StringComparer.Ordinal)
            .ToArray();

        Assert.Collection(
            methods,
            method =>
            {
                Assert.Equal(nameof(IAudioOutputSettings.GetVolumeAsync), method.Name);
                Assert.Equal(typeof(ValueTask<float>), method.ReturnType);
                Assert.Equal(
                    new[] { typeof(CancellationToken) },
                    method.GetParameters().Select(static parameter => parameter.ParameterType).ToArray());
            },
            method =>
            {
                Assert.Equal(nameof(IAudioOutputSettings.SetVolumeAsync), method.Name);
                Assert.Equal(typeof(ValueTask), method.ReturnType);
                Assert.Equal(
                    new[] { typeof(float), typeof(CancellationToken) },
                    method.GetParameters().Select(static parameter => parameter.ParameterType).ToArray());
            });
    }

    [Fact]
    public async Task GetVolumeAsync_WhenCalled_ReturnsValueFromJs()
    {
        JSInterop.Setup<float>("pcmPlayer.getVolume").SetResult(0.42f);

        var settings = new AudioOutputSettings(JSInterop.JSRuntime);

        var volume = await settings.GetVolumeAsync(CancellationToken.None);

        Assert.Equal(0.42f, volume);
    }

    [Fact]
    public async Task SetVolumeAsync_WhenVolumeIsOutOfRange_Throws()
    {
        var settings = new AudioOutputSettings(JSInterop.JSRuntime);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => settings.SetVolumeAsync(-0.01f, CancellationToken.None).AsTask());
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => settings.SetVolumeAsync(1.01f, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SetVolumeAsync_WhenValid_InvokesPcmPlayerAndEarcons()
    {
        JSInterop.SetupVoid("pcmPlayer.setVolume", _ => true).SetVoidResult();
        JSInterop.SetupVoid("earcons.setVolume", _ => true).SetVoidResult();

        var settings = new AudioOutputSettings(JSInterop.JSRuntime);

        await settings.SetVolumeAsync(0.5f, CancellationToken.None);

        JSInterop.VerifyInvoke("pcmPlayer.setVolume");
        JSInterop.VerifyInvoke("earcons.setVolume");
    }
}
