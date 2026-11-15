using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormsCatalogDialogTurnContextTests
{
    [Fact]
    public void Constructor_WhenSelectionIsNull_Throws()
    {
        var parts = CreateParts();

        Assert.Throws<ArgumentNullException>(() => new FormsCatalogDialogTurnContext(
            parts.IntentHandler,
            parts.Catalog,
            "correlation",
            selection: null!,
            parts.PromptRuntime));
    }

    [Fact]
    public void PromptRuntime_WhenIsoCodeIsMissing_Throws()
    {
        var parts = CreateParts();

        Assert.Throws<ArgumentException>(() => new FormsCatalogDialogPromptRuntime(
            parts.PromptContext,
            parts.InteractionSettings,
            PolitenessLevel.Formal,
            " ",
            noInputTimeoutMs: 1000,
            maxRetries: 2));
    }

    [Fact]
    public void Constructor_WhenValid_PreservesTurnParts()
    {
        var parts = CreateParts();

        var turnContext = new FormsCatalogDialogTurnContext(
            parts.IntentHandler,
            parts.Catalog,
            "correlation",
            parts.Selection,
            parts.PromptRuntime);

        Assert.Same(parts.IntentHandler, turnContext.IntentHandler);
        Assert.Same(parts.Catalog, turnContext.Catalog);
        Assert.Same(parts.Selection.Candidates, turnContext.Candidates);
        Assert.Same(parts.Selection.Candidates.InitialCandidates, turnContext.InitialCandidates);
        Assert.Same(parts.Selection.Candidates.VisibleCandidates, turnContext.VisibleCandidates);
        Assert.Equal(parts.Selection.Applications, turnContext.Applications);
        Assert.Equal(parts.Selection.VisibleApplications, turnContext.VisibleApplications);
        Assert.Same(parts.PromptContext, turnContext.PromptContext);
        Assert.Same(parts.InteractionSettings, turnContext.InteractionSettings);
        Assert.Equal("de", turnContext.IsoCode);
        Assert.Equal(1000, turnContext.NoInputTimeoutMs);
        Assert.Equal(2, turnContext.MaxRetries);
    }

    private static (
        SelectionDialogCommonIntentHandler<ApplicationInfo> IntentHandler,
        FormsCatalog Catalog,
        FormsCatalogDialogTurnSelection Selection,
        CatalogPromptContext PromptContext,
        SelectionDialogInteractionSettings InteractionSettings,
        FormsCatalogDialogPromptRuntime PromptRuntime) CreateParts()
    {
        var applications = new List<ApplicationInfo>
        {
            new("app", "App", Description: null, Tags: [], FormCount: 1, DefaultFormId: "form")
        };
        var catalog = new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            SelectionDialog = new ApplicationSelectionDialog
            {
                SchemaVersion = "1",
                ModelVersion = "1",
                SelectionDialogId = "selection",
                SupportedLanguages = ["de"],
                DefaultLanguage = "de",
                StrictLocalization = true,
                Prompts = []
            },
            Applications = []
        };
        var promptContext = CatalogPromptContext.CreateForCatalog(catalog);
        var interactionSettings = new SelectionDialogInteractionSettings(1.0d, 0, false, 85, 65);
        var promptRuntime = new FormsCatalogDialogPromptRuntime(
            promptContext,
            interactionSettings,
            PolitenessLevel.Formal,
            "de",
            noInputTimeoutMs: 1000,
            maxRetries: 2);
        var snapshot = new ApplicationMatcher().CandidateMatcher.CreateSnapshot(
            applications,
            SelectionRevision.Initial);
        var candidates = new SelectionDialogTurnSnapshot<ApplicationInfo>(
            snapshot,
            snapshot,
            activeAmbiguity: null,
            selectedCandidate: null,
            selectedResolutionKind: null);

        return (
            CreateIntentHandler(),
            catalog,
            new FormsCatalogDialogTurnSelection(
                candidates,
                lastPrompt: null),
            promptContext,
            interactionSettings,
            promptRuntime);
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
            transitionAfterSelectionAsync: (selection, resolutionKind, _) =>
                Task.FromResult(new SelectionDialogIntentStep<ApplicationInfo>(
                    SelectionDialogPhase.Launch,
                    SelectionDialogCandidateTransition<ApplicationInfo>.SelectUnique(selection, resolutionKind),
                    spokenPrompt: null)));
    }
}
