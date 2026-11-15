using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant.CatalogPrompting;

/// <summary>
/// Bündelt die modellierten Prompt-Schlüssel der Anwendungs- und Formularauswahl. Die
/// Textauflösung verwendet dadurch dieselben Rollenbezeichnungen in allen Ansagebausteinen.
/// </summary>
internal static class CatalogDialogPromptIds
{
    public const string AppAnnouncingSingle = "catalog.app.announcing.single";
    public const string AppAnnouncingMultiple = "catalog.app.announcing.multiple";
    public const string AppListIntro = "catalog.app.list.intro";
    public const string AppListItem = "catalog.app.list.item";
    public const string AppListSelectPrompt = "catalog.app.list.selectPrompt";
    public const string AppSearchResultsIntro = "catalog.app.search.results.intro";
    public const string AppSearchResultsItem = "catalog.app.search.results.item";
    public const string AppAmbiguityShortIntro = "catalog.app.ambiguity.short.intro";
    public const string AppAmbiguityRefineOrList = "catalog.app.ambiguity.refineOrList";
    public const string AppConfirmation = "catalog.app.confirmation";
    public const string AppLowConfidenceConfirm = "catalog.app.lowConfidence.confirm";
    public const string AppExplicitAuthorization = "catalog.app.confirmation.explicitAuthorization";
    public const string AppVerificationUnavailable = "catalog.app.confirmation.verificationUnavailable";
    public const string AppInitialConfirmationHelp = "catalog.app.confirmation.help.initial";
    public const string AppExplicitAuthorizationHelp = "catalog.app.confirmation.help.explicitAuthorization";
    public const string AppConfidenceFaultRetryHelp = "catalog.app.confirmation.help.confidenceFaultRetry";
    public const string AppNotOpenedReselect = "catalog.app.confirmation.notOpenedReselect";
    public const string AppVerificationFailedTerminal = "catalog.app.confirmation.verificationFailedTerminal";
    public const string AppRepairExhaustedTerminal = "catalog.app.confirmation.repairExhaustedTerminal";
    public const string AppCaptureFailedTerminal = "catalog.app.captureFailedTerminal";
    public const string AppConfirmationRepeat = ApplicationSelectionRequiredPromptId.AppConfirmationRepeat;
    public const string AppLaunch = "catalog.app.launch";
    public const string AppHelp = "catalog.app.help";
    public const string AppCancelled = "catalog.app.cancelled";
    public const string AppChangeLanguage = "catalog.app.changeLanguage";
    public const string AppUnrecognized = "catalog.app.unrecognized";
    public const string AppNoInput = "catalog.app.noInput";
    public const string AppReListen = "catalog.app.reListen";
    public const string AppNoApplications = "catalog.app.noApplications";
    public const string AppNoSearchResults = "catalog.app.noSearchResults";
    public const string AppNoMatch = "catalog.app.noMatch";
    public const string AppInvalidNumber = "catalog.app.invalidNumber";
    public const string AppNoDescription = "catalog.app.noDescription";

    public const string FormAnnouncing = "catalog.form.announcing";
    public const string FormSingle = "catalog.form.single";
    public const string FormListIntro = "catalog.form.list.intro";
    public const string FormListItem = "catalog.form.list.item";
    public const string FormListSelectPrompt = "catalog.form.list.selectPrompt";
    public const string FormAmbiguityShortIntro = "catalog.form.ambiguity.short.intro";
    public const string FormAmbiguityRefineOrList = "catalog.form.ambiguity.refineOrList";
    public const string FormConfirmation = "catalog.form.confirmation";
    public const string FormLowConfidenceConfirm = "catalog.form.lowConfidence.confirm";
    public const string FormExplicitAuthorization = "catalog.form.confirmation.explicitAuthorization";
    public const string FormVerificationUnavailable = "catalog.form.confirmation.verificationUnavailable";
    public const string FormInitialConfirmationHelp = "catalog.form.confirmation.help.initial";
    public const string FormExplicitAuthorizationHelp = "catalog.form.confirmation.help.explicitAuthorization";
    public const string FormConfidenceFaultRetryHelp = "catalog.form.confirmation.help.confidenceFaultRetry";
    public const string FormNotOpenedReselect = "catalog.form.confirmation.notOpenedReselect";
    public const string FormVerificationFailedTerminal = "catalog.form.confirmation.verificationFailedTerminal";
    public const string FormRepairExhaustedTerminal = "catalog.form.confirmation.repairExhaustedTerminal";
    public const string FormCaptureFailedTerminal = "catalog.form.captureFailedTerminal";
    public const string FormConfirmationRepeat = ApplicationSelectionRequiredPromptId.FormConfirmationRepeat;
    public const string FormLaunch = "catalog.form.launch";
    public const string FormPrerequisitesNotMet = "catalog.form.prerequisites.notMet";
    public const string FormHelp = "catalog.form.help";
    public const string FormCancelled = "catalog.form.cancelled";
    public const string FormUnrecognized = "catalog.form.unrecognized";
    public const string FormNoInput = "catalog.form.noInput";
    public const string FormReListen = "catalog.form.reListen";
    public const string FormNoForms = "catalog.form.noForms";
    public const string FormNoMatch = "catalog.form.noMatch";
    public const string FormInvalidNumber = "catalog.form.invalidNumber";
}
