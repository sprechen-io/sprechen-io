using Bunit;
using Microsoft.JSInterop;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class VoiceDialogSettingsOverrideStoreTests : TestContext
{
    [Fact]
    public async Task GetDeviceAllowBargeInOverrideAsync_WhenCalled_ReturnsValueFromJs()
    {
        JSInterop.Setup<bool?>("voiceDialogSettingsOverrides.getDeviceAllowBargeInOverride")
            .SetResult(true);

        var store = new VoiceDialogSettingsOverrideStore(JSInterop.JSRuntime);

        var allowBargeIn = await store.GetDeviceAllowBargeInOverrideAsync(CancellationToken.None);

        Assert.True(allowBargeIn);
    }

    [Fact]
    public async Task GetDeviceAllowBargeInOverrideAsync_WhenJsBridgeIsUnavailable_ReturnsNull()
    {
        JSInterop.Setup<bool?>("voiceDialogSettingsOverrides.getDeviceAllowBargeInOverride")
            .SetException(new JSException("missing bridge"));

        var store = new VoiceDialogSettingsOverrideStore(JSInterop.JSRuntime);

        var allowBargeIn = await store.GetDeviceAllowBargeInOverrideAsync(CancellationToken.None);

        Assert.Null(allowBargeIn);
    }

    [Fact]
    public async Task SetDeviceAllowBargeInOverrideAsync_WhenCalled_InvokesJs()
    {
        JSInterop.SetupVoid("voiceDialogSettingsOverrides.setDeviceAllowBargeInOverride", _ => true).SetVoidResult();

        var store = new VoiceDialogSettingsOverrideStore(JSInterop.JSRuntime);

        await store.SetDeviceAllowBargeInOverrideAsync(false, CancellationToken.None);

        JSInterop.VerifyInvoke("voiceDialogSettingsOverrides.setDeviceAllowBargeInOverride");
    }

    [Fact]
    public async Task GetApplicationAllowBargeInOverridesAsync_WhenCalled_ReturnsCaseInsensitiveDictionary()
    {
        JSInterop.Setup<Dictionary<string, bool>>("voiceDialogSettingsOverrides.getApplicationAllowBargeInOverrides")
            .SetResult(new Dictionary<string, bool> { ["demo.app"] = true });

        var store = new VoiceDialogSettingsOverrideStore(JSInterop.JSRuntime);

        var overrides = await store.GetApplicationAllowBargeInOverridesAsync(CancellationToken.None);

        Assert.True(overrides["DEMO.APP"]);
    }

    [Fact]
    public async Task GetApplicationAllowBargeInOverridesAsync_WhenJsBridgeIsUnavailable_ReturnsEmptyDictionary()
    {
        JSInterop.Setup<Dictionary<string, bool>>("voiceDialogSettingsOverrides.getApplicationAllowBargeInOverrides")
            .SetException(new JSException("missing bridge"));

        var store = new VoiceDialogSettingsOverrideStore(JSInterop.JSRuntime);

        var overrides = await store.GetApplicationAllowBargeInOverridesAsync(CancellationToken.None);

        Assert.Empty(overrides);
    }

    [Fact]
    public async Task GetApplicationAllowBargeInOverrideAsync_WhenApplicationIdMissing_Throws()
    {
        var store = new VoiceDialogSettingsOverrideStore(JSInterop.JSRuntime);

        await Assert.ThrowsAsync<ArgumentException>(() => store.GetApplicationAllowBargeInOverrideAsync(" ", CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SetApplicationAllowBargeInOverrideAsync_WhenJsBridgeIsUnavailable_CompletesWithoutThrowing()
    {
        JSInterop.SetupVoid("voiceDialogSettingsOverrides.setApplicationAllowBargeInOverride", _ => true)
            .SetException(new JSException("missing bridge"));

        var store = new VoiceDialogSettingsOverrideStore(JSInterop.JSRuntime);

        await store.SetApplicationAllowBargeInOverrideAsync("demo.app", true, CancellationToken.None);
    }

    [Fact]
    public async Task SetApplicationAllowBargeInOverrideAsync_WhenCalled_InvokesJs()
    {
        JSInterop.SetupVoid("voiceDialogSettingsOverrides.setApplicationAllowBargeInOverride", _ => true).SetVoidResult();

        var store = new VoiceDialogSettingsOverrideStore(JSInterop.JSRuntime);

        await store.SetApplicationAllowBargeInOverrideAsync("demo.app", true, CancellationToken.None);

        JSInterop.VerifyInvoke("voiceDialogSettingsOverrides.setApplicationAllowBargeInOverride");
    }

    [Fact]
    public async Task GetDeviceAutoAdvanceAfterCommitOverrideAsync_WhenCalled_ReturnsValueFromJs()
    {
        JSInterop.Setup<bool?>("voiceDialogSettingsOverrides.getDeviceAutoAdvanceAfterCommitOverride")
            .SetResult(false);

        var store = new VoiceDialogSettingsOverrideStore(JSInterop.JSRuntime);

        var autoAdvance = await store.GetDeviceAutoAdvanceAfterCommitOverrideAsync(CancellationToken.None);

        Assert.False(autoAdvance);
    }

    [Fact]
    public async Task GetDeviceAutoAdvanceAfterCommitOverrideAsync_WhenJsBridgeIsUnavailable_ReturnsNull()
    {
        JSInterop.Setup<bool?>("voiceDialogSettingsOverrides.getDeviceAutoAdvanceAfterCommitOverride")
            .SetException(new JSException("missing bridge"));

        var store = new VoiceDialogSettingsOverrideStore(JSInterop.JSRuntime);

        var autoAdvance = await store.GetDeviceAutoAdvanceAfterCommitOverrideAsync(CancellationToken.None);

        Assert.Null(autoAdvance);
    }

    [Fact]
    public async Task SetDeviceAutoAdvanceAfterCommitOverrideAsync_WhenCalled_InvokesJs()
    {
        JSInterop.SetupVoid("voiceDialogSettingsOverrides.setDeviceAutoAdvanceAfterCommitOverride", _ => true).SetVoidResult();

        var store = new VoiceDialogSettingsOverrideStore(JSInterop.JSRuntime);

        await store.SetDeviceAutoAdvanceAfterCommitOverrideAsync(false, CancellationToken.None);

        JSInterop.VerifyInvoke("voiceDialogSettingsOverrides.setDeviceAutoAdvanceAfterCommitOverride");
    }

    [Fact]
    public async Task GetApplicationAutoAdvanceAfterCommitOverridesAsync_WhenCalled_ReturnsCaseInsensitiveDictionary()
    {
        JSInterop.Setup<Dictionary<string, bool>>("voiceDialogSettingsOverrides.getApplicationAutoAdvanceAfterCommitOverrides")
            .SetResult(new Dictionary<string, bool> { ["demo.app"] = false });

        var store = new VoiceDialogSettingsOverrideStore(JSInterop.JSRuntime);

        var overrides = await store.GetApplicationAutoAdvanceAfterCommitOverridesAsync(CancellationToken.None);

        Assert.False(overrides["DEMO.APP"]);
    }

    [Fact]
    public async Task GetApplicationAutoAdvanceAfterCommitOverrideAsync_WhenApplicationIdMissing_Throws()
    {
        var store = new VoiceDialogSettingsOverrideStore(JSInterop.JSRuntime);

        await Assert.ThrowsAsync<ArgumentException>(
            () => store.GetApplicationAutoAdvanceAfterCommitOverrideAsync(" ", CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SetApplicationAutoAdvanceAfterCommitOverrideAsync_WhenCalled_InvokesJs()
    {
        JSInterop.SetupVoid("voiceDialogSettingsOverrides.setApplicationAutoAdvanceAfterCommitOverride", _ => true).SetVoidResult();

        var store = new VoiceDialogSettingsOverrideStore(JSInterop.JSRuntime);

        await store.SetApplicationAutoAdvanceAfterCommitOverrideAsync("demo.app", false, CancellationToken.None);

        JSInterop.VerifyInvoke("voiceDialogSettingsOverrides.setApplicationAutoAdvanceAfterCommitOverride");
    }
}
