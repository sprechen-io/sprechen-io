using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.PromptTemplates;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormsCatalogDialogIntentRouterTests
{
    [Theory]
    [InlineData(PolitenessLevel.Formal, "Sagen Sie")]
    [InlineData(PolitenessLevel.Informal, "Sag")]
    public async Task ProcessIntentAsync_WithGermanHelpUtterance_SpeaksGermanCommandList(
        PolitenessLevel politenessLevel,
        string expectedOpening)
    {
        var applications = CreateApplications();
        var router = CreateRouter();
        var context = CreateTurnContextWithModeledHelpPrompt(applications, politenessLevel);
        var intentParser = new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog);

        var helpIntent = intentParser.Parse(
            context.Catalog,
            "hilfe",
            "de",
            PolitenessLevel.Formal);

        Assert.Equal(FormsCatalogIntent.Help, helpIntent.Intent);

        var result = await router.ProcessIntentAsync(context, helpIntent, CancellationToken.None);

        Assert.NotNull(result.SpokenPrompt);
        var spoken = result.SpokenPrompt!.Text;
        Assert.StartsWith(expectedOpening, spoken, StringComparison.Ordinal);
        Assert.Contains("alle Anwendungen", spoken, StringComparison.Ordinal);
        Assert.Contains("Suche nach", spoken, StringComparison.Ordinal);
        Assert.Contains("Nummer", spoken, StringComparison.Ordinal);
        Assert.Contains("wiederholen", spoken, StringComparison.Ordinal);
        Assert.Contains("abbrechen", spoken, StringComparison.Ordinal);
        Assert.DoesNotContain("You can say", spoken, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProcessIntentAsync_WithSearchResults_ReplacesVisibleCandidateSnapshot()
    {
        var applications = CreateApplications();
        var router = CreateRouter();
        var context = CreateTurnContext(applications);

        var result = await router.ProcessIntentAsync(
            context,
            new FormsCatalogIntentResult(FormsCatalogIntent.Search, Query: "target"),
            CancellationToken.None);

        Assert.Equal(SelectionDialogPhase.Listening, result.NextPhase);
        Assert.Equal(SelectionDialogCandidateTransitionKind.VisibleReplacement, result.CandidateTransition.Kind);
        var replacement = Assert.IsType<SelectionCandidateSnapshot<ApplicationInfo>>(
            result.CandidateTransition.Snapshot);
        var candidate = Assert.Single(replacement.Candidates);
        Assert.Equal("alpha", candidate.Identifier);
        Assert.Equal(1, candidate.StableNumber);
        Assert.Equal(context.Candidates.Revision.Next(), replacement.Revision);
        Assert.NotNull(result.SpokenPrompt);
    }

    [Fact]
    public async Task ProcessIntentAsync_WithSearchWithoutResults_PreservesCandidateState()
    {
        var applications = CreateApplications();
        var router = CreateRouter();
        var context = CreateTurnContext(applications);

        var result = await router.ProcessIntentAsync(
            context,
            new FormsCatalogIntentResult(FormsCatalogIntent.Search, Query: "missing"),
            CancellationToken.None);

        Assert.Equal(SelectionDialogPhase.Listening, result.NextPhase);
        Assert.Equal(SelectionDialogCandidateTransitionKind.Preservation, result.CandidateTransition.Kind);
        Assert.Null(result.CandidateTransition.Snapshot);
        Assert.NotNull(result.SpokenPrompt);
    }

    [Fact]
    public async Task ProcessIntentAsync_WithChangeLanguage_DelegatesToLanguageChangeRoute()
    {
        var applications = CreateApplications();
        string? observedLanguageTarget = null;
        var router = new FormsCatalogDialogIntentRouter(
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            (context, rawLanguageTarget, _) =>
            {
                observedLanguageTarget = rawLanguageTarget;
                return Task.FromResult(FormsCatalogIntentHandlingResult.SilentRestartTerminal("en"));
            });
        var context = CreateTurnContext(applications);

        var result = await router.ProcessIntentAsync(
            context,
            new FormsCatalogIntentResult(FormsCatalogIntent.ChangeLanguage, LanguageArgument: "englisch"),
            CancellationToken.None);

        Assert.Equal("englisch", observedLanguageTarget);
        Assert.Equal(SelectionDialogCompletionReason.RestartRequested, result.CompletionReason);
        Assert.Equal("en", result.RestartIsoCode);
    }

    [Theory]
    [InlineData("hilfe")]
    [InlineData("wiederholen")]
    [InlineData("alle anwendungen")]
    public async Task ProcessPendingLanguageTargetAsync_WithPromptControl_ReissuesLanguageTargetPrompt(
        string transcript)
    {
        const string languageTargetPrompt = "Bitte nennen Sie eine unterstuetzte Sprache.";
        var languageChangeCallCount = 0;
        string? observedLanguageTarget = "not-called";
        var router = new FormsCatalogDialogIntentRouter(
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            (context, rawLanguageTarget, _) =>
            {
                languageChangeCallCount++;
                observedLanguageTarget = rawLanguageTarget;
                return Task.FromResult(new FormsCatalogIntentHandlingResult(
                    SelectionDialogPhase.Listening,
                    SelectionDialogCandidateTransition<ApplicationInfo>.Preserve(),
                    SelectionDialogPrompt.FromText(languageTargetPrompt),
                    ExpectsLanguageTarget: true));
            });
        var context = CreateTurnContext(CreateApplications());

        var result = await router.ProcessPendingLanguageTargetAsync(
            context,
            transcript,
            CancellationToken.None);

        Assert.Equal(SelectionDialogPhase.Listening, result.NextPhase);
        Assert.True(result.ExpectsLanguageTarget);
        Assert.Null(result.CompletionReason);
        Assert.Equal(languageTargetPrompt, result.SpokenPrompt?.Text);
        Assert.Equal(1, languageChangeCallCount);
        Assert.Null(observedLanguageTarget);
    }

    [Fact]
    public async Task ProcessPendingLanguageTargetAsync_WithCancel_CompletesImmediately()
    {
        var router = CreateRouter();
        var context = CreateTurnContext(CreateApplications());

        var result = await router.ProcessPendingLanguageTargetAsync(
            context,
            "abbrechen",
            CancellationToken.None);

        Assert.Equal(SelectionDialogPhase.Completed, result.NextPhase);
        Assert.Equal(SelectionDialogCompletionReason.UserCancelled, result.CompletionReason);
        Assert.False(result.ExpectsLanguageTarget);
        Assert.Equal(SelectionDialogCandidateTransitionKind.TerminalClearance, result.CandidateTransition.Kind);
    }

    private static FormsCatalogDialogIntentRouter CreateRouter()
        => new(
            new FormsCatalogIntentParser(new FormsCatalogIntentMatcher(), CatalogIntentParserFixtures.LanguageTargets, CatalogIntentParserFixtures.LanguageCatalog),
            (context, _, _) => Task.FromResult(new FormsCatalogIntentHandlingResult(
                SelectionDialogPhase.Listening,
                SelectionDialogCandidateTransition<ApplicationInfo>.Preserve(),
                SpokenPrompt: null)));

    private static FormsCatalogDialogTurnContext CreateTurnContext(List<ApplicationInfo> applications)
    {
        var selectionDialog = FormsCatalogIntentTestCatalog.Create().SelectionDialog;
        selectionDialog.Prompts =
        [
            new CatalogVoicePrompt
            {
                PromptId = CatalogDialogPromptIds.AppSearchResultsIntro,
                Text = new LocalizedText { ["de"] = "Gefunden: {{count}} Treffer fuer {{query}}." }
            },
            new CatalogVoicePrompt
            {
                PromptId = CatalogDialogPromptIds.AppSearchResultsItem,
                Text = new LocalizedText { ["de"] = "Nummer {{number}}: {{name}}." }
            },
            new CatalogVoicePrompt
            {
                PromptId = CatalogDialogPromptIds.AppListSelectPrompt,
                Text = new LocalizedText { ["de"] = "Sagen Sie die Nummer oder den Namen." }
            },
            new CatalogVoicePrompt
            {
                PromptId = CatalogDialogPromptIds.AppNoSearchResults,
                Text = new LocalizedText { ["de"] = "Keine Anwendungen fuer '{{query}}' gefunden. Sagen Sie alle Anwendungen." }
            }
        ];
        var catalog = new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            SelectionDialog = selectionDialog
        };
        var promptContext = CatalogPromptContext.CreateForCatalog(catalog);
        var promptRuntime = new FormsCatalogDialogPromptRuntime(
            promptContext,
            new SelectionDialogInteractionSettings(1.0, 0, AllowBargeIn: true, 85, 65),
            PolitenessLevel.Formal,
            "de",
            noInputTimeoutMs: 1000,
            maxRetries: 1);

        return new FormsCatalogDialogTurnContext(
            CreateIntentHandler(),
            catalog,
            "correlation",
            new FormsCatalogDialogTurnSelection(
                CreateTurnSnapshot(applications),
                lastPrompt: null),
            promptRuntime);
    }

    private static FormsCatalogDialogTurnContext CreateTurnContextWithModeledHelpPrompt(
        List<ApplicationInfo> applications,
        PolitenessLevel politenessLevel)
    {
        var selectionDialog = FormsCatalogIntentTestCatalog.Create().SelectionDialog;
        selectionDialog.Prompts =
        [
            new CatalogVoicePrompt
            {
                PromptId = CatalogDialogPromptIds.AppHelp,
                Text = new LocalizedText
                {
                    ["de@formal"] = "Sagen Sie alle Anwendungen, Suche nach, Name, Nummer, wiederholen oder abbrechen.",
                    ["de@informal"] = "Sag alle Anwendungen, Suche nach, Name, Nummer, wiederholen oder abbrechen.",
                    ["en"] = "Say all applications, search for, name, number, repeat or cancel."
                }
            }
        ];

        var catalog = new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            SelectionDialog = selectionDialog
        };
        var promptContext = CatalogPromptContext.CreateForCatalog(catalog);
        var promptRuntime = new FormsCatalogDialogPromptRuntime(
            promptContext,
            new SelectionDialogInteractionSettings(1.0, 0, AllowBargeIn: true, 85, 65),
            politenessLevel,
            "de",
            noInputTimeoutMs: 1000,
            maxRetries: 1);

        return new FormsCatalogDialogTurnContext(
            CreateIntentHandlerWithModeledHelpPrompt(promptContext, politenessLevel),
            catalog,
            "correlation",
            new FormsCatalogDialogTurnSelection(
                CreateTurnSnapshot(applications),
                lastPrompt: null),
            promptRuntime);
    }

    private static SelectionDialogCommonIntentHandler<ApplicationInfo> CreateIntentHandlerWithModeledHelpPrompt(
        CatalogPromptContext promptContext,
        PolitenessLevel politenessLevel)
    {
        var matcher = new ApplicationMatcher().CandidateMatcher;
        return new(
            matcher,
            buildListPrompt: _ => SelectionDialogPrompt.FromText("list"),
            buildAmbiguityPrompt: _ => SelectionDialogPrompt.FromText("ambiguous"),
            buildCancelPrompt: () => SelectionDialogPrompt.FromText("cancel"),
            buildHelpPrompt: () => FormsCatalogPromptBuilder.GetHelpPrompt(promptContext, "de", politenessLevel),
            buildInvalidNumberPrompt: _ => SelectionDialogPrompt.FromText("invalid"),
            buildNoMatchPrompt: _ => SelectionDialogPrompt.FromText("no match"),
            transitionAfterSelectionAsync: (selected, resolutionKind, _) => Task.FromResult(
                new SelectionDialogIntentStep<ApplicationInfo>(
                    SelectionDialogPhase.Launch,
                    SelectionDialogCandidateTransition<ApplicationInfo>.SelectUnique(selected, resolutionKind),
                    spokenPrompt: null)));
    }

    private static SelectionDialogCommonIntentHandler<ApplicationInfo> CreateIntentHandler()
    {
        var matcher = new ApplicationMatcher().CandidateMatcher;
        return new(
            matcher,
            buildListPrompt: _ => SelectionDialogPrompt.FromText("list"),
            buildAmbiguityPrompt: _ => SelectionDialogPrompt.FromText("ambiguous"),
            buildCancelPrompt: () => SelectionDialogPrompt.FromText("cancel"),
            buildHelpPrompt: () => SelectionDialogPrompt.FromText("help"),
            buildInvalidNumberPrompt: _ => SelectionDialogPrompt.FromText("invalid"),
            buildNoMatchPrompt: _ => SelectionDialogPrompt.FromText("no match"),
            transitionAfterSelectionAsync: (selected, resolutionKind, _) => Task.FromResult(
                new SelectionDialogIntentStep<ApplicationInfo>(
                    SelectionDialogPhase.Launch,
                    SelectionDialogCandidateTransition<ApplicationInfo>.SelectUnique(selected, resolutionKind),
                    spokenPrompt: null)));
    }

    private static SelectionDialogTurnSnapshot<ApplicationInfo> CreateTurnSnapshot(
        IReadOnlyList<ApplicationInfo> applications)
    {
        var snapshot = new ApplicationMatcher().CandidateMatcher.CreateSnapshot(
            applications,
            SelectionRevision.Initial);
        return new SelectionDialogTurnSnapshot<ApplicationInfo>(
            snapshot,
            snapshot,
            activeAmbiguity: null,
            selectedCandidate: null,
            selectedResolutionKind: null);
    }

    private static List<ApplicationInfo> CreateApplications()
        =>
        [
            new("alpha", "Alpha", "Alpha description", ["target"]),
            new("beta", "Beta", "Beta description", ["other"])
        ];
}
