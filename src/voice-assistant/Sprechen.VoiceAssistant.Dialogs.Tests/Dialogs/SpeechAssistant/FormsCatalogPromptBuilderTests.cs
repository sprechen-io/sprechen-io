using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormsCatalogPromptBuilderTests
{
    [Theory]
    [InlineData("de", "DE-EIN")]
    [InlineData("en", "EN-SINGLE")]
    [InlineData("fr", "FR-UN")]
    [InlineData("es", "ES-UNO")]
    [InlineData("it", "IT-UNO")]
    public void GetAnnouncingPrompt_UsesModelPromptAcrossSupportedLanguages(string isoCode, string expectedPrefix)
    {
        var promptContext = CreatePromptContext(
            CreatePrompt(
                CatalogDialogPromptIds.AppAnnouncingSingle,
                ("de", "DE-EIN {{name}}"),
                ("en", "EN-SINGLE {{name}}"),
                ("fr", "FR-UN {{name}}"),
                ("es", "ES-UNO {{name}}"),
                ("it", "IT-UNO {{name}}")));
        var applications = new[] { CreateCandidate("app-id", "Test App", 1) };

        var result = FormsCatalogPromptBuilder.GetAnnouncingPrompt(
            promptContext,
            applications,
            isoCode,
            PolitenessLevel.Formal);

        Assert.Contains(expectedPrefix, result.Text);
        Assert.Contains("Test App", result.Text);
    }

    [Fact]
    public void GetListPrompt_UsesTemplateParametersFromModel()
    {
        var promptContext = CreatePromptContext(
            CreatePrompt(CatalogDialogPromptIds.AppListIntro, ("en", "intro {{count}}")),
            CreatePrompt(CatalogDialogPromptIds.AppListItem, ("en", "item {{number}} {{name}}")),
            CreatePrompt(CatalogDialogPromptIds.AppListSelectPrompt, ("en", "select")));
        var applications = new[]
        {
            CreateCandidate("alpha", "Alpha", 1),
            CreateCandidate("beta", "Beta", 2)
        };

        var result = FormsCatalogPromptBuilder.GetListPrompt(
            promptContext,
            applications,
            "en",
            PolitenessLevel.Formal);

        Assert.Equal("intro 2", result.IntroText);
        Assert.Equal(["item 1 Alpha", "item 2 Beta"], result.ItemTexts.ToArray());
        Assert.Equal("select", result.TrailingText);
        Assert.Equal("intro 2 item 1 Alpha item 2 Beta select", result.Text);
    }

    [Fact]
    public void GetListPrompt_WithCandidates_PreservesStableGappedNumbersAndFullNames()
    {
        var promptContext = CreatePromptContext(
            CreatePrompt(CatalogDialogPromptIds.AppListIntro, ("en", "intro {{count}}")),
            CreatePrompt(CatalogDialogPromptIds.AppListItem, ("en", "item {{number}} {{name}}")),
            CreatePrompt(CatalogDialogPromptIds.AppListSelectPrompt, ("en", "select")));
        var candidates = new[]
        {
            CreateCandidate("alpha", "Alpha Application", 2),
            CreateCandidate("beta", "Beta Application Extended", 4)
        };

        var result = FormsCatalogPromptBuilder.GetListPrompt(
            promptContext,
            candidates,
            "en",
            PolitenessLevel.Formal);

        Assert.Equal("intro 2", result.IntroText);
        Assert.Equal(
            ["item 2 Alpha Application", "item 4 Beta Application Extended"],
            result.ItemTexts.ToArray());
        Assert.Equal("select", result.TrailingText);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void GetAmbiguityPrompt_WithShortCandidateSet_EnumeratesEveryCandidateWithoutConfirmationCue(
        int candidateCount)
    {
        var promptContext = CreatePromptContext(
            CreatePrompt(CatalogDialogPromptIds.AppAmbiguityShortIntro, ("en", "matches {{count}}")),
            CreatePrompt(CatalogDialogPromptIds.AppListItem, ("en", "item {{number}} {{name}}")),
            CreatePrompt(CatalogDialogPromptIds.AppListSelectPrompt, ("en", "select")),
            CreatePrompt(CatalogDialogPromptIds.AppConfirmation, ("en", "CONFIRM-FIRST {{name}}")),
            CreatePrompt(CatalogDialogPromptIds.AppLaunch, ("en", "LAUNCH-FIRST {{name}}")));
        var candidates = CreateCandidates(candidateCount);

        var result = FormsCatalogPromptBuilder.GetAmbiguityPrompt(
            promptContext,
            candidates,
            "en",
            PolitenessLevel.Formal);

        Assert.Equal($"matches {candidateCount}", result.IntroText);
        Assert.Equal(candidateCount, result.ItemTexts.Length);
        Assert.Equal("select", result.TrailingText);
        foreach (var candidate in candidates)
        {
            Assert.Contains(candidate.DisplayName, result.Text, StringComparison.Ordinal);
            Assert.Contains($"item {candidate.StableNumber}", result.Text, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("CONFIRM-FIRST", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("LAUNCH-FIRST", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void GetAmbiguityPrompt_WithSixCandidates_OnlyOffersModeledRefinementPath()
    {
        var promptContext = CreatePromptContext(
            CreatePrompt(CatalogDialogPromptIds.AppAmbiguityRefineOrList, ("en", "refine {{count}}")),
            CreatePrompt(CatalogDialogPromptIds.AppListItem, ("en", "item {{number}} {{name}}")),
            CreatePrompt(CatalogDialogPromptIds.AppListSelectPrompt, ("en", "select")),
            CreatePrompt(CatalogDialogPromptIds.AppConfirmation, ("en", "CONFIRM-FIRST {{name}}")),
            CreatePrompt(CatalogDialogPromptIds.AppLaunch, ("en", "LAUNCH-FIRST {{name}}")));
        var candidates = CreateCandidates(6);

        var result = FormsCatalogPromptBuilder.GetAmbiguityPrompt(
            promptContext,
            candidates,
            "en",
            PolitenessLevel.Formal);

        Assert.Equal("refine 6", result.Text);
        Assert.Equal("refine 6", result.IntroText);
        Assert.Empty(result.ItemTexts);
        Assert.Null(result.TrailingText);
        Assert.DoesNotContain(candidates[0].DisplayName, result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("CONFIRM-FIRST", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("LAUNCH-FIRST", result.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("de", "DE-INFORMAL")]
    [InlineData("fr", "FR-INFORMAL")]
    [InlineData("es", "ES-INFORMAL")]
    [InlineData("it", "IT-INFORMAL")]
    public void GetListPrompt_UsesInformalPromptVariantAcrossEveryTuVousLanguage(string isoCode, string expectedTrailingText)
    {
        var promptContext = CreatePromptContext(
            CreatePrompt(CatalogDialogPromptIds.AppListIntro, (isoCode, "intro {{count}}")),
            CreatePrompt(CatalogDialogPromptIds.AppListItem, (isoCode, "item {{number}} {{name}}")),
            CreatePrompt(
                CatalogDialogPromptIds.AppListSelectPrompt,
                (isoCode, "FORMAL"),
                ($"{isoCode}@informal", expectedTrailingText)));
        var applications = new[]
        {
            CreateCandidate("alpha", "Alpha", 1)
        };

        var result = FormsCatalogPromptBuilder.GetListPrompt(
            promptContext,
            applications,
            isoCode,
            PolitenessLevel.Informal);

        Assert.Equal(expectedTrailingText, result.TrailingText);
    }

    [Fact]
    public void GetAnnouncingPrompt_ForMultipleApplications_AnnouncesInventoryBeforeSelection()
    {
        var promptContext = CreatePromptContext(
            CreatePrompt(CatalogDialogPromptIds.AppAnnouncingMultiple, ("en", "there are {{count}}")),
            CreatePrompt(CatalogDialogPromptIds.AppListItem, ("en", "item {{number}} {{name}}")),
            CreatePrompt(CatalogDialogPromptIds.AppListSelectPrompt, ("en", "select")));
        var applications = new[]
        {
            CreateCandidate("alpha", "Alpha", 1),
            CreateCandidate("beta", "Beta", 2)
        };

        var result = FormsCatalogPromptBuilder.GetAnnouncingPrompt(
            promptContext,
            applications,
            "en",
            PolitenessLevel.Formal);

        Assert.Equal("there are 2", result.IntroText);
        Assert.Equal(["item 1 Alpha", "item 2 Beta"], result.ItemTexts.ToArray());
        Assert.Equal("select", result.TrailingText);
        Assert.Equal("there are 2 item 1 Alpha item 2 Beta select", result.Text);
    }

    [Fact]
    public void GetSearchResultsPrompt_UsesQueryAndCountFromModel()
    {
        var promptContext = CreatePromptContext(
            CreatePrompt(CatalogDialogPromptIds.AppSearchResultsIntro, ("en", "found {{count}} for {{query}}")),
            CreatePrompt(CatalogDialogPromptIds.AppSearchResultsItem, ("en", "result {{number}} {{name}}")),
            CreatePrompt(CatalogDialogPromptIds.AppListSelectPrompt, ("en", "select")));
        var applications = new[]
        {
            CreateCandidate("alpha", "Alpha", 1),
            CreateCandidate("beta", "Beta", 2)
        };

        var result = FormsCatalogPromptBuilder.GetSearchResultsPrompt(
            promptContext,
            applications,
            "alpha",
            "en",
            PolitenessLevel.Formal);

        Assert.Equal("found 2 for alpha", result.IntroText);
        Assert.Equal(["result 1 Alpha", "result 2 Beta"], result.ItemTexts.ToArray());
        Assert.Equal("select", result.TrailingText);
        Assert.Equal("found 2 for alpha result 1 Alpha result 2 Beta select", result.Text);
    }

    [Fact]
    public void GetSearchResultsPrompt_WithCandidate_PreservesStableNumberAndEndsWithSelectionPrompt()
    {
        var promptContext = CreatePromptContext(
            CreatePrompt(CatalogDialogPromptIds.AppSearchResultsIntro, ("en", "found {{count}} for {{query}}")),
            CreatePrompt(CatalogDialogPromptIds.AppSearchResultsItem, ("en", "result {{number}} {{name}}")),
            CreatePrompt(CatalogDialogPromptIds.AppListSelectPrompt, ("en", "select")));
        var candidates = new[]
        {
            CreateCandidate("beta", "Beta Application Extended", 4)
        };

        var result = FormsCatalogPromptBuilder.GetSearchResultsPrompt(
            promptContext,
            candidates,
            "beta",
            "en",
            PolitenessLevel.Formal);

        Assert.Equal("found 1 for beta", result.IntroText);
        Assert.Equal(["result 4 Beta Application Extended"], result.ItemTexts.ToArray());
        Assert.Equal("select", result.TrailingText);
    }

    [Theory]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(4, 4)]
    [InlineData(5, 5)]
    [InlineData(6, 0)]
    public void GetAnnouncingPrompt_WithCandidates_UsesNormativeAmbiguityContract(
        int candidateCount,
        int expectedSpokenCandidateCount)
    {
        var promptContext = CreatePromptContext(
            CreatePrompt(CatalogDialogPromptIds.AppAnnouncingMultiple, ("en", "announce {{count}}")),
            CreatePrompt(CatalogDialogPromptIds.AppAmbiguityShortIntro, ("en", "matches {{count}}")),
            CreatePrompt(CatalogDialogPromptIds.AppAmbiguityRefineOrList, ("en", "refine {{count}}")),
            CreatePrompt(CatalogDialogPromptIds.AppListItem, ("en", "item {{number}} {{name}}")),
            CreatePrompt(CatalogDialogPromptIds.AppListSelectPrompt, ("en", "select")),
            CreatePrompt(CatalogDialogPromptIds.AppConfirmation, ("en", "CONFIRM-FIRST {{name}}")),
            CreatePrompt(CatalogDialogPromptIds.AppLaunch, ("en", "LAUNCH-FIRST {{name}}")));
        var candidates = CreateCandidates(candidateCount);

        var result = FormsCatalogPromptBuilder.GetAnnouncingPrompt(
            promptContext,
            candidates,
            "en",
            PolitenessLevel.Formal);

        Assert.Equal(expectedSpokenCandidateCount, result.ItemTexts.Length);
        Assert.Equal(candidateCount >= 6 ? $"refine {candidateCount}" : $"announce {candidateCount}", result.IntroText);
        Assert.Equal(candidateCount >= 6 ? null : "select", result.TrailingText);
        Assert.DoesNotContain("CONFIRM-FIRST", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("LAUNCH-FIRST", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void GetConfirmationPrompt_UsesPolitenessVariant()
    {
        var promptContext = CreatePromptContext(
            CreatePrompt(
                CatalogDialogPromptIds.AppConfirmation,
                ("de@formal", "FORMAL {{name}}?"),
                ("de@informal", "INFORMAL {{name}}?")));
        var application = CreateApplication("Test App");

        var formal = FormsCatalogPromptBuilder.GetConfirmationPrompt(
            promptContext,
            application,
            "de",
            PolitenessLevel.Formal);
        var informal = FormsCatalogPromptBuilder.GetConfirmationPrompt(
            promptContext,
            application,
            "de",
            PolitenessLevel.Informal);

        Assert.Equal("FORMAL Test App?", formal.Text);
        Assert.Equal("INFORMAL Test App?", informal.Text);
    }

    [Theory]
    [InlineData("de", PolitenessLevel.Formal, "de@formal", "DE-FORMAL")]
    [InlineData("de", PolitenessLevel.Informal, "de@informal", "DE-INFORMAL")]
    [InlineData("en", PolitenessLevel.Formal, "en", "EN")]
    [InlineData("en", PolitenessLevel.Informal, "en", "EN")]
    [InlineData("fr", PolitenessLevel.Formal, "fr", "FR-FORMAL")]
    [InlineData("fr", PolitenessLevel.Informal, "fr@informal", "FR-INFORMAL")]
    [InlineData("es", PolitenessLevel.Formal, "es", "ES-FORMAL")]
    [InlineData("es", PolitenessLevel.Informal, "es@informal", "ES-INFORMAL")]
    [InlineData("it", PolitenessLevel.Formal, "it", "IT-FORMAL")]
    [InlineData("it", PolitenessLevel.Informal, "it@informal", "IT-INFORMAL")]
    public void SelectionIntegrityPrompts_UseModeledIdsPlaceholdersAndRegister(
        string isoCode,
        PolitenessLevel politenessLevel,
        string registerAwareTextKey,
        string expectedRegisterMarker)
    {
        var neutralMarker = $"{isoCode.ToUpperInvariant()}-NEUTRAL";
        var promptContext = CreatePromptContext(
            CreatePrompt(
                CatalogDialogPromptIds.AppExplicitAuthorization,
                (registerAwareTextKey, $"{expectedRegisterMarker}-AUTH {{{{name}}}} {{{{number}}}}")),
            CreatePrompt(
                CatalogDialogPromptIds.AppVerificationUnavailable,
                (registerAwareTextKey, $"{expectedRegisterMarker}-UNAVAILABLE {{{{name}}}} {{{{number}}}}")),
            CreatePrompt(
                CatalogDialogPromptIds.AppInitialConfirmationHelp,
                (registerAwareTextKey, $"{expectedRegisterMarker}-HELP-INITIAL {{{{name}}}}")),
            CreatePrompt(
                CatalogDialogPromptIds.AppExplicitAuthorizationHelp,
                (registerAwareTextKey, $"{expectedRegisterMarker}-HELP-EXPLICIT {{{{name}}}} {{{{number}}}}")),
            CreatePrompt(
                CatalogDialogPromptIds.AppConfidenceFaultRetryHelp,
                (registerAwareTextKey, $"{expectedRegisterMarker}-HELP-FAULT {{{{name}}}} {{{{number}}}}")),
            CreatePrompt(
                CatalogDialogPromptIds.AppNotOpenedReselect,
                (registerAwareTextKey, $"{expectedRegisterMarker}-RESELECT {{{{name}}}}")),
            CreatePrompt(
                CatalogDialogPromptIds.AppVerificationFailedTerminal,
                (isoCode, $"{neutralMarker}-VERIFICATION-TERMINAL {{{{name}}}}")),
            CreatePrompt(
                CatalogDialogPromptIds.AppRepairExhaustedTerminal,
                (isoCode, $"{neutralMarker}-REPAIR-TERMINAL {{{{name}}}}")),
            CreatePrompt(
                CatalogDialogPromptIds.AppCaptureFailedTerminal,
                (isoCode, $"{neutralMarker}-CAPTURE-TERMINAL")),
            CreatePrompt(
                CatalogDialogPromptIds.AppListIntro,
                (isoCode, $"{neutralMarker}-LIST {{{{count}}}}")),
            CreatePrompt(
                CatalogDialogPromptIds.AppListItem,
                (isoCode, $"{neutralMarker}-ITEM {{{{number}}}} {{{{name}}}}")));
        var retainedCandidates = SelectionCandidateSnapshot<ApplicationInfo>.Create(
            SelectionRevision.Initial,
            [
                CreateCandidate("first-app", "First App", 2),
                CreateCandidate("integrity-app", "Integrity App", 7),
                CreateCandidate("last-app", "Last App", 9)
            ]);
        var candidate = retainedCandidates[1];

        var authorization = FormsCatalogPromptBuilder.GetExplicitAuthorizationPrompt(
            promptContext,
            candidate,
            isoCode,
            politenessLevel);
        var verificationUnavailable = FormsCatalogPromptBuilder.GetVerificationUnavailablePrompt(
            promptContext,
            candidate,
            isoCode,
            politenessLevel);
        var initialHelp = FormsCatalogPromptBuilder.GetContextualConfirmationHelpPrompt(
            promptContext,
            candidate,
            SelectionConfirmationCaptureStage.Initial,
            isoCode,
            politenessLevel);
        var explicitHelp = FormsCatalogPromptBuilder.GetContextualConfirmationHelpPrompt(
            promptContext,
            candidate,
            SelectionConfirmationCaptureStage.ExplicitAuthorization,
            isoCode,
            politenessLevel);
        var confidenceFaultHelp = FormsCatalogPromptBuilder.GetContextualConfirmationHelpPrompt(
            promptContext,
            candidate,
            SelectionConfirmationCaptureStage.ConfidenceFaultRetry,
            isoCode,
            politenessLevel);
        var notOpenedReselect = FormsCatalogPromptBuilder.GetNotOpenedReselectPrompt(
            promptContext,
            candidate,
            isoCode,
            politenessLevel);
        var verificationFailedTerminal = FormsCatalogPromptBuilder.GetVerificationFailedTerminalPrompt(
            promptContext,
            candidate,
            isoCode,
            politenessLevel);
        var repairExhaustedTerminal = FormsCatalogPromptBuilder.GetRepairExhaustedTerminalPrompt(
            promptContext,
            candidate,
            isoCode,
            politenessLevel);
        var captureFailedTerminal = FormsCatalogPromptBuilder.GetCaptureFailedTerminalPrompt(
            promptContext,
            isoCode,
            politenessLevel);
        var confirmationList = FormsCatalogPromptBuilder.GetConfirmationListPrompt(
            promptContext,
            retainedCandidates,
            authorization,
            isoCode,
            politenessLevel);

        Assert.Equal($"{expectedRegisterMarker}-AUTH Integrity App 7", authorization.Text);
        Assert.Equal($"{expectedRegisterMarker}-UNAVAILABLE Integrity App 7", verificationUnavailable.Text);
        Assert.Equal($"{expectedRegisterMarker}-HELP-INITIAL Integrity App", initialHelp.Text);
        Assert.Equal($"{expectedRegisterMarker}-HELP-EXPLICIT Integrity App 7", explicitHelp.Text);
        Assert.Equal($"{expectedRegisterMarker}-HELP-FAULT Integrity App 7", confidenceFaultHelp.Text);
        Assert.Equal($"{expectedRegisterMarker}-RESELECT Integrity App", notOpenedReselect.Text);
        Assert.Equal($"{neutralMarker}-VERIFICATION-TERMINAL Integrity App", verificationFailedTerminal.Text);
        Assert.Equal($"{neutralMarker}-REPAIR-TERMINAL Integrity App", repairExhaustedTerminal.Text);
        Assert.Equal($"{neutralMarker}-CAPTURE-TERMINAL", captureFailedTerminal.Text);
        Assert.Equal($"{neutralMarker}-LIST 3", confirmationList.IntroText);
        Assert.Equal(
            [
                $"{neutralMarker}-ITEM 2 First App",
                $"{neutralMarker}-ITEM 7 Integrity App",
                $"{neutralMarker}-ITEM 9 Last App"
            ],
            confirmationList.ItemTexts.ToArray());
        Assert.Equal(authorization.Text, confirmationList.TrailingText);
        Assert.Equal(
            $"{neutralMarker}-LIST 3 {neutralMarker}-ITEM 2 First App " +
            $"{neutralMarker}-ITEM 7 Integrity App " +
            $"{neutralMarker}-ITEM 9 Last App {authorization.Text}",
            confirmationList.Text);
    }

    [Fact]
    public void StaticPromptMethods_FailClosedWhenModelPromptMissing()
    {
        var promptContext = CreatePromptContext();

        Assert.Throws<ModeledVoiceResourcePromptException>(
            () => FormsCatalogPromptBuilder.GetNoInputPrompt(promptContext, "de", PolitenessLevel.Formal));
        Assert.Throws<ModeledVoiceResourcePromptException>(
            () => FormsCatalogPromptBuilder.GetNoApplicationsPrompt(promptContext, "en", PolitenessLevel.Formal));
        Assert.Throws<ModeledVoiceResourcePromptException>(
            () => FormsCatalogPromptBuilder.GetNoSearchResultsPrompt(promptContext, "query", "fr", PolitenessLevel.Formal));
    }

    [Fact]
    public void GetNoMatchPrompt_InterpolatesNameFromModel()
    {
        var promptContext = CreatePromptContext(
            CreatePrompt(CatalogDialogPromptIds.AppNoMatch, ("en", "missing {{name}}")));

        var result = FormsCatalogPromptBuilder.GetNoMatchPrompt(
            promptContext,
            "Unknown App",
            "en",
            PolitenessLevel.Formal);

        Assert.Equal("missing Unknown App", result.Text);
    }

    [Fact]
    public void CategoryOwners_UseModelPromptResolverDirectly()
    {
        var promptContext = CreatePromptContext(
            CreatePrompt(CatalogDialogPromptIds.AppListIntro, ("en", "intro {{count}}")),
            CreatePrompt(CatalogDialogPromptIds.AppListItem, ("en", "item {{number}} {{name}}")),
            CreatePrompt(CatalogDialogPromptIds.AppListSelectPrompt, ("en", "select")),
            CreatePrompt(CatalogDialogPromptIds.AppSearchResultsIntro, ("en", "found {{query}}")),
            CreatePrompt(CatalogDialogPromptIds.AppSearchResultsItem, ("en", "result {{number}} {{name}}")),
            CreatePrompt(CatalogDialogPromptIds.AppHelp, ("en", "help text")));
        var applications = new[]
        {
            CreateCandidate("alpha", "Alpha", 1)
        };

        var inventoryPrompt = new FormsCatalogInventoryPromptBuilder().GetListPrompt(
            promptContext,
            applications,
            "en",
            PolitenessLevel.Formal);
        var searchPrompt = new FormsCatalogSearchPromptBuilder().GetSearchResultsPrompt(
            promptContext,
            applications,
            "alpha",
            "en",
            PolitenessLevel.Formal);
        var commonPrompt = new SelectionCommonIntentPromptBuilder<ApplicationInfo>(
            FormsCatalogPromptBuilder.CommonIntentPromptIds,
            app => app.DisplayName).GetHelpPrompt(
            promptContext,
            "en",
            PolitenessLevel.Formal);

        Assert.Equal("intro 1 item 1 Alpha select", inventoryPrompt.Text);
        Assert.Equal("found alpha result 1 Alpha select", searchPrompt.Text);
        Assert.Equal("help text", commonPrompt.Text);
    }

    private static ApplicationInfo CreateApplication(string displayName)
    {
        return new ApplicationInfo("app-id", displayName, "desc", []);
    }

    private static SelectionCandidate<ApplicationInfo> CreateCandidate(
        string identifier,
        string displayName,
        int stableNumber)
    {
        var application = new ApplicationInfo(identifier, displayName, "desc", []);
        return SelectionCandidate<ApplicationInfo>.Create(
            application,
            identifier,
            displayName,
            stableNumber,
            SelectionRevision.Initial);
    }

    private static IReadOnlyList<SelectionCandidate<ApplicationInfo>> CreateCandidates(int count)
    {
        var stableNumbers = new[] { 2, 4, 7, 9, 11, 13 };
        return Enumerable.Range(0, count)
            .Select(index => CreateCandidate(
                $"app-{index + 1}",
                $"Application {index + 1} Full Name",
                stableNumbers[index]))
            .ToArray();
    }

    private static CatalogPromptContext CreatePromptContext(params CatalogVoicePrompt[] prompts)
    {
        var catalog = new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            SelectionDialog = new ApplicationSelectionDialog
            {
                SchemaVersion = "1",
                ModelVersion = "1",
                SelectionDialogId = "default.selectionDialog",
                SupportedLanguages = ["de", "en", "fr", "es", "it"],
                DefaultLanguage = "de",
                StrictLocalization = true,
                Prompts = [.. prompts]
            },
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "sample.app",
                    DisplayName = new LocalizedText { ["en"] = "Sample" },
                    Description = new LocalizedText { ["en"] = "Sample app" },
                    SupportedLanguages = ["de", "en", "fr", "es", "it"],
                    DefaultLanguage = "de",
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("form-1"),
                            DisplayName = new LocalizedText { ["en"] = "Form 1" },
                            Description = new LocalizedText { ["en"] = "Description" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        }
                    ]
                }
            ]
        };

        return CatalogPromptContext.CreateForCatalog(catalog);
    }

    private static CatalogVoicePrompt CreatePrompt(string promptId, params (string LanguageCode, string Text)[] texts)
    {
        var localizedText = new LocalizedText();
        foreach (var (languageCode, text) in texts)
        {
            localizedText[languageCode] = text;
        }

        return new CatalogVoicePrompt
        {
            PromptId = promptId,
            Text = localizedText
        };
    }
}
