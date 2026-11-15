using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormSelectionPromptBuilderTests
{
    [Theory]
    [InlineData("de", "DE-LISTE 2")]
    [InlineData("en", "EN-LIST 2")]
    [InlineData("fr", "FR-LISTE 2")]
    [InlineData("es", "ES-LISTA 2")]
    [InlineData("it", "IT-LISTA 2")]
    public void GetListPrompt_UsesModelPromptAcrossSupportedLanguages(string isoCode, string expectedIntro)
    {
        var promptContext = CreatePromptContext(
            CreatePrompt(
                CatalogDialogPromptIds.FormListIntro,
                ("de", "DE-LISTE {{count}}"),
                ("en", "EN-LIST {{count}}"),
                ("fr", "FR-LISTE {{count}}"),
                ("es", "ES-LISTA {{count}}"),
                ("it", "IT-LISTA {{count}}")),
            CreatePrompt(
                CatalogDialogPromptIds.FormListItem,
                ("de", "DE-ELEM {{number}} {{name}}"),
                ("en", "EN-ITEM {{number}} {{name}}"),
                ("fr", "FR-ELEM {{number}} {{name}}"),
                ("es", "ES-ELEM {{number}} {{name}}"),
                ("it", "IT-ELEM {{number}} {{name}}")),
            CreatePrompt(
                CatalogDialogPromptIds.FormListSelectPrompt,
                ("de", "DE-WAHL"),
                ("en", "EN-SELECT"),
                ("fr", "FR-CHOIX"),
                ("es", "ES-SELEC"),
                ("it", "IT-SCELTA")));

        var forms = new[]
        {
            CreateCandidate("a", "Alpha", 1),
            CreateCandidate("b", "Beta", 2)
        };

        var prompt = FormSelectionPromptBuilder.GetListPrompt(promptContext, forms, isoCode, PolitenessLevel.Formal);

        Assert.Contains(expectedIntro, prompt.IntroText ?? string.Empty);
        Assert.Contains("Alpha", prompt.Text);
        Assert.Contains("Beta", prompt.Text);
        Assert.Equal(2, prompt.ItemTexts.Length);
        Assert.NotNull(prompt.TrailingText);
    }

    [Fact]
    public void GetListPrompt_WithCandidates_PreservesStableGappedNumbersAndFullNames()
    {
        var promptContext = CreatePromptContext(
            CreatePrompt(CatalogDialogPromptIds.FormListIntro, ("en", "intro {{count}}")),
            CreatePrompt(CatalogDialogPromptIds.FormListItem, ("en", "item {{number}} {{name}}")),
            CreatePrompt(CatalogDialogPromptIds.FormListSelectPrompt, ("en", "select")));
        var candidates = new[]
        {
            CreateCandidate("alpha", "Alpha Form", 2),
            CreateCandidate("beta", "Beta Form Extended", 4)
        };

        var prompt = FormSelectionPromptBuilder.GetListPrompt(
            promptContext,
            candidates,
            "en",
            PolitenessLevel.Formal);

        Assert.Equal("intro 2", prompt.IntroText);
        Assert.Equal(["item 2 Alpha Form", "item 4 Beta Form Extended"], prompt.ItemTexts.ToArray());
        Assert.Equal("select", prompt.TrailingText);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void GetAmbiguityPrompt_WithShortCandidateSet_EnumeratesEveryCandidateWithoutConfirmationCue(
        int candidateCount)
    {
        var promptContext = CreatePromptContext(
            CreatePrompt(CatalogDialogPromptIds.FormAmbiguityShortIntro, ("en", "matches {{count}}")),
            CreatePrompt(CatalogDialogPromptIds.FormListItem, ("en", "item {{number}} {{name}}")),
            CreatePrompt(CatalogDialogPromptIds.FormListSelectPrompt, ("en", "select")),
            CreatePrompt(CatalogDialogPromptIds.FormConfirmation, ("en", "CONFIRM-FIRST {{name}}")),
            CreatePrompt(CatalogDialogPromptIds.FormLaunch, ("en", "LAUNCH-FIRST {{name}}")));
        var candidates = CreateCandidates(candidateCount);

        var prompt = FormSelectionPromptBuilder.GetAmbiguityPrompt(
            promptContext,
            candidates,
            "en",
            PolitenessLevel.Formal);

        Assert.Equal($"matches {candidateCount}", prompt.IntroText);
        Assert.Equal(candidateCount, prompt.ItemTexts.Length);
        Assert.Equal("select", prompt.TrailingText);
        foreach (var candidate in candidates)
        {
            Assert.Contains(candidate.DisplayName, prompt.Text, StringComparison.Ordinal);
            Assert.Contains($"item {candidate.StableNumber}", prompt.Text, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("CONFIRM-FIRST", prompt.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("LAUNCH-FIRST", prompt.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void GetAmbiguityPrompt_WithSixCandidates_OnlyOffersModeledRefinementPath()
    {
        var promptContext = CreatePromptContext(
            CreatePrompt(CatalogDialogPromptIds.FormAmbiguityRefineOrList, ("en", "refine {{count}}")),
            CreatePrompt(CatalogDialogPromptIds.FormListItem, ("en", "item {{number}} {{name}}")),
            CreatePrompt(CatalogDialogPromptIds.FormListSelectPrompt, ("en", "select")),
            CreatePrompt(CatalogDialogPromptIds.FormConfirmation, ("en", "CONFIRM-FIRST {{name}}")),
            CreatePrompt(CatalogDialogPromptIds.FormLaunch, ("en", "LAUNCH-FIRST {{name}}")));
        var candidates = CreateCandidates(6);

        var prompt = FormSelectionPromptBuilder.GetAmbiguityPrompt(
            promptContext,
            candidates,
            "en",
            PolitenessLevel.Formal);

        Assert.Equal("refine 6", prompt.Text);
        Assert.Equal("refine 6", prompt.IntroText);
        Assert.Empty(prompt.ItemTexts);
        Assert.Null(prompt.TrailingText);
        Assert.DoesNotContain(candidates[0].DisplayName, prompt.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("CONFIRM-FIRST", prompt.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("LAUNCH-FIRST", prompt.Text, StringComparison.Ordinal);
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
            CreatePrompt(CatalogDialogPromptIds.FormAnnouncing, ("en", "announce {{count}}")),
            CreatePrompt(CatalogDialogPromptIds.FormAmbiguityShortIntro, ("en", "matches {{count}}")),
            CreatePrompt(CatalogDialogPromptIds.FormAmbiguityRefineOrList, ("en", "refine {{count}}")),
            CreatePrompt(CatalogDialogPromptIds.FormListItem, ("en", "item {{number}} {{name}}")),
            CreatePrompt(CatalogDialogPromptIds.FormListSelectPrompt, ("en", "select")),
            CreatePrompt(CatalogDialogPromptIds.FormConfirmation, ("en", "CONFIRM-FIRST {{name}}")),
            CreatePrompt(CatalogDialogPromptIds.FormLaunch, ("en", "LAUNCH-FIRST {{name}}")));
        var candidates = CreateCandidates(candidateCount);

        var prompt = FormSelectionPromptBuilder.GetAnnouncingPrompt(
            promptContext,
            candidates,
            "en",
            PolitenessLevel.Formal);

        Assert.Equal(expectedSpokenCandidateCount, prompt.ItemTexts.Length);
        Assert.Equal(candidateCount >= 6 ? $"refine {candidateCount}" : $"announce {candidateCount}", prompt.IntroText);
        Assert.Equal(candidateCount >= 6 ? null : "select", prompt.TrailingText);
        Assert.DoesNotContain("CONFIRM-FIRST", prompt.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("LAUNCH-FIRST", prompt.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("de", "DE-INFORMAL")]
    [InlineData("fr", "FR-INFORMAL")]
    [InlineData("es", "ES-INFORMAL")]
    [InlineData("it", "IT-INFORMAL")]
    public void GetListPrompt_UsesInformalPromptVariantAcrossEveryTuVousLanguage(string isoCode, string expectedTrailingText)
    {
        var promptContext = CreatePromptContext(
            CreatePrompt(CatalogDialogPromptIds.FormListIntro, (isoCode, "intro {{count}}")),
            CreatePrompt(CatalogDialogPromptIds.FormListItem, (isoCode, "item {{number}} {{name}}")),
            CreatePrompt(
                CatalogDialogPromptIds.FormListSelectPrompt,
                (isoCode, "FORMAL"),
                ($"{isoCode}@informal", expectedTrailingText)));

        var forms = new[]
        {
            CreateCandidate("a", "Alpha", 1)
        };

        var prompt = FormSelectionPromptBuilder.GetListPrompt(promptContext, forms, isoCode, PolitenessLevel.Informal);

        Assert.Equal(expectedTrailingText, prompt.TrailingText);
    }

    [Fact]
    public void GetConfirmationRepeatPrompt_UsesPolitenessVariant()
    {
        var promptContext = CreatePromptContext(
            CreatePrompt(
                CatalogDialogPromptIds.FormConfirmationRepeat,
                ("de@formal", "FORMAL"),
                ("de@informal", "INFORMAL")));

        var formal = FormSelectionPromptBuilder.GetConfirmationRepeatPrompt(promptContext, "de", PolitenessLevel.Formal);
        var informal = FormSelectionPromptBuilder.GetConfirmationRepeatPrompt(promptContext, "de", PolitenessLevel.Informal);

        Assert.Equal("FORMAL", formal.Text);
        Assert.Equal("INFORMAL", informal.Text);
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
                CatalogDialogPromptIds.FormExplicitAuthorization,
                (registerAwareTextKey, $"{expectedRegisterMarker}-AUTH {{{{name}}}} {{{{number}}}}")),
            CreatePrompt(
                CatalogDialogPromptIds.FormVerificationUnavailable,
                (registerAwareTextKey, $"{expectedRegisterMarker}-UNAVAILABLE {{{{name}}}} {{{{number}}}}")),
            CreatePrompt(
                CatalogDialogPromptIds.FormInitialConfirmationHelp,
                (registerAwareTextKey, $"{expectedRegisterMarker}-HELP-INITIAL {{{{name}}}}")),
            CreatePrompt(
                CatalogDialogPromptIds.FormExplicitAuthorizationHelp,
                (registerAwareTextKey, $"{expectedRegisterMarker}-HELP-EXPLICIT {{{{name}}}} {{{{number}}}}")),
            CreatePrompt(
                CatalogDialogPromptIds.FormConfidenceFaultRetryHelp,
                (registerAwareTextKey, $"{expectedRegisterMarker}-HELP-FAULT {{{{name}}}} {{{{number}}}}")),
            CreatePrompt(
                CatalogDialogPromptIds.FormNotOpenedReselect,
                (registerAwareTextKey, $"{expectedRegisterMarker}-RESELECT {{{{name}}}}")),
            CreatePrompt(
                CatalogDialogPromptIds.FormVerificationFailedTerminal,
                (isoCode, $"{neutralMarker}-VERIFICATION-TERMINAL {{{{name}}}}")),
            CreatePrompt(
                CatalogDialogPromptIds.FormRepairExhaustedTerminal,
                (isoCode, $"{neutralMarker}-REPAIR-TERMINAL {{{{name}}}}")),
            CreatePrompt(
                CatalogDialogPromptIds.FormCaptureFailedTerminal,
                (isoCode, $"{neutralMarker}-CAPTURE-TERMINAL")),
            CreatePrompt(
                CatalogDialogPromptIds.FormListIntro,
                (isoCode, $"{neutralMarker}-LIST {{{{count}}}}")),
            CreatePrompt(
                CatalogDialogPromptIds.FormListItem,
                (isoCode, $"{neutralMarker}-ITEM {{{{number}}}} {{{{name}}}}")));
        var retainedCandidates = SelectionCandidateSnapshot<FormInfo>.Create(
            SelectionRevision.Initial,
            [
                CreateCandidate("first-form", "First Form", 2),
                CreateCandidate("integrity-form", "Integrity Form", 7),
                CreateCandidate("last-form", "Last Form", 9)
            ]);
        var candidate = retainedCandidates[1];

        var authorization = FormSelectionPromptBuilder.GetExplicitAuthorizationPrompt(
            promptContext,
            candidate,
            isoCode,
            politenessLevel);
        var verificationUnavailable = FormSelectionPromptBuilder.GetVerificationUnavailablePrompt(
            promptContext,
            candidate,
            isoCode,
            politenessLevel);
        var initialHelp = FormSelectionPromptBuilder.GetContextualConfirmationHelpPrompt(
            promptContext,
            candidate,
            SelectionConfirmationCaptureStage.Initial,
            isoCode,
            politenessLevel);
        var explicitHelp = FormSelectionPromptBuilder.GetContextualConfirmationHelpPrompt(
            promptContext,
            candidate,
            SelectionConfirmationCaptureStage.ExplicitAuthorization,
            isoCode,
            politenessLevel);
        var confidenceFaultHelp = FormSelectionPromptBuilder.GetContextualConfirmationHelpPrompt(
            promptContext,
            candidate,
            SelectionConfirmationCaptureStage.ConfidenceFaultRetry,
            isoCode,
            politenessLevel);
        var notOpenedReselect = FormSelectionPromptBuilder.GetNotOpenedReselectPrompt(
            promptContext,
            candidate,
            isoCode,
            politenessLevel);
        var verificationFailedTerminal = FormSelectionPromptBuilder.GetVerificationFailedTerminalPrompt(
            promptContext,
            candidate,
            isoCode,
            politenessLevel);
        var repairExhaustedTerminal = FormSelectionPromptBuilder.GetRepairExhaustedTerminalPrompt(
            promptContext,
            candidate,
            isoCode,
            politenessLevel);
        var captureFailedTerminal = FormSelectionPromptBuilder.GetCaptureFailedTerminalPrompt(
            promptContext,
            isoCode,
            politenessLevel);
        var confirmationList = FormSelectionPromptBuilder.GetConfirmationListPrompt(
            promptContext,
            retainedCandidates,
            authorization,
            isoCode,
            politenessLevel);

        Assert.Equal($"{expectedRegisterMarker}-AUTH Integrity Form 7", authorization.Text);
        Assert.Equal($"{expectedRegisterMarker}-UNAVAILABLE Integrity Form 7", verificationUnavailable.Text);
        Assert.Equal($"{expectedRegisterMarker}-HELP-INITIAL Integrity Form", initialHelp.Text);
        Assert.Equal($"{expectedRegisterMarker}-HELP-EXPLICIT Integrity Form 7", explicitHelp.Text);
        Assert.Equal($"{expectedRegisterMarker}-HELP-FAULT Integrity Form 7", confidenceFaultHelp.Text);
        Assert.Equal($"{expectedRegisterMarker}-RESELECT Integrity Form", notOpenedReselect.Text);
        Assert.Equal($"{neutralMarker}-VERIFICATION-TERMINAL Integrity Form", verificationFailedTerminal.Text);
        Assert.Equal($"{neutralMarker}-REPAIR-TERMINAL Integrity Form", repairExhaustedTerminal.Text);
        Assert.Equal($"{neutralMarker}-CAPTURE-TERMINAL", captureFailedTerminal.Text);
        Assert.Equal($"{neutralMarker}-LIST 3", confirmationList.IntroText);
        Assert.Equal(
            [
                $"{neutralMarker}-ITEM 2 First Form",
                $"{neutralMarker}-ITEM 7 Integrity Form",
                $"{neutralMarker}-ITEM 9 Last Form"
            ],
            confirmationList.ItemTexts.ToArray());
        Assert.Equal(authorization.Text, confirmationList.TrailingText);
        Assert.Equal(
            $"{neutralMarker}-LIST 3 {neutralMarker}-ITEM 2 First Form " +
            $"{neutralMarker}-ITEM 7 Integrity Form " +
            $"{neutralMarker}-ITEM 9 Last Form {authorization.Text}",
            confirmationList.Text);
    }

    [Fact]
    public void GetPrerequisitesNotMetPrompt_InterpolatesMissingPrerequisites()
    {
        var promptContext = CreatePromptContext(
            CreatePrompt(
                CatalogDialogPromptIds.FormPrerequisitesNotMet,
                ("en", "form {{name}} needs {{missingPrerequisites}}")));
        var form = new FormInfo("target", "Target", ["first", "second"]);

        var prompt = FormSelectionPromptBuilder.GetPrerequisitesNotMetPrompt(
            promptContext,
            form,
            ["Form A", "Form B"],
            "en",
            PolitenessLevel.Formal);

        Assert.Equal("form Target needs Form A, Form B", prompt.Text);
    }

    [Fact]
    public void GetListPrompt_FailsClosedWhenModelPromptMissing()
    {
        var promptContext = CreatePromptContext();
        var forms = new[]
        {
            CreateCandidate("a", "Alpha", 1),
            CreateCandidate("b", "Beta", 2)
        };

        Assert.Throws<ModeledVoiceResourcePromptException>(
            () => FormSelectionPromptBuilder.GetListPrompt(promptContext, forms, "de", PolitenessLevel.Informal));
    }

    [Fact]
    public void CategoryOwners_UseModelPromptResolverDirectly()
    {
        var promptContext = CreatePromptContext(
            CreatePrompt(CatalogDialogPromptIds.FormListIntro, ("en", "intro {{count}}")),
            CreatePrompt(CatalogDialogPromptIds.FormListItem, ("en", "item {{number}} {{name}}")),
            CreatePrompt(CatalogDialogPromptIds.FormListSelectPrompt, ("en", "select")),
            CreatePrompt(CatalogDialogPromptIds.FormHelp, ("en", "help text")));
        var forms = new[]
        {
            CreateCandidate("a", "Alpha", 1)
        };

        var inventoryPrompt = new FormSelectionInventoryPromptBuilder().GetListPrompt(
            promptContext,
            forms,
            "en",
            PolitenessLevel.Formal);
        var commonPrompt = new SelectionCommonIntentPromptBuilder<FormInfo>(
            FormSelectionPromptBuilder.CommonIntentPromptIds,
            form => form.DisplayName).GetHelpPrompt(
            promptContext,
            "en",
            PolitenessLevel.Formal);

        Assert.Equal("intro 1 item 1 Alpha select", inventoryPrompt.Text);
        Assert.Equal("help text", commonPrompt.Text);
    }

    private static SelectionCandidate<FormInfo> CreateCandidate(
        string identifier,
        string displayName,
        int stableNumber)
    {
        var form = new FormInfo(identifier, displayName, []);
        return SelectionCandidate<FormInfo>.Create(
            form,
            identifier,
            displayName,
            stableNumber,
            SelectionRevision.Initial);
    }

    private static IReadOnlyList<SelectionCandidate<FormInfo>> CreateCandidates(int count)
    {
        var stableNumbers = new[] { 2, 4, 7, 9, 11, 13 };
        return Enumerable.Range(0, count)
            .Select(index => CreateCandidate(
                $"form-{index + 1}",
                $"Form {index + 1} Full Name",
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
