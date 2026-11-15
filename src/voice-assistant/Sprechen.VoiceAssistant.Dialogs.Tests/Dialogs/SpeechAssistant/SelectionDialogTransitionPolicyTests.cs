using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SelectionDialogTransitionPolicyTests
{
    [Fact]
    public async Task FormSelectionPolicy_WhenPrerequisiteIsMissing_ReturnsListeningWithPrerequisitePrompt()
    {
        var context = new FormSelectionTransitionContext(
            ConfirmationPolicy.Never,
            completedFormIds: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            formNamesById: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["first"] = "First Form"
            },
            CreatePromptContext(
                CreatePrompt(
                    CatalogDialogPromptIds.FormPrerequisitesNotMet,
                    ("en", "{{name}} requires {{missingPrerequisites}} first."))),
            PolitenessLevel.Formal,
            "en");
        var selected = SelectionCandidate<FormInfo>.Create(
            new FormInfo("target", "Target Form", ["first"]),
            "target",
            "Target Form",
            stableNumber: 2,
            SelectionRevision.Initial);

        var result = await new FormSelectionTransitionPolicy(new SelectionConfirmationPolicyEvaluator())
            .EvaluateAsync(
                selected,
                SelectionCandidateResolutionKind.ExactName,
                context,
                CancellationToken.None);

        Assert.Equal(SelectionDialogPhase.Listening, result.NextPhase);
        Assert.Equal(SelectionDialogCandidateTransitionKind.InitialReset, result.CandidateTransition.Kind);
        Assert.Null(result.CandidateTransition.Candidate);
        Assert.NotNull(result.SpokenPrompt);
        Assert.Contains("First Form", result.SpokenPrompt.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FormsCatalogPolicy_WhenConfirmationIsRequired_ReturnsConfirmingWithSelectedApplication()
    {
        var promptContext = CreatePromptContext();
        var selectedItem = new ApplicationInfo("app", "Sample App", "Sample", ["tag"]);
        var selected = SelectionCandidate<ApplicationInfo>.Create(
            selectedItem,
            selectedItem.Id,
            selectedItem.DisplayName,
            stableNumber: 1,
            SelectionRevision.Initial);

        var result = await new FormsCatalogSelectionTransitionPolicy(new SelectionConfirmationPolicyEvaluator())
            .EvaluateAsync(
                selected,
                SelectionCandidateResolutionKind.ExactName,
                ConfirmationPolicy.Always,
                promptContext,
                PolitenessLevel.Formal,
                "en",
                CancellationToken.None);

        Assert.Equal(SelectionDialogPhase.Confirming, result.NextPhase);
        Assert.Equal(SelectionDialogCandidateTransitionKind.UniqueSelection, result.CandidateTransition.Kind);
        Assert.Same(selected, result.SelectedCandidate);
        Assert.Same(selectedItem, result.CandidateTransition.Candidate?.Item);
        Assert.Equal(SelectionCandidateResolutionKind.ExactName, result.CandidateTransition.ResolutionKind);
        Assert.Null(result.SpokenPrompt);
    }

    [Theory]
    [InlineData(ConfirmationPolicy.Never)]
    [InlineData(ConfirmationPolicy.Ambiguous)]
    public async Task FormsCatalogPolicy_WhenStableNumberIsDeterministic_LaunchesWithoutConfirmation(
        ConfirmationPolicy confirmationPolicy)
    {
        var selected = CreateApplicationCandidate();

        var result = await new FormsCatalogSelectionTransitionPolicy(new SelectionConfirmationPolicyEvaluator())
            .EvaluateAsync(
                selected,
                SelectionCandidateResolutionKind.StableNumber,
                confirmationPolicy,
                CreatePromptContext(),
                PolitenessLevel.Formal,
                "en",
                CancellationToken.None);

        Assert.Equal(SelectionDialogPhase.Launch, result.NextPhase);
        Assert.Equal(SelectionDialogCandidateTransitionKind.UniqueSelection, result.CandidateTransition.Kind);
        Assert.Same(selected, result.CandidateTransition.Candidate);
        Assert.Equal(SelectionCandidateResolutionKind.StableNumber, result.CandidateTransition.ResolutionKind);
        Assert.Null(result.SpokenPrompt);
    }

    [Theory]
    [InlineData(ConfirmationPolicy.Never)]
    [InlineData(ConfirmationPolicy.Ambiguous)]
    public async Task FormSelectionPolicy_WhenStableNumberIsDeterministic_LaunchesWithoutConfirmation(
        ConfirmationPolicy confirmationPolicy)
    {
        var selected = CreateFormCandidate();
        var context = CreateFormTransitionContext(confirmationPolicy);

        var result = await new FormSelectionTransitionPolicy(new SelectionConfirmationPolicyEvaluator())
            .EvaluateAsync(
                selected,
                SelectionCandidateResolutionKind.StableNumber,
                context,
                CancellationToken.None);

        Assert.Equal(SelectionDialogPhase.Launch, result.NextPhase);
        Assert.Equal(SelectionDialogCandidateTransitionKind.UniqueSelection, result.CandidateTransition.Kind);
        Assert.Same(selected, result.CandidateTransition.Candidate);
        Assert.Equal(SelectionCandidateResolutionKind.StableNumber, result.CandidateTransition.ResolutionKind);
        Assert.Null(result.SpokenPrompt);
    }

    [Theory]
    [InlineData(SelectionCandidateResolutionKind.PartialName)]
    [InlineData(SelectionCandidateResolutionKind.Identifier)]
    public async Task FormsCatalogPolicy_WhenResolutionIsUncertain_ConfirmsEvenWhenPolicyNever(
        SelectionCandidateResolutionKind resolutionKind)
    {
        var selected = CreateApplicationCandidate();
        var promptContext = CreatePromptContext();

        var result = await new FormsCatalogSelectionTransitionPolicy(new SelectionConfirmationPolicyEvaluator())
            .EvaluateAsync(
                selected,
                resolutionKind,
                ConfirmationPolicy.Never,
                promptContext,
                PolitenessLevel.Formal,
                "en",
                CancellationToken.None);

        Assert.Equal(SelectionDialogPhase.Confirming, result.NextPhase);
        Assert.Equal(SelectionDialogCandidateTransitionKind.UniqueSelection, result.CandidateTransition.Kind);
        Assert.Same(selected, result.CandidateTransition.Candidate);
        Assert.Equal(resolutionKind, result.CandidateTransition.ResolutionKind);
        Assert.Null(result.SpokenPrompt);
    }

    [Theory]
    [InlineData(SelectionCandidateResolutionKind.PartialName)]
    [InlineData(SelectionCandidateResolutionKind.Identifier)]
    public async Task FormSelectionPolicy_WhenResolutionIsUncertain_ConfirmsEvenWhenPolicyNever(
        SelectionCandidateResolutionKind resolutionKind)
    {
        var selected = CreateFormCandidate();
        var context = CreateFormTransitionContext(ConfirmationPolicy.Never);

        var result = await new FormSelectionTransitionPolicy(new SelectionConfirmationPolicyEvaluator())
            .EvaluateAsync(
                selected,
                resolutionKind,
                context,
                CancellationToken.None);

        Assert.Equal(SelectionDialogPhase.Confirming, result.NextPhase);
        Assert.Equal(SelectionDialogCandidateTransitionKind.UniqueSelection, result.CandidateTransition.Kind);
        Assert.Same(selected, result.CandidateTransition.Candidate);
        Assert.Equal(resolutionKind, result.CandidateTransition.ResolutionKind);
        Assert.Null(result.SpokenPrompt);
    }

    [Fact]
    public async Task FormSelectionListeningInputRoute_WhenDomainTransitionLaunches_PreservesDecision()
    {
        // Die allgemeine Auswahlregel bewertet die Konfidenz. Diese Route wertet
        // die Eingabe aus und übernimmt den vom Absichtshandler gelieferten Übergang.
        var catalog = FormsCatalogIntentTestCatalog.Create();
        var session = new FormSelectionDialogSessionFactory(new UserLanguageState())
            .Create(catalog, "sample.catalog", "de", []);
        var route = new FormSelectionListeningInputRoute(
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog));
        var matcher = new FormSelectionMatcher().CandidateMatcher;
        var snapshot = matcher.CreateSnapshot(session.Forms, SelectionRevision.Initial);
        var candidates = new SelectionDialogTurnSnapshot<FormInfo>(
            snapshot,
            snapshot,
            activeAmbiguity: null,
            selectedCandidate: null,
            selectedResolutionKind: null);
        var intentHandler = new SelectionDialogCommonIntentHandler<FormInfo>(
            matcher,
            buildListPrompt: _ => SelectionDialogPrompt.FromText("list"),
            buildAmbiguityPrompt: _ => SelectionDialogPrompt.FromText("ambiguous"),
            buildCancelPrompt: () => SelectionDialogPrompt.FromText("cancel"),
            buildHelpPrompt: () => SelectionDialogPrompt.FromText("help"),
            buildInvalidNumberPrompt: _ => SelectionDialogPrompt.FromText("invalid"),
            buildNoMatchPrompt: _ => SelectionDialogPrompt.FromText("no match"),
            transitionAfterSelectionAsync: (selected, resolutionKind, _) => Task.FromResult(
                new SelectionDialogIntentStep<FormInfo>(
                    SelectionDialogPhase.Launch,
                    SelectionDialogCandidateTransition<FormInfo>.SelectUnique(selected, resolutionKind),
                    spokenPrompt: null)));

        var result = await route.ProcessAsync(
            session,
            intentHandler,
            "Formular",
            candidates,
            lastPrompt: null,
            CancellationToken.None);

        Assert.Equal(SelectionDialogPhase.Launch, result.NextPhase);
        Assert.Equal(SelectionDialogCandidateTransitionKind.UniqueSelection, result.CandidateTransition.Kind);
        Assert.NotNull(result.CandidateTransition.Candidate);
        Assert.Null(result.SpokenPrompt);
    }

    private static CatalogPromptContext CreatePromptContext(params CatalogVoicePrompt[] prompts)
    {
        var application = new ApplicationDescriptor
        {
            ApplicationId = "sample.app",
            DisplayName = new LocalizedText { ["en"] = "Sample App" },
            Description = new LocalizedText { ["en"] = "Sample App" },
            SupportedLanguages = ["en"],
            DefaultLanguage = "en",
            Forms =
            [
                new FormDescriptor
                {
                    FormId = FormId.From("form-1"),
                    DisplayName = new LocalizedText { ["en"] = "Form 1" },
                    Description = new LocalizedText { ["en"] = "Form 1" },
                    LatestContractVersion = "1",
                    AvailableContractVersions = ["1"]
                }
            ]
        };
        var catalog = new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            SelectionDialog = new ApplicationSelectionDialog
            {
                SchemaVersion = "1",
                ModelVersion = "1",
                SelectionDialogId = "default.selectionDialog",
                SupportedLanguages = ["en"],
                DefaultLanguage = "en",
                StrictLocalization = true,
                Prompts = [.. prompts]
            },
            Applications = [application]
        };

        return CatalogPromptContext.CreateForApplication(catalog, application);
    }

    private static FormSelectionTransitionContext CreateFormTransitionContext(
        ConfirmationPolicy confirmationPolicy,
        params CatalogVoicePrompt[] prompts)
        => new(
            confirmationPolicy,
            completedFormIds: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            formNamesById: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            CreatePromptContext(prompts),
            PolitenessLevel.Formal,
            "en");

    private static SelectionCandidate<ApplicationInfo> CreateApplicationCandidate()
    {
        var item = new ApplicationInfo("app", "Sample App", "Sample", ["tag"]);
        return SelectionCandidate<ApplicationInfo>.Create(
            item,
            item.Id,
            item.DisplayName,
            stableNumber: 3,
            SelectionRevision.Initial);
    }

    private static SelectionCandidate<FormInfo> CreateFormCandidate()
    {
        var item = new FormInfo("form-1", "Form 1", []);
        return SelectionCandidate<FormInfo>.Create(
            item,
            item.Id,
            item.DisplayName,
            stableNumber: 4,
            SelectionRevision.Initial);
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
