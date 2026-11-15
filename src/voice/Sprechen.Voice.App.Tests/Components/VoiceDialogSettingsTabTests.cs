using Bunit;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Web.SpeechComponents;
using Sprechen.Voice.App.Tests.TestDoubles;

namespace Sprechen.Voice.App.Tests.Components;

public sealed class VoiceDialogSettingsTabTests : TestContext
{
    [Fact]
    public async Task WhenLoaded_RendersCatalogDefaultsAndStoredOverrides()
    {
        Services.AddSingleton<IUserLanguageState>(new UserLanguageState());
        Services.AddSingleton<IFormsCatalogDataClient>(new FixedFormsCatalogDataClient(CreateCatalog()));
        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);
        Services.AddSingleton<IBrowserDebugConsole>(browserDebugConsole);
        var overrideStore = new InMemoryVoiceDialogSettingsOverrideStore();
        await overrideStore.SetDeviceAllowBargeInOverrideAsync(false, CancellationToken.None);
        await overrideStore.SetApplicationAllowBargeInOverrideAsync("demo.true", false, CancellationToken.None);
        Services.AddSingleton<IVoiceDialogSettingsOverrideStore>(overrideStore);

        var cut = Render<VoiceDialogSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("false", cut.Find("#voice-dialog-device-override").GetAttribute("value"));
            Assert.NotNull(cut.Find("#browser-debug-enabled").GetAttribute("checked"));
            Assert.Contains("Enabled", cut.Find("[data-testid='voice-dialog-catalog-default-demo.true']").TextContent, StringComparison.Ordinal);
            Assert.Contains("Disabled", cut.Find("[data-testid='voice-dialog-catalog-default-demo.false']").TextContent, StringComparison.Ordinal);
            Assert.Equal("false", cut.Find("select[data-application-id='demo.true']").GetAttribute("value"));
        });
    }

    [Fact]
    public void DeviceOverride_WhenChanged_PersistsOverride()
    {
        Services.AddSingleton<IUserLanguageState>(new UserLanguageState());
        Services.AddSingleton<IFormsCatalogDataClient>(new FixedFormsCatalogDataClient(CreateCatalog()));
        Services.AddSingleton<IBrowserDebugConsole>(new InMemoryBrowserDebugConsole());
        var overrideStore = new InMemoryVoiceDialogSettingsOverrideStore();
        Services.AddSingleton<IVoiceDialogSettingsOverrideStore>(overrideStore);

        var cut = Render<VoiceDialogSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Loading.", cut.Markup, StringComparison.Ordinal);
        });

        cut.Find("#voice-dialog-device-override").Change("true");

        cut.WaitForAssertion(() =>
        {
            Assert.True(overrideStore.DeviceAllowBargeInOverride);
        });
    }

    [Fact]
    public void AutoAdvanceDeviceOverride_WhenChanged_PersistsOverride()
    {
        Services.AddSingleton<IUserLanguageState>(new UserLanguageState());
        Services.AddSingleton<IFormsCatalogDataClient>(new FixedFormsCatalogDataClient(CreateCatalog()));
        Services.AddSingleton<IBrowserDebugConsole>(new InMemoryBrowserDebugConsole());
        var overrideStore = new InMemoryVoiceDialogSettingsOverrideStore();
        Services.AddSingleton<IVoiceDialogSettingsOverrideStore>(overrideStore);

        var cut = Render<VoiceDialogSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Loading.", cut.Markup, StringComparison.Ordinal);
        });

        cut.Find("#voice-dialog-auto-advance-device-override").Change("false");

        cut.WaitForAssertion(() =>
        {
            Assert.False(overrideStore.DeviceAutoAdvanceAfterCommitOverride);
            Assert.True(overrideStore.DeviceAutoAdvanceAfterCommitOverride.HasValue);
        });
    }

    [Fact]
    public void ApplicationOverride_WhenChanged_PersistsOverride()
    {
        Services.AddSingleton<IUserLanguageState>(new UserLanguageState());
        Services.AddSingleton<IFormsCatalogDataClient>(new FixedFormsCatalogDataClient(CreateCatalog()));
        Services.AddSingleton<IBrowserDebugConsole>(new InMemoryBrowserDebugConsole());
        var overrideStore = new InMemoryVoiceDialogSettingsOverrideStore();
        Services.AddSingleton<IVoiceDialogSettingsOverrideStore>(overrideStore);

        var cut = Render<VoiceDialogSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Loading.", cut.Markup, StringComparison.Ordinal);
        });

        cut.Find("select[data-application-id='demo.false']").Change("true");

        cut.WaitForAssertion(() =>
        {
            Assert.True(overrideStore.ApplicationAllowBargeInOverrides["demo.false"]);
        });
    }

    [Fact]
    public void BrowserDebug_WhenChanged_PersistsToggle()
    {
        Services.AddSingleton<IUserLanguageState>(new UserLanguageState());
        Services.AddSingleton<IFormsCatalogDataClient>(new FixedFormsCatalogDataClient(CreateCatalog()));
        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        Services.AddSingleton<IBrowserDebugConsole>(browserDebugConsole);
        Services.AddSingleton<IVoiceDialogSettingsOverrideStore>(new InMemoryVoiceDialogSettingsOverrideStore());

        var cut = Render<VoiceDialogSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Loading.", cut.Markup, StringComparison.Ordinal);
        });

        cut.Find("#browser-debug-enabled").Change(true);

        cut.WaitForAssertion(() =>
        {
            Assert.True(browserDebugConsole.Enabled);
        });
    }

    [Fact]
    public void WhenCatalogIsEmpty_RendersNoApplicationsMessage()
    {
        Services.AddSingleton<IUserLanguageState>(new UserLanguageState());
        Services.AddSingleton<IFormsCatalogDataClient>(new FixedFormsCatalogDataClient(new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1"
        }));
        Services.AddSingleton<IBrowserDebugConsole>(new InMemoryBrowserDebugConsole());
        Services.AddSingleton<IVoiceDialogSettingsOverrideStore>(new InMemoryVoiceDialogSettingsOverrideStore());

        var cut = Render<VoiceDialogSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No applications available.", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void WhenLoadFails_ShowsWarning()
    {
        Services.AddSingleton<IUserLanguageState>(new UserLanguageState());
        var catalogClient = new Mock<IFormsCatalogDataClient>();
        catalogClient.Setup(client => client.GetFormsCatalogAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromException<FormsCatalog>(new InvalidOperationException("catalog unavailable")));
        Services.AddSingleton(catalogClient.Object);
        Services.AddSingleton<IBrowserDebugConsole>(new InMemoryBrowserDebugConsole());
        Services.AddSingleton<IVoiceDialogSettingsOverrideStore>(new InMemoryVoiceDialogSettingsOverrideStore());

        var cut = Render<VoiceDialogSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("catalog unavailable", cut.Find(".alert").TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void DeviceOverride_WhenPersistFails_RevertsSelectionAndShowsWarning()
    {
        Services.AddSingleton<IUserLanguageState>(new UserLanguageState());
        Services.AddSingleton<IFormsCatalogDataClient>(new FixedFormsCatalogDataClient(CreateCatalog()));
        Services.AddSingleton<IBrowserDebugConsole>(new InMemoryBrowserDebugConsole());
        var overrideStore = new Mock<IVoiceDialogSettingsOverrideStore>();
        overrideStore.Setup(store => store.GetDeviceAllowBargeInOverrideAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<bool?>(null));
        overrideStore.Setup(store => store.GetApplicationAllowBargeInOverridesAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<IReadOnlyDictionary<string, bool>>(
                new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)));
        overrideStore.Setup(store => store.SetDeviceAllowBargeInOverrideAsync(true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("device override failed"));
        Services.AddSingleton(overrideStore.Object);

        var cut = Render<VoiceDialogSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Loading.", cut.Markup, StringComparison.Ordinal);
        });

        cut.Find("#voice-dialog-device-override").Change("true");

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(string.Empty, cut.Find("#voice-dialog-device-override").GetAttribute("value"));
            Assert.Contains("device override failed", cut.Find(".alert").TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void BrowserDebug_WhenPersistFails_RevertsToggleAndShowsWarning()
    {
        Services.AddSingleton<IUserLanguageState>(new UserLanguageState());
        Services.AddSingleton<IFormsCatalogDataClient>(new FixedFormsCatalogDataClient(CreateCatalog()));
        var browserDebugConsole = new Mock<IBrowserDebugConsole>();
        browserDebugConsole.Setup(console => console.GetEnabledAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(false));
        browserDebugConsole.Setup(console => console.SetEnabledAsync(true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("browser debug failed"));
        Services.AddSingleton(browserDebugConsole.Object);
        Services.AddSingleton<IVoiceDialogSettingsOverrideStore>(new InMemoryVoiceDialogSettingsOverrideStore());

        var cut = Render<VoiceDialogSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.Null(cut.Find("#browser-debug-enabled").GetAttribute("checked"));
        });

        cut.Find("#browser-debug-enabled").Change(true);

        cut.WaitForAssertion(() =>
        {
            Assert.Null(cut.Find("#browser-debug-enabled").GetAttribute("checked"));
            Assert.Contains("browser debug failed", cut.Find(".alert").TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task ApplicationOverride_WhenCleared_RemovesStoredOverride()
    {
        Services.AddSingleton<IUserLanguageState>(new UserLanguageState());
        Services.AddSingleton<IFormsCatalogDataClient>(new FixedFormsCatalogDataClient(CreateCatalog()));
        Services.AddSingleton<IBrowserDebugConsole>(new InMemoryBrowserDebugConsole());
        var overrideStore = new InMemoryVoiceDialogSettingsOverrideStore();
        await overrideStore.SetApplicationAllowBargeInOverrideAsync("demo.true", false, CancellationToken.None);
        Services.AddSingleton<IVoiceDialogSettingsOverrideStore>(overrideStore);

        var cut = Render<VoiceDialogSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("false", cut.Find("select[data-application-id='demo.true']").GetAttribute("value"));
        });

        cut.Find("select[data-application-id='demo.true']").Change(string.Empty);

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("demo.true", overrideStore.ApplicationAllowBargeInOverrides.Keys);
        });
    }

    [Fact]
    public void ApplicationOverride_WhenPersistFails_RevertsSelectionAndShowsWarning()
    {
        Services.AddSingleton<IUserLanguageState>(new UserLanguageState());
        Services.AddSingleton<IFormsCatalogDataClient>(new FixedFormsCatalogDataClient(CreateCatalog()));
        Services.AddSingleton<IBrowserDebugConsole>(new InMemoryBrowserDebugConsole());
        var overrideStore = new Mock<IVoiceDialogSettingsOverrideStore>();
        overrideStore.Setup(store => store.GetDeviceAllowBargeInOverrideAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<bool?>(null));
        overrideStore.Setup(store => store.GetApplicationAllowBargeInOverridesAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<IReadOnlyDictionary<string, bool>>(
                new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
                {
                    ["demo.false"] = false
                }));
        overrideStore.Setup(store => store.SetApplicationAllowBargeInOverrideAsync(
                "demo.false",
                true,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("application override failed"));
        Services.AddSingleton(overrideStore.Object);

        var cut = Render<VoiceDialogSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("false", cut.Find("select[data-application-id='demo.false']").GetAttribute("value"));
        });

        cut.Find("select[data-application-id='demo.false']").Change("true");

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("false", cut.Find("select[data-application-id='demo.false']").GetAttribute("value"));
            Assert.Contains("application override failed", cut.Find(".alert").TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ApplicationOverride_WhenPersistFailsWithoutPreviousOverride_ClearsSelectionAndShowsWarning()
    {
        Services.AddSingleton<IUserLanguageState>(new UserLanguageState());
        Services.AddSingleton<IFormsCatalogDataClient>(new FixedFormsCatalogDataClient(CreateCatalog()));
        Services.AddSingleton<IBrowserDebugConsole>(new InMemoryBrowserDebugConsole());
        var overrideStore = new Mock<IVoiceDialogSettingsOverrideStore>();
        overrideStore.Setup(store => store.GetDeviceAllowBargeInOverrideAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<bool?>(null));
        overrideStore.Setup(store => store.GetApplicationAllowBargeInOverridesAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<IReadOnlyDictionary<string, bool>>(
                new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)));
        overrideStore.Setup(store => store.SetApplicationAllowBargeInOverrideAsync(
                "demo.true",
                true,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("application override failed"));
        Services.AddSingleton(overrideStore.Object);

        var cut = Render<VoiceDialogSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(string.Empty, cut.Find("select[data-application-id='demo.true']").GetAttribute("value"));
        });

        cut.Find("select[data-application-id='demo.true']").Change("true");

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(string.Empty, cut.Find("select[data-application-id='demo.true']").GetAttribute("value"));
            Assert.Contains("application override failed", cut.Find(".alert").TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void WhenLoaded_UsesCanonicalApplicationDisplayNamesForCurrentLanguage()
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");
        Services.AddSingleton<IUserLanguageState>(userLanguageState);
        Services.AddSingleton<IFormsCatalogDataClient>(new FixedFormsCatalogDataClient(CreateCatalog()));
        Services.AddSingleton<IBrowserDebugConsole>(new InMemoryBrowserDebugConsole());
        Services.AddSingleton<IVoiceDialogSettingsOverrideStore>(new InMemoryVoiceDialogSettingsOverrideStore());

        var cut = Render<VoiceDialogSettingsTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Reiseassistent", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Travel assistant", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void WhenUserLanguageChanges_RerendersLocalizedApplicationDisplayNames()
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");
        Services.AddSingleton<IUserLanguageState>(userLanguageState);
        Services.AddSingleton<IFormsCatalogDataClient>(new FixedFormsCatalogDataClient(CreateCatalog()));
        Services.AddSingleton<IBrowserDebugConsole>(new InMemoryBrowserDebugConsole());
        Services.AddSingleton<IVoiceDialogSettingsOverrideStore>(new InMemoryVoiceDialogSettingsOverrideStore());

        var cut = Render<VoiceDialogSettingsTab>();

        userLanguageState.SetSelectedIsoCode("en");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Travel assistant", cut.Markup, StringComparison.Ordinal);
        });
    }

    private static FormsCatalog CreateCatalog()
    {
        return new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            VoiceDialogSettings = new VoiceDialogSettings { AllowBargeIn = false },
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "demo.true",
                    DisplayName = new LocalizedText
                    {
                        ["en"] = "Travel assistant",
                        ["de"] = "Reiseassistent"
                    },
                    SupportedLanguages = ["en", "de"],
                    DefaultLanguage = "en",
                    VoiceDialogSettings = new VoiceDialogSettings { AllowBargeIn = true }
                },
                new ApplicationDescriptor
                {
                    ApplicationId = "demo.false",
                    DisplayName = new LocalizedText
                    {
                        ["en"] = "Passenger survey",
                        ["de"] = "Passagierbefragung"
                    },
                    SupportedLanguages = ["en", "de"],
                    DefaultLanguage = "en"
                }
            ]
        };
    }
}
