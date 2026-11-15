using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Ansagen für die Anwendungsauswahl und delegiert an gemeinsame Funktionen
/// sowie an Ansagen für verfügbare Anwendungen und Suchergebnisse. Dabei nutzt sie
/// ihre eigenen Modellschlüssel.
/// </summary>
internal static class FormsCatalogPromptBuilder
{
    private static readonly FormsCatalogInventoryPromptBuilder InventoryPrompts = new();
    private static readonly FormsCatalogSearchPromptBuilder SearchPrompts = new();
    internal static readonly SelectionCommonIntentPromptIds CommonIntentPromptIds = new()
    {
        Confirmation = CatalogDialogPromptIds.AppConfirmation,
        LowConfidenceConfirm = CatalogDialogPromptIds.AppLowConfidenceConfirm,
        ExplicitAuthorization = CatalogDialogPromptIds.AppExplicitAuthorization,
        VerificationUnavailable = CatalogDialogPromptIds.AppVerificationUnavailable,
        InitialConfirmationHelp = CatalogDialogPromptIds.AppInitialConfirmationHelp,
        ExplicitAuthorizationHelp = CatalogDialogPromptIds.AppExplicitAuthorizationHelp,
        ConfidenceFaultRetryHelp = CatalogDialogPromptIds.AppConfidenceFaultRetryHelp,
        NotOpenedReselect = CatalogDialogPromptIds.AppNotOpenedReselect,
        VerificationFailedTerminal = CatalogDialogPromptIds.AppVerificationFailedTerminal,
        RepairExhaustedTerminal = CatalogDialogPromptIds.AppRepairExhaustedTerminal,
        CaptureFailedTerminal = CatalogDialogPromptIds.AppCaptureFailedTerminal,
        ConfirmationRepeat = CatalogDialogPromptIds.AppConfirmationRepeat,
        ListIntro = CatalogDialogPromptIds.AppListIntro,
        ListItem = CatalogDialogPromptIds.AppListItem,
        Launch = CatalogDialogPromptIds.AppLaunch,
        Help = CatalogDialogPromptIds.AppHelp,
        Cancelled = CatalogDialogPromptIds.AppCancelled,
        Unrecognized = CatalogDialogPromptIds.AppUnrecognized,
        NoInput = CatalogDialogPromptIds.AppNoInput,
        NoItems = CatalogDialogPromptIds.AppNoApplications,
        NoMatch = CatalogDialogPromptIds.AppNoMatch,
        InvalidNumber = CatalogDialogPromptIds.AppInvalidNumber
    };

    private static readonly SelectionCommonIntentPromptBuilder<ApplicationInfo> CommonIntentPrompts =
        new(CommonIntentPromptIds, app => app.DisplayName);

