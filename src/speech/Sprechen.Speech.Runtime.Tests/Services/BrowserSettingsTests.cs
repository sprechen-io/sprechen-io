using Microsoft.JSInterop;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class BrowserSettingsTests
{
    [Fact]
    public async Task AudioOutputSettings_ForwardsVolumeAndRejectsInvalidValues()
    {
        var jsRuntime = new FakeJsRuntime();
        jsRuntime.SetResult("pcmPlayer.getVolume", 0.75f);
        jsRuntime.SetVoidResult("pcmPlayer.setVolume");
        jsRuntime.SetVoidResult("earcons.setVolume");
        var settings = new AudioOutputSettings(jsRuntime);

        Assert.Equal(0.75f, await settings.GetVolumeAsync(CancellationToken.None));
        await settings.SetVolumeAsync(0.5f, CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => settings.SetVolumeAsync(float.NaN, CancellationToken.None).AsTask());
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => settings.SetVolumeAsync(1.1f, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SpeechCaptureSettings_ForwardsValidValuesAndRejectsInvalidValues()
    {
        var jsRuntime = new FakeJsRuntime();
        jsRuntime.SetResult("speechCaptureSettings.getSilenceDebounceMs", 1500);
        jsRuntime.SetVoidResult("speechCaptureSettings.setSilenceDebounceMs");
        jsRuntime.SetVoidResult("speechCaptureSettings.clearSilenceDebounceMs");
        var settings = new SpeechCaptureSettings(jsRuntime);

        Assert.Equal(1500, await settings.GetSilenceDebounceMsAsync(CancellationToken.None));
        await settings.SetSilenceDebounceMsAsync(1200, CancellationToken.None);
        await settings.ResetSilenceDebounceMsAsync(CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => settings.SetSilenceDebounceMsAsync(100, CancellationToken.None).AsTask());
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => settings.SetSilenceDebounceMsAsync(10000, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task VoiceActivationSettings_NormalizesAndForwardsActivationForms()
    {
        var jsRuntime = new FakeJsRuntime();
        jsRuntime.SetResult("voiceActivationSettings.getWakeWordActivationFormsOverride", new[] { "  okay sprechen  ", "OKAY SPRECHEN" });
        jsRuntime.SetVoidResult("voiceActivationSettings.setWakeWordActivationFormsOverride");
        jsRuntime.SetVoidResult("voiceActivationSettings.clearWakeWordActivationFormsOverride");
        var settings = new VoiceActivationSettings(jsRuntime);

        var forms = await settings.GetWakeWordActivationFormsOverrideAsync(CancellationToken.None);

        Assert.Equal(["okay sprechen"], forms);
        await settings.SetWakeWordActivationFormsOverrideAsync([" Okay Sprechen "], CancellationToken.None);
        await settings.ClearWakeWordActivationFormsOverrideAsync(CancellationToken.None);
        await Assert.ThrowsAsync<ArgumentNullException>(() => settings.SetWakeWordActivationFormsOverrideAsync(null!, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task ThemeSettings_RoundTripsModeAndFallsBackToSystemWhenJsFails()
    {
        var jsRuntime = new FakeJsRuntime();
        jsRuntime.SetResult("themeSettings.get", "dark");
        jsRuntime.SetVoidResult("themeSettings.set");
        var settings = new ThemeSettings(jsRuntime);

        Assert.Equal(ThemeMode.Dark, await settings.GetAsync(CancellationToken.None));
        await settings.SetAsync(ThemeMode.Light, CancellationToken.None);

        // Eine fehlende Browserverbindung darf die optionale Themespeicherung nicht zum Fehler machen.
        var failingRuntime = new FakeJsRuntime();
        failingRuntime.SetException("themeSettings.get", new JSException("missing bridge"));
        failingRuntime.SetException("themeSettings.set", new JSDisconnectedException("gone"));
        var failingSettings = new ThemeSettings(failingRuntime);

        Assert.Equal(ThemeMode.System, await failingSettings.GetAsync(CancellationToken.None));
        await failingSettings.SetAsync(ThemeMode.Dark, CancellationToken.None);
    }

    [Fact]
    public async Task UserLanguageSettingsStore_WhenJsFails_ReturnsFallbackAndSuppressesPersistenceFailure()
    {
        var jsRuntime = new FakeJsRuntime();
        jsRuntime.SetException("userLanguageSettings.get", new JSException("missing bridge"));
        jsRuntime.SetException("userLanguageSettings.set", new JSDisconnectedException("gone"));
        var store = new UserLanguageSettingsStore(jsRuntime);

        var settings = await store.GetAsync(CancellationToken.None);
        await store.SetAsync(new UserLanguageSettings("de", PolitenessLevel.Formal), CancellationToken.None);

        Assert.Null(settings.SelectedIsoCode);
        Assert.Null(settings.SelectedPolitenessLevel);
    }

    [Fact]
    public async Task VoiceDialogSettingsOverrideStore_UsesFallbacksAndTrimsApplicationIds()
    {
        var jsRuntime = new FakeJsRuntime();
        jsRuntime.SetException("voiceDialogSettingsOverrides.getDeviceAllowBargeInOverride", new JSException("missing"));
        jsRuntime.SetResult("voiceDialogSettingsOverrides.getApplicationAllowBargeInOverrides", new Dictionary<string, bool>
        {
            ["demo"] = true
        });
        jsRuntime.SetResult("voiceDialogSettingsOverrides.getApplicationAllowBargeInOverride", false);
        jsRuntime.SetVoidResult("voiceDialogSettingsOverrides.setDeviceAllowBargeInOverride");
        jsRuntime.SetVoidResult("voiceDialogSettingsOverrides.setApplicationAllowBargeInOverride");
        var store = new VoiceDialogSettingsOverrideStore(jsRuntime);

        Assert.Null(await store.GetDeviceAllowBargeInOverrideAsync(CancellationToken.None));
        var overrides = await store.GetApplicationAllowBargeInOverridesAsync(CancellationToken.None);
        Assert.True(overrides["DEMO"]);
        Assert.False(await store.GetApplicationAllowBargeInOverrideAsync(" demo ", CancellationToken.None));
        await store.SetDeviceAllowBargeInOverrideAsync(true, CancellationToken.None);
        await store.SetApplicationAllowBargeInOverrideAsync(" demo ", true, CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentException>(() => store.GetApplicationAllowBargeInOverrideAsync(" ", CancellationToken.None).AsTask());
        await Assert.ThrowsAsync<ArgumentException>(() => store.SetApplicationAllowBargeInOverrideAsync(" ", true, CancellationToken.None).AsTask());
    }
}
