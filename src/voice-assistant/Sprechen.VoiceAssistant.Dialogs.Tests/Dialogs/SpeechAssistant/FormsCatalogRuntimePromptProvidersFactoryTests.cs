using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Zuordnung der Bestätigungsarten zu den modellierten Anwendungstexten.
/// </summary>
public sealed class FormsCatalogRuntimePromptProvidersFactoryTests
{
    public static TheoryData<SelectionConfirmationPromptKind> EveryKind
    {
        get
        {
            var data = new TheoryData<SelectionConfirmationPromptKind>();
            foreach (var kind in Enum.GetValues<SelectionConfirmationPromptKind>())
            {
                data.Add(kind);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(EveryKind))]
    public async Task ConfirmationPrompt_ResolvesTheModeledApplicationIdForEveryKind(SelectionConfirmationPromptKind kind)
    {
        var promptContext = CreatePromptContext();
        var session = CreateSession(promptContext);
        var selected = CreateCandidate("alpha", "Alpha", 3);
        var retained = SelectionCandidateSnapshot<ApplicationInfo>.Create(
            SelectionRevision.Initial,
            [selected, CreateCandidate("beta", "Beta", 5)]);
        var activeStagePrompt = SelectionDialogPrompt.FromText("active stage");
        var request = new SelectionConfirmationPromptRequest<ApplicationInfo>(
            kind,
            selected,
            retained,
            SelectionConfirmationCaptureStage.ExplicitAuthorization,
            activeStagePrompt);

        var providers = new FormsCatalogRuntimePromptProvidersFactory().Create(session);
        var prompt = await providers.GetConfirmationPromptAsync(request, CancellationToken.None);

        Assert.Equal(ExpectedText(kind), prompt.Text);
    }

    [Theory]
    [InlineData(SelectionConfirmationCaptureStage.Initial, "id=catalog.app.confirmation.help.initial name=Alpha number={{number}}")]
    [InlineData(SelectionConfirmationCaptureStage.ExplicitAuthorization, "id=catalog.app.confirmation.help.explicitAuthorization name=Alpha number=3")]
    [InlineData(SelectionConfirmationCaptureStage.ConfidenceFaultRetry, "id=catalog.app.confirmation.help.confidenceFaultRetry name=Alpha number=3")]
    public async Task ContextualHelp_ResolvesTheStageSpecificApplicationId(
        SelectionConfirmationCaptureStage stage,
        string expectedText)
    {
        var promptContext = CreatePromptContext();
        var session = CreateSession(promptContext);
        var selected = CreateCandidate("alpha", "Alpha", 3);
        var retained = SelectionCandidateSnapshot<ApplicationInfo>.Create(SelectionRevision.Initial, [selected]);
        var request = new SelectionConfirmationPromptRequest<ApplicationInfo>(
            SelectionConfirmationPromptKind.ContextualHelp,
            selected,
            retained,
            stage,
            activeStagePrompt: null);

        var providers = new FormsCatalogRuntimePromptProvidersFactory().Create(session);
        var prompt = await providers.GetConfirmationPromptAsync(request, CancellationToken.None);

        Assert.Equal(expectedText, prompt.Text);
    }

    private static string ExpectedText(SelectionConfirmationPromptKind kind)
        => kind switch
        {
            SelectionConfirmationPromptKind.InitialConfirmation => "id=catalog.app.confirmation name=Alpha",
            SelectionConfirmationPromptKind.LowConfidenceConfirmation => "id=catalog.app.lowConfidence.confirm name=Alpha",
            SelectionConfirmationPromptKind.ExplicitAuthorization =>
                "id=catalog.app.confirmation.explicitAuthorization name=Alpha number=3",
            SelectionConfirmationPromptKind.VerificationUnavailable =>
                "id=catalog.app.confirmation.verificationUnavailable name=Alpha number=3",
            SelectionConfirmationPromptKind.NotOpenedReselect => "id=catalog.app.confirmation.notOpenedReselect name=Alpha",
            SelectionConfirmationPromptKind.Cancellation => "id=catalog.app.cancelled",
            SelectionConfirmationPromptKind.ActiveStage => "active stage",
            SelectionConfirmationPromptKind.ActiveStageList =>
                "id=catalog.app.list.intro count=2 " +
                "id=catalog.app.list.item number=3 name=Alpha " +
                "id=catalog.app.list.item number=5 name=Beta " +
                "active stage",
            SelectionConfirmationPromptKind.VerificationFailedTerminal =>
                "id=catalog.app.confirmation.verificationFailedTerminal name=Alpha",
            SelectionConfirmationPromptKind.RepairExhaustedTerminal =>
                "id=catalog.app.confirmation.repairExhaustedTerminal name=Alpha",
            SelectionConfirmationPromptKind.ContextualHelp =>
                "id=catalog.app.confirmation.help.explicitAuthorization name=Alpha number=3",
            SelectionConfirmationPromptKind.NoInputReminder => "id=" + CatalogDialogPromptIds.AppConfirmationRepeat,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Every kind needs an expectation.")
        };

    private static FormsCatalogDialogSession CreateSession(CatalogPromptContext promptContext)
        => new(
            promptContext.Catalog,
            [],
            promptContext,
            new FormsCatalogDialogPromptRuntime(
                promptContext,
                new SelectionDialogInteractionSettings(1.0, 0, AllowBargeIn: true, 85, 65),
                PolitenessLevel.Formal,
                "en",
                noInputTimeoutMs: 0,
                maxRetries: 0),
            ConfirmationPolicy.Always);

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

    private static CatalogPromptContext CreatePromptContext()
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
                Prompts =
                [
                    NamedPrompt(CatalogDialogPromptIds.AppConfirmation),
                    NamedPrompt(CatalogDialogPromptIds.AppLowConfidenceConfirm),
                    NumberedPrompt(CatalogDialogPromptIds.AppExplicitAuthorization),
                    NumberedPrompt(CatalogDialogPromptIds.AppVerificationUnavailable),
                    NumberedPrompt(CatalogDialogPromptIds.AppInitialConfirmationHelp),
                    NumberedPrompt(CatalogDialogPromptIds.AppExplicitAuthorizationHelp),
                    NumberedPrompt(CatalogDialogPromptIds.AppConfidenceFaultRetryHelp),
                    NamedPrompt(CatalogDialogPromptIds.AppNotOpenedReselect),
                    NamedPrompt(CatalogDialogPromptIds.AppVerificationFailedTerminal),
                    NamedPrompt(CatalogDialogPromptIds.AppRepairExhaustedTerminal),
                    PlainPrompt(CatalogDialogPromptIds.AppCancelled),
                    PlainPrompt(CatalogDialogPromptIds.AppConfirmationRepeat),
                    CreatePrompt(CatalogDialogPromptIds.AppListIntro, "id=" + CatalogDialogPromptIds.AppListIntro + " count={{count}}"),
                    CreatePrompt(CatalogDialogPromptIds.AppListItem, "id=" + CatalogDialogPromptIds.AppListItem + " number={{number}} name={{name}}")
                ]
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

    private static CatalogVoicePrompt PlainPrompt(string promptId)
        => CreatePrompt(promptId, "id=" + promptId);

    private static CatalogVoicePrompt NamedPrompt(string promptId)
        => CreatePrompt(promptId, "id=" + promptId + " name={{name}}");

    private static CatalogVoicePrompt NumberedPrompt(string promptId)
        => CreatePrompt(promptId, "id=" + promptId + " name={{name}} number={{number}}");

    private static CatalogVoicePrompt CreatePrompt(string promptId, string englishText)
        => new()
        {
            PromptId = promptId,
            Text = new LocalizedText { ["en"] = englishText }
        };
}
