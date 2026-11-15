using Bunit;
using Microsoft.JSInterop;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class VoiceActivationSettingsTests : TestContext
{
    [Fact]
    public async Task GetWakeWordActivationFormsOverrideAsync_WhenCalled_NormalizesActivationForms()
    {
        JSInterop.Setup<string[]>("voiceActivationSettings.getWakeWordActivationFormsOverride")
            .SetResult([" Okay ", "okay", "OKAY"]);

        var settings = new VoiceActivationSettings(JSInterop.JSRuntime);

        var activationForms = await settings.GetWakeWordActivationFormsOverrideAsync(CancellationToken.None);

        Assert.Equal(["Okay"], activationForms);
    }

    [Fact]
    public async Task SetWakeWordActivationFormsOverrideAsync_WhenWakeWordsAreNull_Throws()
    {
        var settings = new VoiceActivationSettings(JSInterop.JSRuntime);

        await Assert.ThrowsAsync<ArgumentNullException>(() => settings.SetWakeWordActivationFormsOverrideAsync(null!, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SetWakeWordActivationFormsOverrideAsync_WhenCalled_InvokesJs()
    {
        JSInterop.SetupVoid("voiceActivationSettings.setWakeWordActivationFormsOverride", _ => true).SetVoidResult();

        var settings = new VoiceActivationSettings(JSInterop.JSRuntime);

        await settings.SetWakeWordActivationFormsOverrideAsync(["Nova", " nova "], CancellationToken.None);

        JSInterop.VerifyInvoke("voiceActivationSettings.setWakeWordActivationFormsOverride");
    }

    [Fact]
    public async Task ClearWakeWordActivationFormsOverrideAsync_WhenCalled_InvokesJs()
    {
        JSInterop.SetupVoid("voiceActivationSettings.clearWakeWordActivationFormsOverride", _ => true).SetVoidResult();

        var settings = new VoiceActivationSettings(JSInterop.JSRuntime);

        await settings.ClearWakeWordActivationFormsOverrideAsync(CancellationToken.None);

        JSInterop.VerifyInvoke("voiceActivationSettings.clearWakeWordActivationFormsOverride");
    }
}

