using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Mda.Contracts.V1.Preflight;

namespace Sprechen.Mda.Contracts.Tests;

public sealed class FormsCatalogDocumentResolverTests
{
    [Theory]
    [InlineData(95, null, null, 90, 95, 90)]
    [InlineData(null, 50, 55, null, 55, 50)]
    public void Resolve_WhenSparseProfilesSupplyComplementaryConfidenceThresholds_MergesEffectiveSettings(
        int? globalAcceptThreshold,
        int? globalConfirmThreshold,
        int? applicationAcceptThreshold,
        int? applicationConfirmThreshold,
        int expectedAcceptThreshold,
        int expectedConfirmThreshold)
    {
        var globalProfile = CreateProfile(
            "global.selection-dialog-profile",
            globalAcceptThreshold,
            globalConfirmThreshold);
        var applicationProfile = CreateProfile(
            "application.selection-dialog-profile",
            applicationAcceptThreshold,
            applicationConfirmThreshold);
        var document = CreateDocument(globalProfile, applicationProfile);
        var profiles = CreateProfileIndex(globalProfile, applicationProfile);

        var result = new FormsCatalogDocumentResolver().Resolve(
            document,
            CreateSelectionDialog(),
            profiles);

        var applicationSettings = Assert.Single(result.Applications).VoiceDialogSettings;
        Assert.NotNull(applicationSettings);
        Assert.Equal(expectedAcceptThreshold, applicationSettings.SttMinConfidenceToAccept);
        Assert.Equal(expectedConfirmThreshold, applicationSettings.SttMinConfidenceToConfirm);
        Assert.True(FormsCatalogPreflight.Validate(result).Ok);
    }

    [Fact]
    public void Resolve_WhenSparseProfilesProduceInvertedEffectiveThresholds_IsRejectedByResolvedCatalogPreflight()
    {
        var globalProfile = CreateProfile("global.selection-dialog-profile", null, 65);
        var applicationProfile = CreateProfile("application.selection-dialog-profile", 50, null);
        var document = CreateDocument(globalProfile, applicationProfile);
        var profiles = CreateProfileIndex(globalProfile, applicationProfile);

        var result = new FormsCatalogDocumentResolver().Resolve(
            document,
            CreateSelectionDialog(),
            profiles);
        var preflight = FormsCatalogPreflight.Validate(result);

        var applicationSettings = Assert.Single(result.Applications).VoiceDialogSettings;
        Assert.NotNull(applicationSettings);
        Assert.Equal(50, applicationSettings.SttMinConfidenceToAccept);
        Assert.Equal(65, applicationSettings.SttMinConfidenceToConfirm);
        Assert.Contains(preflight.Errors, error =>
            error.Code == PreflightCodes.ErrorSettingsInvalid
            && error.Id == "app1"
            && error.Path == "$.applications[0].voiceDialogSettings.sttMinConfidenceToConfirm");
    }

    [Fact]
    public void Resolve_WhenProfileSetsAutoAdvanceAfterCommit_FlowsIntoEffectiveSettings()
    {
        var globalProfile = CreateProfile("global.selection-dialog-profile", null, null);
        var applicationProfile = CreateProfile("application.selection-dialog-profile", null, null);
        applicationProfile.AutoAdvanceAfterCommit = false;
        var document = CreateDocument(globalProfile, applicationProfile);
        var profiles = CreateProfileIndex(globalProfile, applicationProfile);

        var result = new FormsCatalogDocumentResolver().Resolve(
            document,
            CreateSelectionDialog(),
            profiles);

        var applicationSettings = Assert.Single(result.Applications).VoiceDialogSettings;
        Assert.NotNull(applicationSettings);
        Assert.False(applicationSettings.AutoAdvanceAfterCommit);
    }

    private static FormsCatalogDocument CreateDocument(
        ApplicationSelectionDialogProfile globalProfile,
        ApplicationSelectionDialogProfile applicationProfile)
    {
        return new FormsCatalogDocument
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            SelectionDialog = new ApplicationSelectionDialogReference
            {
                SelectionDialogId = "default.selectionDialog",
                ModelVersion = "1"
            },
            SelectionDialogProfile = CreateProfileReference(globalProfile),
            Applications =
            [
                new ApplicationDescriptorDocument
                {
                    ApplicationId = "app1",
                    DisplayName = new LocalizedText { ["de"] = "App 1" },
                    Description = new LocalizedText { ["de"] = "Description 1" },
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    SelectionDialogProfile = CreateProfileReference(applicationProfile),
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("form1"),
                            DisplayName = new LocalizedText { ["de"] = "Form 1" },
                            Description = new LocalizedText { ["de"] = "Form Description" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        }
                    ]
                }
            ]
        };
    }

    private static ApplicationSelectionDialogProfileReference CreateProfileReference(
        ApplicationSelectionDialogProfile profile)
    {
        return new ApplicationSelectionDialogProfileReference
        {
            ProfileId = profile.ProfileId,
            ProfileVersion = profile.ProfileVersion
        };
    }

    private static Dictionary<ApplicationSelectionDialogProfileKey, ApplicationSelectionDialogProfile> CreateProfileIndex(
        params ApplicationSelectionDialogProfile[] profiles)
    {
        return profiles.ToDictionary(
            profile => new ApplicationSelectionDialogProfileKey(profile.ProfileId, profile.ProfileVersion));
    }

    private static ApplicationSelectionDialogProfile CreateProfile(
        string profileId,
        int? acceptThreshold,
        int? confirmThreshold)
    {
        return new ApplicationSelectionDialogProfile
        {
            SchemaVersion = "1",
            ProfileVersion = "1",
            ProfileId = profileId,
            SttMinConfidenceToAccept = acceptThreshold,
            SttMinConfidenceToConfirm = confirmThreshold
        };
    }

    private static ApplicationSelectionDialog CreateSelectionDialog()
    {
        return new ApplicationSelectionDialog
        {
            SchemaVersion = "1",
            ModelVersion = "1",
            SelectionDialogId = "default.selectionDialog",
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            Prompts =
            [
                new CatalogVoicePrompt
                {
                    PromptId = "app.help",
                    Text = new LocalizedText { ["de"] = "hilfe" }
                },
                CreatePrompt("catalog.app.confirmation.repeat"),
                CreatePrompt("catalog.form.confirmation.repeat"),
                CreatePrompt("catalog.app.reListen"),
                CreatePrompt("catalog.form.reListen")
            ],
            Intents =
            [
                new CatalogVoiceIntent
                {
                    IntentId = CatalogVoiceIntentId.Help,
                    MatchMode = CatalogVoiceIntentMatchMode.Phrase,
                    Utterances = [new LocalizedText { ["de"] = "hilfe" }]
                }
            ],
            NumberWords =
            [
                new CatalogVoiceNumberWord
                {
                    Value = 1,
                    Utterances = [new LocalizedText { ["de"] = "eins" }]
                }
            ]
        };
    }

    private static CatalogVoicePrompt CreatePrompt(string promptId)
    {
        return new CatalogVoicePrompt
        {
            PromptId = promptId,
            Text = new LocalizedText
            {
                ["de@formal"] = "Bitte sagen Sie ja oder nein.",
                ["de@informal"] = "Bitte sag ja oder nein."
            }
        };
    }
}
