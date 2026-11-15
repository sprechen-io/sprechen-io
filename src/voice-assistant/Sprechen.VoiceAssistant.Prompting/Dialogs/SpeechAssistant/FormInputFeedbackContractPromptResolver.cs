using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.PromptTemplates;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Löst Rückmeldungen zur Formulareingabe aus dem Vertrag auf und verwendet
/// die eingebauten Standardtexte, wenn der Vertrag keine passende Vorlage liefert.
/// </summary>
internal sealed class FormInputFeedbackContractPromptResolver
{
    private readonly FormInputFeedbackFallbackPromptResolver fallbackPromptResolver;
    private readonly FormInputModeChangedPromptIdResolver inputModePromptIdResolver;
    private readonly FormSilentInputFallbackLabelPromptIdResolver silentInputFallbackLabelPromptIdResolver;

    public FormInputFeedbackContractPromptResolver()
        : this(
            new FormInputFeedbackFallbackPromptResolver(),
            new FormInputModeChangedPromptIdResolver(),
            new FormSilentInputFallbackLabelPromptIdResolver())
    {
    }

    public FormInputFeedbackContractPromptResolver(
        FormInputFeedbackFallbackPromptResolver fallbackPromptResolver,
        FormInputModeChangedPromptIdResolver inputModePromptIdResolver,
        FormSilentInputFallbackLabelPromptIdResolver silentInputFallbackLabelPromptIdResolver)
    {
        this.fallbackPromptResolver = fallbackPromptResolver;
        this.inputModePromptIdResolver = inputModePromptIdResolver;
        this.silentInputFallbackLabelPromptIdResolver = silentInputFallbackLabelPromptIdResolver;
    }

