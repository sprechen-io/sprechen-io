using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormVoice;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandContextLoaderTests
{
    [Fact]
    public async Task LoadAsync_WhenLanguageInvalid_ReturnsAbandoned()
    {
        var catalogClient = new FixedFormsCatalogDataClient(CreateCatalog());
        var contractClient = new FixedMdaFormContractDataClient(CreateContractWithField());
        var loader = CreateLoader(
            catalogClient,
            contractClient,
            new UserLanguageState(),
            CreateOverrideStore(deviceAllowBargeInOverride: null, applicationAllowBargeInOverride: null));

        var initialValues = ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)
            .Add("x", "1");

        var result = await loader.LoadAsync(
            "invalid",
            " app ",
            " form ",
            SpeechAssistantApplicationSessionState.Empty,
            initialValues,
            CancellationToken.None);

        Assert.Null(result.Context);
        Assert.NotNull(result.AbandonedResult);
        Assert.True(result.AbandonedResult!.WasAbandoned);
        Assert.Equal("app", result.AbandonedResult.ApplicationId);
        Assert.Equal("form", result.AbandonedResult.FormId);
    }

    [Fact]
    public async Task LoadAsync_WhenContractMissingFields_ReturnsAbandoned()
    {
        var catalogClient = new FixedFormsCatalogDataClient(CreateCatalog());
        var contractClient = new FixedMdaFormContractDataClient(CreateEmptyContract());
        var loader = CreateLoader(
            catalogClient,
            contractClient,
            new UserLanguageState(),
            CreateOverrideStore(deviceAllowBargeInOverride: null, applicationAllowBargeInOverride: null));

        var result = await loader.LoadAsync(
            "de",
            "app",
            "form",
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.Null(result.Context);
        Assert.NotNull(result.AbandonedResult);
        Assert.True(result.AbandonedResult!.WasAbandoned);
    }

    [Fact]
    public async Task LoadAsync_WhenUserSelectedInformalPoliteness_OverridesVoiceDialogSettings()
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Informal);

        var catalogClient = new FixedFormsCatalogDataClient(CreateCatalog(new VoiceDialogSettings
        {
            PolitenessLevel = PolitenessLevel.Formal
        }));
        var contractClient = new FixedMdaFormContractDataClient(CreateContractWithField());
        var loader = CreateLoader(
            catalogClient,
            contractClient,
            userLanguageState,
            CreateOverrideStore(deviceAllowBargeInOverride: null, applicationAllowBargeInOverride: null));

        var result = await loader.LoadAsync(
            "de",
            "app",
            "form",
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.NotNull(result.Context);
        Assert.Equal(PolitenessLevel.Informal, result.Context!.VoiceSettings.GetPolitenessLevel());
    }

    [Fact]
    public async Task LoadAsync_WhenApplicationOverrideExists_UsesOverride()
    {
        var catalogClient = new FixedFormsCatalogDataClient(CreateCatalog(new VoiceDialogSettings
        {
            AllowBargeIn = false
        }));
        var contractClient = new FixedMdaFormContractDataClient(CreateContractWithField());
        var loader = CreateLoader(
            catalogClient,
            contractClient,
            new UserLanguageState(),
            CreateOverrideStore(deviceAllowBargeInOverride: null, applicationAllowBargeInOverride: true));

        var result = await loader.LoadAsync(
            "de",
            "app",
            "form",
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.NotNull(result.Context);
        Assert.True(result.Context!.VoiceSettings.GetAllowBargeIn());
    }

    [Fact]
    public async Task LoadAsync_WhenDeviceOverrideExists_UsesOverride()
    {
        var catalogClient = new FixedFormsCatalogDataClient(CreateCatalog(new VoiceDialogSettings
        {
            AllowBargeIn = false
        }));
        var contractClient = new FixedMdaFormContractDataClient(CreateContractWithField());
        var loader = CreateLoader(
            catalogClient,
            contractClient,
            new UserLanguageState(),
            CreateOverrideStore(deviceAllowBargeInOverride: true, applicationAllowBargeInOverride: null));

        var result = await loader.LoadAsync(
            "de",
            "app",
            "form",
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.NotNull(result.Context);
        Assert.True(result.Context!.VoiceSettings.GetAllowBargeIn());
    }

    [Fact]
    public async Task LoadAsync_WhenDeviceAndApplicationOverrideExist_DeviceOverrideWins()
    {
        var catalogClient = new FixedFormsCatalogDataClient(CreateCatalog(new VoiceDialogSettings
        {
            AllowBargeIn = false
        }));
        var contractClient = new FixedMdaFormContractDataClient(CreateContractWithField());
        var loader = CreateLoader(
            catalogClient,
            contractClient,
            new UserLanguageState(),
            CreateOverrideStore(deviceAllowBargeInOverride: false, applicationAllowBargeInOverride: true));

        var result = await loader.LoadAsync(
            "de",
            "app",
            "form",
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.NotNull(result.Context);
        Assert.False(result.Context!.VoiceSettings.GetAllowBargeIn());
    }

    [Fact]
    public async Task LoadAsync_WhenContractFormVoiceProfileOverridesAllowBargeIn_UsesContractValue()
    {
        var catalogClient = new FixedFormsCatalogDataClient(CreateCatalog(new VoiceDialogSettings
        {
            AllowBargeIn = false
        }));
        var contractClient = new FixedMdaFormContractDataClient(CreateContractWithField(formVoiceProfile: new FormVoiceProfile
        {
            AllowBargeIn = true
        }));
        var loader = CreateLoader(
            catalogClient,
            contractClient,
            new UserLanguageState(),
            CreateOverrideStore(deviceAllowBargeInOverride: false, applicationAllowBargeInOverride: false));

        var result = await loader.LoadAsync(
            "de",
            "app",
            "form",
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.NotNull(result.Context);
        Assert.True(result.Context!.VoiceSettings.GetAllowBargeIn());
    }

    [Fact]
    public async Task LoadAsync_WhenContractFormVoiceProfileOverridesRuntimeSettings_UsesContractValues()
    {
        var catalogClient = new FixedFormsCatalogDataClient(CreateCatalog(
            catalogVoiceDialogSettings: new VoiceDialogSettings
            {
                NoInputTimeoutMs = 10000,
                MaxRetries = 3,
                SpeechRate = 1.0,
                AllowBargeIn = false
            },
            applicationVoiceDialogSettings: new VoiceDialogSettings
            {
                NoInputTimeoutMs = 9000,
                MaxRetries = 4,
                SpeechRate = 0.9,
                AllowBargeIn = false
            }));
        var contractClient = new FixedMdaFormContractDataClient(CreateContractWithField(formVoiceProfile: new FormVoiceProfile
        {
            AllowBargeIn = true,
            NoInputTimeoutMs = 8000,
            NoInputMaxRetries = 2,
            SpeechRate = 1.3
        }));
        var loader = CreateLoader(
            catalogClient,
            contractClient,
            new UserLanguageState(),
            CreateOverrideStore(deviceAllowBargeInOverride: null, applicationAllowBargeInOverride: null));

        var result = await loader.LoadAsync(
            "de",
            "app",
            "form",
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.NotNull(result.Context);
        Assert.Equal(8000, result.Context!.VoiceSettings.GetNoInputTimeoutMs());
        Assert.Equal(2, result.Context.VoiceSettings.GetMaxRetries());
        Assert.Equal(1.3, result.Context.VoiceSettings.GetSpeechRate());
        Assert.True(result.Context.VoiceSettings.GetAllowBargeIn());
    }

    [Fact]
    public async Task LoadAsync_WhenContractFormVoiceProfileConfiguresPauseAfterPrompt_ExposesFormCaptureSettings()
    {
        var catalogClient = new FixedFormsCatalogDataClient(CreateCatalog());
        var contractClient = new FixedMdaFormContractDataClient(CreateContractWithField(formVoiceProfile: new FormVoiceProfile
        {
            PauseAfterPromptMs = 250
        }));
        var loader = CreateLoader(
            catalogClient,
            contractClient,
            new UserLanguageState(),
            CreateOverrideStore(deviceAllowBargeInOverride: null, applicationAllowBargeInOverride: null));

        var result = await loader.LoadAsync(
            "de",
            "app",
            "form",
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.NotNull(result.Context);
        Assert.Equal(250, result.Context!.CaptureSettings.PauseAfterPromptMs);
        Assert.Same(result.Context.VoiceSettings, result.Context.CaptureSettings.VoiceSettings);
    }

    [Fact]
    public async Task LoadAsync_WhenContractVoiceHintsOmitRuntimeOverrides_UsesMergedCatalogAndApplicationSettings()
    {
        var catalogClient = new FixedFormsCatalogDataClient(CreateCatalog(
            catalogVoiceDialogSettings: new VoiceDialogSettings
            {
                NoInputTimeoutMs = 10000,
                MaxRetries = 3,
                SpeechRate = 1.0,
                AllowBargeIn = false
            },
            applicationVoiceDialogSettings: new VoiceDialogSettings
            {
                NoInputTimeoutMs = 7000,
                MaxRetries = 1,
                SpeechRate = 1.2,
                AllowBargeIn = true
            }));
        var contractClient = new FixedMdaFormContractDataClient(CreateContractWithField());
        var loader = CreateLoader(
            catalogClient,
            contractClient,
            new UserLanguageState(),
            CreateOverrideStore(deviceAllowBargeInOverride: null, applicationAllowBargeInOverride: null));

        var result = await loader.LoadAsync(
            "de",
            "app",
            "form",
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.NotNull(result.Context);
        Assert.Equal(7000, result.Context!.VoiceSettings.GetNoInputTimeoutMs());
        Assert.Equal(1, result.Context.VoiceSettings.GetMaxRetries());
        Assert.Equal(1.2, result.Context.VoiceSettings.GetSpeechRate());
        Assert.True(result.Context.VoiceSettings.GetAllowBargeIn());
    }

    [Fact]
    public async Task LoadAsync_WhenWakeWordsAreAvailable_PopulatesContextWakeWordActivationForms()
    {
        var catalogClient = new FixedFormsCatalogDataClient(CreateCatalog());
        var contractClient = new FixedMdaFormContractDataClient(CreateContractWithField());
        var loader = new FormVoiceCommandContextLoader(
            new FormVoiceCommandCatalogSnapshotLoader(
                catalogClient,
                CreateOverrideStore(deviceAllowBargeInOverride: null, applicationAllowBargeInOverride: null),
                NullLogger<FormVoiceCommandCatalogSnapshotLoader>.Instance),
            new FormVoiceCommandContractLoader(
                contractClient,
                NullLogger<FormVoiceCommandContractLoader>.Instance),
            new SharedVariableResolver(),
            new UserLanguageState(),
            new FixedWakeWordProvider(["Okay"]));

        var result = await loader.LoadAsync(
            "de",
            "app",
            "form",
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        Assert.NotNull(result.Context);
        Assert.Equal(["Okay"], result.Context!.WakeWordActivationForms.ToArray());
    }

    private static IVoiceDialogSettingsOverrideStore CreateOverrideStore(
        bool? deviceAllowBargeInOverride,
        bool? applicationAllowBargeInOverride)
    {
        var store = new Mock<IVoiceDialogSettingsOverrideStore>(MockBehavior.Strict);
        store.Setup(s => s.GetDeviceAllowBargeInOverrideAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(deviceAllowBargeInOverride));
        store.Setup(s => s.GetApplicationAllowBargeInOverrideAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(applicationAllowBargeInOverride));
        store.Setup(s => s.GetDeviceAutoAdvanceAfterCommitOverrideAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<bool?>(null));
        store.Setup(s => s.GetApplicationAutoAdvanceAfterCommitOverrideAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<bool?>(null));

        return store.Object;
    }

    private static FormVoiceCommandContextLoader CreateLoader(
        IFormsCatalogDataClient catalogClient,
        IMdaFormContractDataClient contractClient,
        IUserLanguageState userLanguageState,
        IVoiceDialogSettingsOverrideStore overrideStore)
    {
        ArgumentNullException.ThrowIfNull(catalogClient);
        ArgumentNullException.ThrowIfNull(contractClient);
        ArgumentNullException.ThrowIfNull(userLanguageState);
        ArgumentNullException.ThrowIfNull(overrideStore);

        return new FormVoiceCommandContextLoader(
            new FormVoiceCommandCatalogSnapshotLoader(
                catalogClient,
                overrideStore,
                NullLogger<FormVoiceCommandCatalogSnapshotLoader>.Instance),
            new FormVoiceCommandContractLoader(
                contractClient,
                NullLogger<FormVoiceCommandContractLoader>.Instance),
            new SharedVariableResolver(),
            userLanguageState,
            new FixedWakeWordProvider([]));
    }

    private static FormsCatalog CreateCatalog(
        VoiceDialogSettings? catalogVoiceDialogSettings = null,
        VoiceDialogSettings? applicationVoiceDialogSettings = null)
    {
        return new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            VoiceDialogSettings = catalogVoiceDialogSettings,
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "app",
                    DisplayName = new LocalizedText { ["de"] = "App" },
                    Description = new LocalizedText { ["de"] = "App" },
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    VoiceDialogSettings = applicationVoiceDialogSettings,
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("form"),
                            DisplayName = new LocalizedText { ["de"] = "Form" },
                            Description = new LocalizedText { ["de"] = "Form" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        }
                    ]
                }
            ]
        };
    }

    private static FormContract CreateEmptyContract()
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("form"),
            DisplayName = new LocalizedText { ["de"] = "Form" },
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Fields = [],
            FieldGroups = []
        };
    }

    private static FormContract CreateContractWithField()
        => CreateContractWithField(hints: null, formVoiceProfile: null);

    private static FormContract CreateContractWithField(
        VoiceHintsDefinition? hints = null,
        FormVoiceProfile? formVoiceProfile = null)
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("form"),
            DisplayName = new LocalizedText { ["de"] = "Form" },
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Projections = hints is null && formVoiceProfile is null
                ? null
                : new ContractProjections
                {
                    Voice = new VoiceProjection
                    {
                        Hints = hints,
                        FormVoiceProfile = formVoiceProfile
                    }
                },
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("name"),
                    VariableId = VariableId.From("name"),
                    Label = new LocalizedText { ["de"] = "Name" }
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("name") }]
                }
            ]
        };
    }
}
