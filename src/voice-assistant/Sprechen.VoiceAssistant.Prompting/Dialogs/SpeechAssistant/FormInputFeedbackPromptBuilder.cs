using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt lokalisierte Rückmeldungen für Formulareingabe und Korrektur bereit.
/// Den Wortlaut bestimmen die zuständigen Resolver aus Vertrags- oder Ersatzvorlagen.
/// </summary>
public static class FormInputFeedbackPromptBuilder
{
    private static readonly FormInputFeedbackFallbackPromptResolver FallbackPromptResolver = new();
    private static readonly FormInputFeedbackContractPromptResolver ContractPromptResolver = new();

    internal static string GetUnrecognizedInputPrompt(string isoCode)
        => FallbackPromptResolver.GetUnrecognizedInputPrompt(isoCode);

    internal static string GetInputModeChangedPrompt(string isoCode, InputMode inputMode)
        => FallbackPromptResolver.GetInputModeChangedPrompt(isoCode, inputMode);

    internal static string GetLiteralModeCommandBlockedPrompt(string isoCode)
        => FallbackPromptResolver.GetLiteralModeCommandBlockedPrompt(isoCode);

    internal static string GetVoiceInputForbiddenPrompt(string isoCode, string? silentInputFallback)
        => FallbackPromptResolver.GetVoiceInputForbiddenPrompt(isoCode, silentInputFallback);

    internal static string GetReadbackOptInPrompt(string isoCode, string fieldLabel, PolitenessLevel politenessLevel)
        => FallbackPromptResolver.GetReadbackOptInPrompt(isoCode, fieldLabel, politenessLevel);

    internal static string GetCorrectionRequestedPrompt(string isoCode)
        => FallbackPromptResolver.GetCorrectionRequestedPrompt(isoCode);

    internal static string GetSilentInputOfferPrompt(string isoCode, string? silentInputFallback)
        => FallbackPromptResolver.GetSilentInputOfferPrompt(isoCode, silentInputFallback);

    internal static string GetAmbiguousInputVsCommandPrompt(string isoCode, string transcript, string? commandLabel)
        => FallbackPromptResolver.GetAmbiguousInputVsCommandPrompt(isoCode, transcript, commandLabel);

    internal static string? GetSilentInputFallbackLabel(string isoCode, string silentInputFallback)
        => FallbackPromptResolver.GetSilentInputFallbackLabel(isoCode, silentInputFallback);

    public static string GetUnrecognizedInputPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
        => ContractPromptResolver.GetUnrecognizedInputPrompt(contract, isoCode, politenessLevel);

    public static string GetUnrecognizedInputWithTranscriptPrompt(
        FormContract contract,
        string isoCode,
        string transcript,
        PolitenessLevel politenessLevel)
        => ContractPromptResolver.GetUnrecognizedInputWithTranscriptPrompt(contract, isoCode, transcript, politenessLevel);

    public static string GetUnrecognizedInputSecondPrompt(
        FormContract contract,
        string isoCode,
        string transcript,
        PolitenessLevel politenessLevel)
        => ContractPromptResolver.GetUnrecognizedInputSecondPrompt(contract, isoCode, transcript, politenessLevel);

    public static string GetUnrecognizedInputThirdPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
        => ContractPromptResolver.GetUnrecognizedInputThirdPrompt(contract, isoCode, politenessLevel);

    public static string GetUnrecognizedInputThirdBeforeHelpPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
        => ContractPromptResolver.GetUnrecognizedInputThirdBeforeHelpPrompt(contract, isoCode, politenessLevel);

    public static string GetInputModeChangedPrompt(
        FormContract contract,
        string isoCode,
        InputMode inputMode,
        PolitenessLevel politenessLevel,
        bool teachFullSpellingTail = true)
        => ContractPromptResolver.GetInputModeChangedPrompt(contract, isoCode, inputMode, politenessLevel, teachFullSpellingTail);

    public static string GetLiteralModeCommandBlockedPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
        => ContractPromptResolver.GetLiteralModeCommandBlockedPrompt(contract, isoCode, politenessLevel);

    public static string GetVoiceInputForbiddenPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        string? silentInputFallback)
        => ContractPromptResolver.GetVoiceInputForbiddenPrompt(contract, isoCode, politenessLevel, silentInputFallback);

    public static string GetReadbackOptInPrompt(
        FormContract contract,
        string isoCode,
        string fieldLabel,
        PolitenessLevel politenessLevel)
        => ContractPromptResolver.GetReadbackOptInPrompt(contract, isoCode, fieldLabel, politenessLevel);

    public static string GetInputModeUnavailablePrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
        => ContractPromptResolver.GetInputModeUnavailablePrompt(contract, isoCode, politenessLevel);

    public static string GetSilentInputUnavailablePrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
        => ContractPromptResolver.GetSilentInputUnavailablePrompt(contract, isoCode, politenessLevel);

    public static string GetCommandParkWaitingPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
        => ContractPromptResolver.GetCommandParkWaitingPrompt(contract, isoCode, politenessLevel);

    public static string GetCorrectionRequestedPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
        => ContractPromptResolver.GetCorrectionRequestedPrompt(contract, isoCode, politenessLevel);

    public static string GetSilentInputOfferPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        string? silentInputFallback)
        => ContractPromptResolver.GetSilentInputOfferPrompt(contract, isoCode, politenessLevel, silentInputFallback);

    public static string GetAmbiguousInputVsCommandPrompt(
        FormContract contract,
        string isoCode,
        string transcript,
        string? commandLabel,
        PolitenessLevel politenessLevel)
        => ContractPromptResolver.GetAmbiguousInputVsCommandPrompt(contract, isoCode, transcript, commandLabel, politenessLevel);

    public static string? GetSilentInputFallbackLabel(
        FormContract contract,
        string isoCode,
        string silentInputFallback,
        PolitenessLevel politenessLevel)
        => ContractPromptResolver.GetSilentInputFallbackLabel(contract, isoCode, silentInputFallback, politenessLevel);
}