    public string GetUnrecognizedInputPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.UnrecognizedInput,
            isoCode,
            politenessLevel,
            fallbackPromptResolver.GetUnrecognizedInputPrompt(isoCode));
    }

    public string GetUnrecognizedInputWithTranscriptPrompt(
        FormContract contract,
        string isoCode,
        string transcript,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.UnrecognizedInputWithTranscript,
            isoCode,
            politenessLevel,
            fallbackPromptResolver.GetUnrecognizedInputWithTranscriptPrompt(isoCode, transcript),
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.Transcript] = transcript
            });
    }

    public string GetUnrecognizedInputSecondPrompt(
        FormContract contract,
        string isoCode,
        string transcript,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.UnrecognizedInputSecond,
            isoCode,
            politenessLevel,
            fallbackPromptResolver.GetUnrecognizedInputSecondPrompt(isoCode, transcript),
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.Transcript] = transcript
            });
    }

    public string GetUnrecognizedInputThirdPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.UnrecognizedInputThird,
            isoCode,
            politenessLevel,
            fallbackPromptResolver.GetUnrecognizedInputThirdPrompt(isoCode));
    }

    public string GetUnrecognizedInputThirdBeforeHelpPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.UnrecognizedInputThirdBeforeHelp,
            isoCode,
            politenessLevel,
            fallbackPromptResolver.GetUnrecognizedInputThirdBeforeHelpPrompt(isoCode));
    }

    public string GetInputModeChangedPrompt(
        FormContract contract,
        string isoCode,
        InputMode inputMode,
        PolitenessLevel politenessLevel,
        bool teachFullSpellingTail = true)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            inputModePromptIdResolver.Resolve(inputMode, teachFullSpellingTail),
            isoCode,
            politenessLevel,
            fallbackPromptResolver.GetInputModeChangedPrompt(isoCode, inputMode, teachFullSpellingTail));
    }

    public string GetLiteralModeCommandBlockedPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.LiteralModeCommandBlocked,
            isoCode,
            politenessLevel,
            fallbackPromptResolver.GetLiteralModeCommandBlockedPrompt(isoCode));
    }

    public string GetSilentInputUnavailablePrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.SilentInputUnavailable,
            isoCode,
            politenessLevel,
            fallbackPromptResolver.GetSilentInputUnavailablePrompt(isoCode));
    }

    public string GetVoiceInputForbiddenPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        string? silentInputFallback)
    {
        ArgumentNullException.ThrowIfNull(contract);

        var fallback = fallbackPromptResolver.GetVoiceInputForbiddenPrompt(isoCode, silentInputFallback);
        if (!string.IsNullOrWhiteSpace(silentInputFallback))
        {
            return FormPromptResolution.ResolvePromptOrFallback(
                contract,
                FormDialogPromptIds.VoiceInputForbiddenWithFallback,
                isoCode,
                politenessLevel,
                fallback,
                new Dictionary<string, string?>
                {
                    [FormPromptTemplateParameterNames.SilentInputFallback] = silentInputFallback
                });
        }

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.VoiceInputForbiddenDefault,
            isoCode,
            politenessLevel,
            fallback);
    }

    public string GetReadbackOptInPrompt(
        FormContract contract,
        string isoCode,
        string fieldLabel,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.ReadbackOptIn,
            isoCode,
            politenessLevel,
            fallbackPromptResolver.GetReadbackOptInPrompt(isoCode, fieldLabel, politenessLevel),
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.FieldLabel] = fieldLabel
            });
    }

    public string GetInputModeUnavailablePrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.InputModeUnavailable,
            isoCode,
            politenessLevel,
            fallbackPromptResolver.GetInputModeUnavailablePrompt(isoCode, politenessLevel));
    }

    public string GetCommandParkWaitingPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.CommandParkWaiting,
            isoCode,
            politenessLevel,
            fallbackPromptResolver.GetCommandParkWaitingPrompt(isoCode, politenessLevel));
    }

    public string GetCorrectionRequestedPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.CorrectionRequested,
            isoCode,
            politenessLevel,
            fallbackPromptResolver.GetCorrectionRequestedPrompt(isoCode));
    }

    public string GetSilentInputOfferPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        string? silentInputFallback)
    {
        ArgumentNullException.ThrowIfNull(contract);

        var fallback = fallbackPromptResolver.GetSilentInputOfferPrompt(isoCode, silentInputFallback);
        if (!string.IsNullOrWhiteSpace(silentInputFallback))
        {
            return FormPromptResolution.ResolvePromptOrFallback(
                contract,
                FormDialogPromptIds.SilentInputOfferWithFallback,
                isoCode,
                politenessLevel,
                fallback,
                new Dictionary<string, string?>
                {
                    [FormPromptTemplateParameterNames.SilentInputFallback] = silentInputFallback
                });
        }

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.SilentInputOfferDefault,
            isoCode,
            politenessLevel,
            fallback);
    }

    public string GetAmbiguousInputVsCommandPrompt(
        FormContract contract,
        string isoCode,
        string transcript,
        string? commandLabel,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        var fallback = fallbackPromptResolver.GetAmbiguousInputVsCommandPrompt(isoCode, transcript, commandLabel);
        if (string.IsNullOrWhiteSpace(commandLabel))
        {
            return FormPromptResolution.ResolvePromptOrFallback(
                contract,
                FormDialogPromptIds.AmbiguousInputVsCommandWithoutLabel,
                isoCode,
                politenessLevel,
                fallback,
                new Dictionary<string, string?>
                {
                    [FormPromptTemplateParameterNames.Transcript] = transcript
                });
        }

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.AmbiguousInputVsCommandWithLabel,
            isoCode,
            politenessLevel,
            fallback,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.Transcript] = transcript,
                [FormPromptTemplateParameterNames.CommandLabel] = commandLabel
            });
    }

    public string? GetSilentInputFallbackLabel(
        FormContract contract,
        string isoCode,
        string silentInputFallback,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        var fallback = fallbackPromptResolver.GetSilentInputFallbackLabel(isoCode, silentInputFallback);
        if (fallback is null)
        {
            return null;
        }

        var promptId = silentInputFallbackLabelPromptIdResolver.Resolve(silentInputFallback);
        if (promptId is null)
        {
            return fallback;
        }

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            promptId,
            isoCode,
            politenessLevel,
            fallback);
    }
}
