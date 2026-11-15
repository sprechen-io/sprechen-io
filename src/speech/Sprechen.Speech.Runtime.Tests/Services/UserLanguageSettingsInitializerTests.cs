using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class UserLanguageSettingsInitializerTests
{
    [Fact]
    public async Task InitializeAsync_WhenStoreContainsSupportedLanguage_AppliesStoredSelection()
    {
        var store = new Mock<IUserLanguageSettingsStore>(MockBehavior.Strict);
        store.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(new UserLanguageSettings("en", PolitenessLevel.Informal)));

        var state = new UserLanguageState();
        var initializer = CreateInitializer(store.Object, state, new FixedBrowserLanguageProvider("it-IT"));

        await initializer.InitializeAsync(CancellationToken.None);

        Assert.Equal("en", state.SelectedIsoCode);
        Assert.Equal(PolitenessLevel.Informal, state.SelectedPolitenessLevel);
    }

    [Fact]
    public async Task InitializeAsync_WhenStoreContainsUnsupportedLanguage_UsesFirstSupportedBrowserLanguage()
    {
        var store = new Mock<IUserLanguageSettingsStore>(MockBehavior.Strict);
        store.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(new UserLanguageSettings("pt", PolitenessLevel.Formal)));

        var state = new UserLanguageState();
        var initializer = CreateInitializer(store.Object, state, new FixedBrowserLanguageProvider("ja-JP", "en-GB"));

        await initializer.InitializeAsync(CancellationToken.None);

        Assert.Equal("en", state.SelectedIsoCode);
        Assert.Null(state.SelectedPolitenessLevel);
    }

    [Fact]
    public async Task InitializeAsync_WhenStoreIsEmpty_UsesBrowserLanguageRegionFallback()
    {
        var store = new Mock<IUserLanguageSettingsStore>(MockBehavior.Strict);
        store.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(new UserLanguageSettings(null, null)));

        var state = new UserLanguageState();
        var initializer = CreateInitializer(store.Object, state, new FixedBrowserLanguageProvider("de-CH"));

        await initializer.InitializeAsync(CancellationToken.None);

        Assert.Equal("de", state.SelectedIsoCode);
        Assert.Null(state.SelectedPolitenessLevel);
    }

    [Fact]
    public async Task InitializeAsync_WhenStoreAndBrowserLanguagesAreUnsupported_UsesModeledDefaultLanguage()
    {
        var store = new Mock<IUserLanguageSettingsStore>(MockBehavior.Strict);
        store.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(new UserLanguageSettings("pt", null)));

        var state = new UserLanguageState();
        var initializer = CreateInitializer(store.Object, state, new FixedBrowserLanguageProvider("ja-JP", "pt-BR"));

        await initializer.InitializeAsync(CancellationToken.None);

        Assert.Equal(VoiceInterfaceModelCatalog.DefaultLanguage, state.SelectedIsoCode);
        Assert.Null(state.SelectedPolitenessLevel);
    }

    [Fact]
    public async Task InitializeAsync_WhenStoreIsEmptyAndBrowserProvidesNoTags_UsesModeledDefaultLanguage()
    {
        var store = new Mock<IUserLanguageSettingsStore>(MockBehavior.Strict);
        store.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(new UserLanguageSettings(null, null)));

        var state = new UserLanguageState();
        var initializer = CreateInitializer(store.Object, state, new NoOpBrowserLanguageProvider());

        await initializer.InitializeAsync(CancellationToken.None);

        Assert.Equal(VoiceInterfaceModelCatalog.DefaultLanguage, state.SelectedIsoCode);
        Assert.Null(state.SelectedPolitenessLevel);
    }

    private static UserLanguageSettingsInitializer CreateInitializer(
        IUserLanguageSettingsStore store,
        IUserLanguageState state,
        IBrowserLanguageProvider browserLanguageProvider)
    {
        return new UserLanguageSettingsInitializer(
            store,
            state,
            browserLanguageProvider,
            NullLogger<UserLanguageSettingsInitializer>.Instance);
    }
}