    internal static SelectionDialogPrompt GetAnnouncingPrompt(
        CatalogPromptContext promptContext,
        IReadOnlyList<SelectionCandidate<ApplicationInfo>> candidates,
        string isoCode,
        PolitenessLevel politenessLevel)
        => InventoryPrompts.GetAnnouncingPrompt(promptContext, candidates, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetListPrompt(
        CatalogPromptContext promptContext,
        IReadOnlyList<SelectionCandidate<ApplicationInfo>> candidates,
        string isoCode,
        PolitenessLevel politenessLevel)
        => InventoryPrompts.GetListPrompt(promptContext, candidates, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetAmbiguityPrompt(
        CatalogPromptContext promptContext,
        IReadOnlyList<SelectionCandidate<ApplicationInfo>> candidates,
        string isoCode,
        PolitenessLevel politenessLevel)
        => InventoryPrompts.GetAmbiguityPrompt(promptContext, candidates, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetSearchResultsPrompt(
        CatalogPromptContext promptContext,
        IReadOnlyList<SelectionCandidate<ApplicationInfo>> candidates,
        string? query,
        string isoCode,
        PolitenessLevel politenessLevel)
        => SearchPrompts.GetSearchResultsPrompt(promptContext, candidates, query, isoCode, politenessLevel);

    internal static SelectionDialogPrompt ResolveConfirmationPrompt(
        CatalogPromptContext promptContext,
        SelectionConfirmationPromptRequest<ApplicationInfo> request,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.ResolveConfirmationPrompt(promptContext, request, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetConfirmationPrompt(
        CatalogPromptContext promptContext,
        ApplicationInfo app,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetConfirmationPrompt(promptContext, app, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetLowConfidenceConfirmationPrompt(
        CatalogPromptContext promptContext,
        ApplicationInfo app,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetLowConfidenceConfirmationPrompt(promptContext, app, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetExplicitAuthorizationPrompt(
        CatalogPromptContext promptContext,
        SelectionCandidate<ApplicationInfo> candidate,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetExplicitAuthorizationPrompt(promptContext, candidate, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetVerificationUnavailablePrompt(
        CatalogPromptContext promptContext,
        SelectionCandidate<ApplicationInfo> candidate,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetVerificationUnavailablePrompt(promptContext, candidate, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetContextualConfirmationHelpPrompt(
        CatalogPromptContext promptContext,
        SelectionCandidate<ApplicationInfo> candidate,
        SelectionConfirmationCaptureStage stage,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetContextualConfirmationHelpPrompt(
            promptContext,
            candidate,
            stage,
            isoCode,
            politenessLevel);

    internal static SelectionDialogPrompt GetNotOpenedReselectPrompt(
        CatalogPromptContext promptContext,
        SelectionCandidate<ApplicationInfo> candidate,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetNotOpenedReselectPrompt(promptContext, candidate, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetVerificationFailedTerminalPrompt(
        CatalogPromptContext promptContext,
        SelectionCandidate<ApplicationInfo> candidate,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetVerificationFailedTerminalPrompt(promptContext, candidate, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetRepairExhaustedTerminalPrompt(
        CatalogPromptContext promptContext,
        SelectionCandidate<ApplicationInfo> candidate,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetRepairExhaustedTerminalPrompt(promptContext, candidate, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetCaptureFailedTerminalPrompt(
        CatalogPromptContext promptContext,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetCaptureFailedTerminalPrompt(promptContext, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetConfirmationListPrompt(
        CatalogPromptContext promptContext,
        SelectionCandidateSnapshot<ApplicationInfo> retainedCandidates,
        SelectionDialogPrompt activeStagePrompt,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetConfirmationListPrompt(
            promptContext,
            retainedCandidates,
            activeStagePrompt,
            isoCode,
            politenessLevel);

    internal static SelectionDialogPrompt GetConfirmationRepeatPrompt(
        CatalogPromptContext promptContext,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetConfirmationRepeatPrompt(promptContext, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetLaunchPrompt(
        CatalogPromptContext promptContext,
        ApplicationInfo app,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetLaunchPrompt(promptContext, app, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetHelpPrompt(
        CatalogPromptContext promptContext,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetHelpPrompt(promptContext, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetCancelPrompt(
        CatalogPromptContext promptContext,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetCancelPrompt(promptContext, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetUnrecognizedPrompt(
        CatalogPromptContext promptContext,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetUnrecognizedPrompt(promptContext, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetNoInputPrompt(
        CatalogPromptContext promptContext,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetNoInputPrompt(promptContext, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetReListenPrompt(
        CatalogPromptContext promptContext,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        // Die anwendungsbezogene Wiederaufnahme bleibt an dieser Fassade, damit der gemeinsame
        // Prompt-Katalog dafür keine zusätzliche Rolle benötigt.
        ArgumentNullException.ThrowIfNull(promptContext);
        return SelectionDialogPrompt.FromText(
            CatalogPromptComposer.ResolvePromptOrThrow(
                promptContext,
                CatalogDialogPromptIds.AppReListen,
                isoCode,
                politenessLevel));
    }

    internal static SelectionDialogPrompt GetNoApplicationsPrompt(
        CatalogPromptContext promptContext,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetNoItemsPrompt(promptContext, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetNoSearchResultsPrompt(
        CatalogPromptContext promptContext,
        string? query,
        string isoCode,
        PolitenessLevel politenessLevel)
        => SearchPrompts.GetNoSearchResultsPrompt(promptContext, query, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetNoMatchPrompt(
        CatalogPromptContext promptContext,
        string? name,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetNoMatchPrompt(promptContext, name, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetInvalidNumberPrompt(
        CatalogPromptContext promptContext,
        int max,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetInvalidNumberPrompt(promptContext, max, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetNoDescriptionPrompt(
        CatalogPromptContext promptContext,
        string isoCode,
        PolitenessLevel politenessLevel)
        => SearchPrompts.GetNoDescriptionPrompt(promptContext, isoCode, politenessLevel);
}
