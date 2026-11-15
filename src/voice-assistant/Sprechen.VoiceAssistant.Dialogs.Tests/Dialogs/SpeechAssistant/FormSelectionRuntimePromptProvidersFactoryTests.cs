using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft den erzeugten Bestätigungstext für jeden SelectionConfirmationPromptKind
/// anhand ausdrücklich festgelegter Erwartungen. Neue Enumwerte benötigen
/// einen eigenen Erwartungswert; die Standardverzweigung lehnt sie ab.
/// </summary>
public sealed class FormSelectionRuntimePromptProvidersFactoryTests
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
    public async Task ConfirmationPrompt_ResolvesTheModeledFormIdForEveryKind(SelectionConfirmationPromptKind kind)
    {
        var promptContext = CreatePromptContext();
        var session = CreateSession(promptContext);
        var selected = CreateCandidate("alpha", "Alpha", 3);
        var retained = SelectionCandidateSnapshot<FormInfo>.Create(
            SelectionRevision.Initial,
            [selected, CreateCandidate("beta", "Beta", 5)]);
        var activeStagePrompt = SelectionDialogPrompt.FromText("active stage");
        var request = new SelectionConfirmationPromptRequest<FormInfo>(
            kind,
            selected,
            retained,
            SelectionConfirmationCaptureStage.ExplicitAuthorization,
            activeStagePrompt);

        var providers = new FormSelectionRuntimePromptProvidersFactory().Create(session);
        var prompt = await providers.GetConfirmationPromptAsync(request, CancellationToken.None);

        Assert.Equal(ExpectedText(kind), prompt.Text);
    }

    [Theory]
    [InlineData(SelectionConfirmationCaptureStage.Initial, "id=catalog.form.confirmation.help.initial name=Alpha number={{number}}")]
    [InlineData(SelectionConfirmationCaptureStage.ExplicitAuthorization, "id=catalog.form.confirmation.help.explicitAuthorization name=Alpha number=3")]
    [InlineData(SelectionConfirmationCaptureStage.ConfidenceFaultRetry, "id=catalog.form.confirmation.help.confidenceFaultRetry name=Alpha number=3")]
    public async Task ContextualHelp_ResolvesTheStageSpecificFormId(
        SelectionConfirmationCaptureStage stage,
        string expectedText)
    {
        var promptContext = CreatePromptContext();
        var session = CreateSession(promptContext);
        var selected = CreateCandidate("alpha", "Alpha", 3);
        var retained = SelectionCandidateSnapshot<FormInfo>.Create(SelectionRevision.Initial, [selected]);
        var request = new SelectionConfirmationPromptRequest<FormInfo>(
            SelectionConfirmationPromptKind.ContextualHelp,
            selected,
            retained,
            stage,
            activeStagePrompt: null);

        var providers = new FormSelectionRuntimePromptProvidersFactory().Create(session);
        var prompt = await providers.GetConfirmationPromptAsync(request, CancellationToken.None);

        Assert.Equal(expectedText, prompt.Text);
    }

    private static string ExpectedText(SelectionConfirmationPromptKind kind)
        => kind switch
        {
            SelectionConfirmationPromptKind.InitialConfirmation => "id=catalog.form.confirmation name=Alpha",
            SelectionConfirmationPromptKind.LowConfidenceConfirmation => "id=catalog.form.lowConfidence.confirm name=Alpha",
            SelectionConfirmationPromptKind.ExplicitAuthorization =>
                "id=catalog.form.confirmation.explicitAuthorization name=Alpha number=3",
            SelectionConfirmationPromptKind.VerificationUnavailable =>
                "id=catalog.form.confirmation.verificationUnavailable name=Alpha number=3",
            SelectionConfirmationPromptKind.NotOpenedReselect => "id=catalog.form.confirmation.notOpenedReselect name=Alpha",
            SelectionConfirmationPromptKind.Cancellation => "id=catalog.form.cancelled",
            SelectionConfirmationPromptKind.ActiveStage => "active stage",
            SelectionConfirmationPromptKind.ActiveStageList =>
                "id=catalog.form.list.intro count=2 " +
                "id=catalog.form.list.item number=3 name=Alpha " +
                "id=catalog.form.list.item number=5 name=Beta " +
                "active stage",
            SelectionConfirmationPromptKind.VerificationFailedTerminal =>
                "id=catalog.form.confirmation.verificationFailedTerminal name=Alpha",
            SelectionConfirmationPromptKind.RepairExhaustedTerminal =>
                "id=catalog.form.confirmation.repairExhaustedTerminal name=Alpha",
            SelectionConfirmationPromptKind.ContextualHelp =>
                "id=catalog.form.confirmation.help.explicitAuthorization name=Alpha number=3",
            SelectionConfirmationPromptKind.NoInputReminder => "id=" + CatalogDialogPromptIds.FormConfirmationRepeat,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Every kind needs an expectation.")
        };

    private static FormSelectionDialogSession CreateSession(CatalogPromptContext promptContext)
        => new(
            promptContext.Catalog,
            [],
            promptContext,
            new SelectionDialogInteractionSettings(1.0, 0, AllowBargeIn: true, 85, 65),
            PolitenessLevel.Formal,
            "en",
            noInputTimeoutMs: 0,
            maxRetries: 0,
            new FormSelectionTransitionContext(
                ConfirmationPolicy.Always,
                new HashSet<string>(StringComparer.Ordinal),
                new Dictionary<string, string>(StringComparer.Ordinal),
                promptContext,
                PolitenessLevel.Formal,
                "en"));

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
                    NamedPrompt(CatalogDialogPromptIds.FormConfirmation),
                    NamedPrompt(CatalogDialogPromptIds.FormLowConfidenceConfirm),
                    NumberedPrompt(CatalogDialogPromptIds.FormExplicitAuthorization),
                    NumberedPrompt(CatalogDialogPromptIds.FormVerificationUnavailable),
                    NumberedPrompt(CatalogDialogPromptIds.FormInitialConfirmationHelp),
                    NumberedPrompt(CatalogDialogPromptIds.FormExplicitAuthorizationHelp),
                    NumberedPrompt(CatalogDialogPromptIds.FormConfidenceFaultRetryHelp),
                    NamedPrompt(CatalogDialogPromptIds.FormNotOpenedReselect),
                    NamedPrompt(CatalogDialogPromptIds.FormVerificationFailedTerminal),
                    NamedPrompt(CatalogDialogPromptIds.FormRepairExhaustedTerminal),
                    PlainPrompt(CatalogDialogPromptIds.FormCancelled),
                    PlainPrompt(CatalogDialogPromptIds.FormConfirmationRepeat),
                    CreatePrompt(CatalogDialogPromptIds.FormListIntro, "id=" + CatalogDialogPromptIds.FormListIntro + " count={{count}}"),
                    CreatePrompt(CatalogDialogPromptIds.FormListItem, "id=" + CatalogDialogPromptIds.FormListItem + " number={{number}} name={{name}}")
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
