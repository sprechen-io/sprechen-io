using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Ansagen für die Formularauswahl und delegiert an gemeinsame Funktionen
/// sowie an die Ansagen für verfügbare Formulare. Dabei verwendet sie ihre eigenen Modellschlüssel.
/// </summary>
internal static class FormSelectionPromptBuilder
{
    private static readonly FormSelectionInventoryPromptBuilder InventoryPrompts = new();
    internal static readonly SelectionCommonIntentPromptIds CommonIntentPromptIds = new()
    {
        Confirmation = CatalogDialogPromptIds.FormConfirmation,
        LowConfidenceConfirm = CatalogDialogPromptIds.FormLowConfidenceConfirm,
        ExplicitAuthorization = CatalogDialogPromptIds.FormExplicitAuthorization,
        VerificationUnavailable = CatalogDialogPromptIds.FormVerificationUnavailable,
        InitialConfirmationHelp = CatalogDialogPromptIds.FormInitialConfirmationHelp,
        ExplicitAuthorizationHelp = CatalogDialogPromptIds.FormExplicitAuthorizationHelp,
        ConfidenceFaultRetryHelp = CatalogDialogPromptIds.FormConfidenceFaultRetryHelp,
        NotOpenedReselect = CatalogDialogPromptIds.FormNotOpenedReselect,
        VerificationFailedTerminal = CatalogDialogPromptIds.FormVerificationFailedTerminal,
        RepairExhaustedTerminal = CatalogDialogPromptIds.FormRepairExhaustedTerminal,
        CaptureFailedTerminal = CatalogDialogPromptIds.FormCaptureFailedTerminal,
        ConfirmationRepeat = CatalogDialogPromptIds.FormConfirmationRepeat,
        ListIntro = CatalogDialogPromptIds.FormListIntro,
        ListItem = CatalogDialogPromptIds.FormListItem,
        Launch = CatalogDialogPromptIds.FormLaunch,
        Help = CatalogDialogPromptIds.FormHelp,
        Cancelled = CatalogDialogPromptIds.FormCancelled,
        Unrecognized = CatalogDialogPromptIds.FormUnrecognized,
        NoInput = CatalogDialogPromptIds.FormNoInput,
        NoItems = CatalogDialogPromptIds.FormNoForms,
        NoMatch = CatalogDialogPromptIds.FormNoMatch,
        InvalidNumber = CatalogDialogPromptIds.FormInvalidNumber
    };

    private static readonly SelectionCommonIntentPromptBuilder<FormInfo> CommonIntentPrompts =
        new(CommonIntentPromptIds, form => form.DisplayName);

    internal static SelectionDialogPrompt GetAnnouncingPrompt(
        CatalogPromptContext promptContext,
        IReadOnlyList<SelectionCandidate<FormInfo>> candidates,
        string isoCode,
        PolitenessLevel politenessLevel)
        => InventoryPrompts.GetAnnouncingPrompt(promptContext, candidates, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetSingleFormPrompt(
        CatalogPromptContext promptContext,
        FormInfo form,
        string isoCode,
        PolitenessLevel politenessLevel)
        => InventoryPrompts.GetSingleFormPrompt(promptContext, form, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetListPrompt(
        CatalogPromptContext promptContext,
        IReadOnlyList<SelectionCandidate<FormInfo>> candidates,
        string isoCode,
        PolitenessLevel politenessLevel)
        => InventoryPrompts.GetListPrompt(promptContext, candidates, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetAmbiguityPrompt(
        CatalogPromptContext promptContext,
        IReadOnlyList<SelectionCandidate<FormInfo>> candidates,
        string isoCode,
        PolitenessLevel politenessLevel)
        => InventoryPrompts.GetAmbiguityPrompt(promptContext, candidates, isoCode, politenessLevel);

    internal static SelectionDialogPrompt ResolveConfirmationPrompt(
        CatalogPromptContext promptContext,
        SelectionConfirmationPromptRequest<FormInfo> request,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.ResolveConfirmationPrompt(promptContext, request, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetConfirmationPrompt(
        CatalogPromptContext promptContext,
        FormInfo form,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetConfirmationPrompt(promptContext, form, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetLowConfidenceConfirmationPrompt(
        CatalogPromptContext promptContext,
        FormInfo form,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetLowConfidenceConfirmationPrompt(promptContext, form, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetExplicitAuthorizationPrompt(
        CatalogPromptContext promptContext,
        SelectionCandidate<FormInfo> candidate,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetExplicitAuthorizationPrompt(promptContext, candidate, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetVerificationUnavailablePrompt(
        CatalogPromptContext promptContext,
        SelectionCandidate<FormInfo> candidate,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetVerificationUnavailablePrompt(promptContext, candidate, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetContextualConfirmationHelpPrompt(
        CatalogPromptContext promptContext,
        SelectionCandidate<FormInfo> candidate,
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
        SelectionCandidate<FormInfo> candidate,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetNotOpenedReselectPrompt(promptContext, candidate, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetVerificationFailedTerminalPrompt(
        CatalogPromptContext promptContext,
        SelectionCandidate<FormInfo> candidate,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetVerificationFailedTerminalPrompt(promptContext, candidate, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetRepairExhaustedTerminalPrompt(
        CatalogPromptContext promptContext,
        SelectionCandidate<FormInfo> candidate,
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
        SelectionCandidateSnapshot<FormInfo> retainedCandidates,
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
        FormInfo form,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetLaunchPrompt(promptContext, form, isoCode, politenessLevel);

    internal static SelectionDialogPrompt GetPrerequisitesNotMetPrompt(
        CatalogPromptContext promptContext,
        FormInfo form,
        IReadOnlyList<string> missingPrerequisites,
        string isoCode,
        PolitenessLevel politenessLevel)
        => InventoryPrompts.GetPrerequisitesNotMetPrompt(promptContext, form, missingPrerequisites, isoCode, politenessLevel);

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
        // Die formularbezogene Wiederaufnahme bleibt an dieser Fassade, damit der gemeinsame
        // Prompt-Katalog dafür keine zusätzliche Rolle benötigt.
        ArgumentNullException.ThrowIfNull(promptContext);
        return SelectionDialogPrompt.FromText(
            CatalogPromptComposer.ResolvePromptOrThrow(
                promptContext,
                CatalogDialogPromptIds.FormReListen,
                isoCode,
                politenessLevel));
    }

    internal static SelectionDialogPrompt GetNoFormsPrompt(
        CatalogPromptContext promptContext,
        string isoCode,
        PolitenessLevel politenessLevel)
        => CommonIntentPrompts.GetNoItemsPrompt(promptContext, isoCode, politenessLevel);

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
}
