using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.PromptTemplates;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Liefert die eingebauten Standardansagen für Rückmeldungen zur Formulareingabe,
/// wenn der Vertrag keine eigene Vorlage bereitstellt.
/// </summary>
internal sealed class FormInputFeedbackFallbackPromptResolver
{
    private readonly FormInputModeChangedPromptIdResolver inputModePromptIdResolver;
    private readonly FormSilentInputFallbackLabelPromptIdResolver silentInputFallbackLabelPromptIdResolver;

    public FormInputFeedbackFallbackPromptResolver()
        : this(new FormInputModeChangedPromptIdResolver(), new FormSilentInputFallbackLabelPromptIdResolver())
    {
    }

    public FormInputFeedbackFallbackPromptResolver(
        FormInputModeChangedPromptIdResolver inputModePromptIdResolver,
        FormSilentInputFallbackLabelPromptIdResolver silentInputFallbackLabelPromptIdResolver)
    {
        this.inputModePromptIdResolver = inputModePromptIdResolver;
        this.silentInputFallbackLabelPromptIdResolver = silentInputFallbackLabelPromptIdResolver;
    }

    public string GetUnrecognizedInputPrompt(string isoCode)
        => FormPromptResolution.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.UnrecognizedInput);

    public string GetUnrecognizedInputWithTranscriptPrompt(string isoCode, string transcript)
        => FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIds.UnrecognizedInputWithTranscript,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.Transcript] = transcript
            });

    public string GetUnrecognizedInputSecondPrompt(string isoCode, string transcript)
        => FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIds.UnrecognizedInputSecond,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.Transcript] = transcript
            });

    public string GetUnrecognizedInputThirdPrompt(string isoCode)
        => FormPromptResolution.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.UnrecognizedInputThird);

    public string GetUnrecognizedInputThirdBeforeHelpPrompt(string isoCode)
        => FormPromptResolution.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.UnrecognizedInputThirdBeforeHelp);

    public string GetInputModeChangedPrompt(string isoCode, InputMode inputMode, bool teachFullSpellingTail = true)
        => FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            inputModePromptIdResolver.Resolve(inputMode, teachFullSpellingTail));

    public string GetLiteralModeCommandBlockedPrompt(string isoCode)
        => FormPromptResolution.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.LiteralModeCommandBlocked);

    public string GetSilentInputUnavailablePrompt(string isoCode)
        => FormPromptResolution.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.SilentInputUnavailable);

    public string GetVoiceInputForbiddenPrompt(string isoCode, string? silentInputFallback)
    {
        if (string.IsNullOrWhiteSpace(silentInputFallback))
        {
            return FormPromptResolution.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.VoiceInputForbiddenDefault);
        }

        return FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIds.VoiceInputForbiddenWithFallback,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.SilentInputFallback] = silentInputFallback
            });
    }

    public string GetReadbackOptInPrompt(string isoCode, string fieldLabel, PolitenessLevel politenessLevel)
        => FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIdVariantResolver.ResolvePolitenessVariantPromptId(
                FormDialogPromptIds.ReadbackOptIn,
                politenessLevel),
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.FieldLabel] = fieldLabel
            });

    public string GetInputModeUnavailablePrompt(string isoCode, PolitenessLevel politenessLevel)
        => FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIdVariantResolver.ResolvePolitenessVariantPromptId(
                FormDialogPromptIds.InputModeUnavailable,
                politenessLevel));

    public string GetCommandParkWaitingPrompt(string isoCode, PolitenessLevel politenessLevel)
        => FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIdVariantResolver.ResolvePolitenessVariantPromptId(
                FormDialogPromptIds.CommandParkWaiting,
                politenessLevel));

    public string GetCorrectionRequestedPrompt(string isoCode)
        => FormPromptResolution.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.CorrectionRequested);

    public string GetSilentInputOfferPrompt(string isoCode, string? silentInputFallback)
    {
        if (string.IsNullOrWhiteSpace(silentInputFallback))
        {
            return FormPromptResolution.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.SilentInputOfferDefault);
        }

        return FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIds.SilentInputOfferWithFallback,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.SilentInputFallback] = silentInputFallback
            });
    }

    public string GetAmbiguousInputVsCommandPrompt(string isoCode, string transcript, string? commandLabel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);

        var normalizedTranscript = transcript.Trim();
        var normalizedCommandLabel = string.IsNullOrWhiteSpace(commandLabel)
            ? null
            : commandLabel.Trim();

        if (normalizedCommandLabel is null)
        {
            return FormPromptResolution.ResolveFallbackPrompt(
                isoCode,
                FormDialogPromptIds.AmbiguousInputVsCommandWithoutLabel,
                new Dictionary<string, string?>
                {
                    [FormPromptTemplateParameterNames.Transcript] = normalizedTranscript
                });
        }

        return FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIds.AmbiguousInputVsCommandWithLabel,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.Transcript] = normalizedTranscript,
                [FormPromptTemplateParameterNames.CommandLabel] = normalizedCommandLabel
            });
    }

    public string? GetSilentInputFallbackLabel(string isoCode, string silentInputFallback)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(silentInputFallback);

        var promptId = silentInputFallbackLabelPromptIdResolver.Resolve(silentInputFallback);
        return promptId is null
            ? null
            : FormPromptResolution.ResolveFallbackPrompt(isoCode, promptId);
    }
}
