using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.ModelRuntime.PromptTemplates;

/// <summary>
/// Versorgt den Formularablauf mit parametrisierten Ansagen aus dem Dialogmodell, sodass Ablaufcode keine eigenen Formulierungen verwalten muss.
/// </summary>
public static class FormDialogFlowPromptTextResolver
{
    public static string GetMissingArgumentPrompt(string isoCode, string supportedLanguages)
        => GetMissingArgumentPrompt(isoCode, supportedLanguages, PolitenessLevel.Formal);

    public static string GetMissingArgumentPrompt(string isoCode, string supportedLanguages, PolitenessLevel politenessLevel)
        => ResolvePolitenessVariantPrompt(
            isoCode,
            FormDialogPromptIds.ChangeLanguageMissingArgument,
            politenessLevel,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.Items] = supportedLanguages
            });

    public static string GetUnsupportedPrompt(string isoCode, string supportedLanguages)
        => ResolvePrompt(
            isoCode,
            FormDialogPromptIds.ChangeLanguageUnsupported,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.Items] = supportedLanguages
            });

    public static string GetAlreadySelectedPrompt(string isoCode, string languageName)
        => ResolvePrompt(
            isoCode,
            FormDialogPromptIds.ChangeLanguageAlreadySelected,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.LanguageName] = languageName
            });

    public static string GetConfirmationPrompt(string isoCode, string languageName)
        => ResolvePrompt(
            isoCode,
            FormDialogPromptIds.ChangeLanguageConfirmation,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.LanguageName] = languageName
            });

    public static string GetKeptPrompt(string isoCode, string languageName)
        => ResolvePrompt(
            isoCode,
            FormDialogPromptIds.ChangeLanguageKept,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.LanguageName] = languageName
            });

    public static string GetWakeWordHintPrompt(string isoCode)
        => GetWakeWordHintPrompt(isoCode, PolitenessLevel.Formal);

    public static string GetWakeWordHintPrompt(string isoCode, PolitenessLevel politenessLevel)
        => ResolvePolitenessVariantPrompt(
            isoCode,
            FormDialogPromptIds.ChangeLanguageWakeWordHint,
            politenessLevel,
            new Dictionary<string, string?>());

    public static string GetSessionWakeWordIntroPrompt(string isoCode)
        => GetSessionWakeWordIntroPrompt(isoCode, PolitenessLevel.Formal);

    public static string GetSessionWakeWordIntroPrompt(string isoCode, PolitenessLevel politenessLevel)
        => ResolvePolitenessVariantPrompt(
            isoCode,
            FormDialogPromptIds.SessionWakeWordIntro,
            politenessLevel,
            new Dictionary<string, string?>());

    public static string GetStudyEntryStudyAnnouncementPrompt(string isoCode, string applicationName)
        => ResolvePrompt(
            isoCode,
            FormDialogPromptIds.StudyEntryStudyAnnouncement,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.ApplicationName] = applicationName
            });

    public static string GetSessionWakeWordRecoveryHintPrompt(string isoCode)
        => GetSessionWakeWordRecoveryHintPrompt(isoCode, PolitenessLevel.Formal);

    public static string GetSessionWakeWordRecoveryHintPrompt(string isoCode, PolitenessLevel politenessLevel)
        => ResolvePolitenessVariantPrompt(
            isoCode,
            FormDialogPromptIds.SessionWakeWordRecoveryHint,
            politenessLevel,
            new Dictionary<string, string?>());

    public static string GetSessionWakeWordRecoveryHintAlternativesPrompt(string isoCode)
        => GetSessionWakeWordRecoveryHintAlternativesPrompt(isoCode, PolitenessLevel.Formal);

    public static string GetSessionWakeWordRecoveryHintAlternativesPrompt(string isoCode, PolitenessLevel politenessLevel)
        => ResolvePolitenessVariantPrompt(
            isoCode,
            FormDialogPromptIds.SessionWakeWordRecoveryHintAlternatives,
            politenessLevel,
            new Dictionary<string, string?>());

    public static string GetSessionMicrophoneUnavailableDeviceNotFoundPrompt(string isoCode)
        => GetSessionMicrophoneUnavailableDeviceNotFoundPrompt(isoCode, PolitenessLevel.Formal);

    public static string GetSessionMicrophoneUnavailableDeviceNotFoundPrompt(string isoCode, PolitenessLevel politenessLevel)
        => ResolvePolitenessVariantPrompt(
            isoCode,
            FormDialogPromptIds.SessionMicrophoneUnavailableDeviceNotFound,
            politenessLevel,
            new Dictionary<string, string?>());

    public static string GetSessionMicrophoneUnavailablePermissionDeniedPrompt(string isoCode)
        => GetSessionMicrophoneUnavailablePermissionDeniedPrompt(isoCode, PolitenessLevel.Formal);

    public static string GetSessionMicrophoneUnavailablePermissionDeniedPrompt(string isoCode, PolitenessLevel politenessLevel)
        => ResolvePolitenessVariantPrompt(
            isoCode,
            FormDialogPromptIds.SessionMicrophoneUnavailablePermissionDenied,
            politenessLevel,
            new Dictionary<string, string?>());

    public static string GetSessionMicrophoneUnavailableTechnicalPrompt(string isoCode)
        => GetSessionMicrophoneUnavailableTechnicalPrompt(isoCode, PolitenessLevel.Formal);

    public static string GetSessionMicrophoneUnavailableTechnicalPrompt(string isoCode, PolitenessLevel politenessLevel)
        => ResolvePolitenessVariantPrompt(
            isoCode,
            FormDialogPromptIds.SessionMicrophoneUnavailableTechnical,
            politenessLevel,
            new Dictionary<string, string?>());

    public static string GetSessionRepeatHintPrompt(string isoCode)
        => GetSessionRepeatHintPrompt(isoCode, PolitenessLevel.Formal);

    public static string GetSessionRepeatHintPrompt(string isoCode, PolitenessLevel politenessLevel)
        => ResolvePolitenessVariantPrompt(
            isoCode,
            FormDialogPromptIds.SessionRepeatHint,
            politenessLevel,
            new Dictionary<string, string?>());

    public static string GetAutoAdvanceManualContinuationPrompt(string isoCode)
        => GetAutoAdvanceManualContinuationPrompt(isoCode, PolitenessLevel.Formal);

    public static string GetAutoAdvanceManualContinuationPrompt(string isoCode, PolitenessLevel politenessLevel)
        => ResolvePolitenessVariantPrompt(
            isoCode,
            FormDialogPromptIds.AutoAdvanceManualContinuation,
            politenessLevel,
            new Dictionary<string, string?>());

    public static string GetAutoAdvanceManualContinuationShortPrompt(string isoCode)
        => ResolvePrompt(
            isoCode,
            FormDialogPromptIds.AutoAdvanceManualContinuationShort,
            new Dictionary<string, string?>());

    public static string GetCommitAcceptedPrompt(string isoCode)
        => ResolvePrompt(
            isoCode,
            FormDialogPromptIds.CommitAccepted,
            new Dictionary<string, string?>());

    public static string GetProgressAnswerableTotalChangedPrompt(string isoCode, int previous, int total)
        => ResolvePrompt(
            isoCode,
            FormDialogPromptIds.ProgressAnswerableTotalChanged,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.Previous] = previous.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [FormPromptTemplateParameterNames.Total] = total.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });

    public static string GetFormSubmittedPrompt(string isoCode)
        => ResolvePrompt(
            isoCode,
            FormDialogPromptIds.FormSubmitted,
            new Dictionary<string, string?>());

    public static string GetEvaluationDeliveryWaitingPrompt(string isoCode)
        => ResolvePrompt(isoCode, FormDialogPromptIds.EvaluationDeliveryWaiting, new Dictionary<string, string?>());

    public static string GetEvaluationOtherTabPrompt(string isoCode)
        => ResolvePrompt(isoCode, FormDialogPromptIds.EvaluationOtherTab, new Dictionary<string, string?>());

    public static string GetEvaluationPermanentFailurePrompt(string isoCode)
        => ResolvePrompt(isoCode, FormDialogPromptIds.EvaluationPermanentFailure, new Dictionary<string, string?>());

    public static string GetEvaluationLocalCommitWaitingPrompt(string isoCode)
        => ResolvePrompt(isoCode, FormDialogPromptIds.EvaluationLocalCommitWaiting, new Dictionary<string, string?>());

    public static string GetEvaluationTaskCancelledPrompt(string isoCode)
        => ResolvePrompt(isoCode, FormDialogPromptIds.EvaluationTaskCancelled, new Dictionary<string, string?>());

    public static string GetEvaluationTechnicalFailurePrompt(string isoCode)
        => ResolvePrompt(isoCode, FormDialogPromptIds.EvaluationTechnicalFailure, new Dictionary<string, string?>());

    public static string GetEvaluationRestoredAbandonedPrompt(string isoCode)
        => ResolvePrompt(isoCode, FormDialogPromptIds.EvaluationRestoredAbandoned, new Dictionary<string, string?>());

    public static string GetFormClosingReviewPrompt(string isoCode)
        => GetFormClosingReviewPrompt(isoCode, PolitenessLevel.Formal);

    public static string GetFormClosingReviewPrompt(string isoCode, PolitenessLevel politenessLevel)
        => ResolvePolitenessVariantPrompt(
            isoCode,
            FormDialogPromptIds.FormClosingReview,
            politenessLevel,
            new Dictionary<string, string?>());

    public static string GetSilentInputReadyPrompt(string isoCode)
        => ResolvePrompt(isoCode, FormDialogPromptIds.SilentInputReady, new Dictionary<string, string?>());

    public static string GetSilentInputUnavailablePrompt(string isoCode)
        => ResolvePrompt(isoCode, FormDialogPromptIds.SilentInputUnavailable, new Dictionary<string, string?>());

    public static string GetSilentInputStateChangedPrompt(string isoCode)
        => ResolvePrompt(isoCode, FormDialogPromptIds.SilentInputStateChanged, new Dictionary<string, string?>());

    private static string ResolvePolitenessVariantPrompt(
        string isoCode,
        string promptId,
        PolitenessLevel politenessLevel,
        IReadOnlyDictionary<string, string?> templateParameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(promptId);
        ArgumentNullException.ThrowIfNull(templateParameters);

        // Der Katalog wählt Höflichkeitsvariante oder Grundvorlage aus.
        // Hier werden nur die Parameter in die gelieferte Vorlage eingesetzt.
        if (!VoiceInterfaceModelCatalog.Resources.TryGetPromptTemplate(promptId, isoCode, politenessLevel, out var template))
        {
            throw new InvalidOperationException($"Prompt template '{promptId}' is missing for '{isoCode}'.");
        }

        return PromptTemplateFormatter.Apply(template, templateParameters);
    }

    private static string ResolvePrompt(
        string isoCode,
        string promptId,
        IReadOnlyDictionary<string, string?> templateParameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(promptId);
        ArgumentNullException.ThrowIfNull(templateParameters);

        if (!VoiceInterfaceModelCatalog.Resources.TryGetPromptTemplate(promptId, isoCode, out var template))
        {
            throw new InvalidOperationException($"Prompt template '{promptId}' is missing for '{isoCode}'.");
        }

        return PromptTemplateFormatter.Apply(template, templateParameters);
    }
}
