using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft die Auflösbarkeit der Datenschutzregeln und überführt Fehler
/// in Vorprüfungsmeldungen mit dem betroffenen Eigenschaftspfad.
/// </summary>
internal static class EffectiveSensitiveDataPolicyPreflightMapper
{
    public static bool TryAddResolvedOrIssue(
        EffectiveSensitiveDataPolicyResolver resolver,
        PrivacyClass? privacyClass,
        SensitiveDataPolicy? authoredPolicy,
        List<PreflightIssue> errors,
        string issueCode,
        string id,
        string privacyClassPath,
        string voiceInputPath,
        string readbackPath,
        string silentFallbackPath,
        string subject)
    {
        if (resolver.TryResolve(privacyClass, authoredPolicy, out _, out var failure))
        {
            return true;
        }

        AddFailure(
            errors,
            failure,
            issueCode,
            id,
            privacyClassPath,
            voiceInputPath,
            readbackPath,
            silentFallbackPath,
            subject,
            authoredPolicy);
        return false;
    }

    public static void AddFailure(
        List<PreflightIssue> errors,
        EffectiveSensitiveDataPolicyFailure failure,
        string issueCode,
        string id,
        string privacyClassPath,
        string voiceInputPath,
        string readbackPath,
        string silentFallbackPath,
        string subject,
        SensitiveDataPolicy? authoredPolicy = null)
    {
        var message = Describe(failure, subject, authoredPolicy);
        var resolvedPath = failure switch
        {
            EffectiveSensitiveDataPolicyFailure.UnknownPrivacyClass => privacyClassPath,
            EffectiveSensitiveDataPolicyFailure.BlankVoiceInputPolicy
                or EffectiveSensitiveDataPolicyFailure.UnsupportedVoiceInputPolicy
                or EffectiveSensitiveDataPolicyFailure.SecretVoiceInputNotForbidden => voiceInputPath,
            EffectiveSensitiveDataPolicyFailure.BlankReadbackPolicy
                or EffectiveSensitiveDataPolicyFailure.UnsupportedReadbackPolicy
                or EffectiveSensitiveDataPolicyFailure.SecretReadbackNotNever
                or EffectiveSensitiveDataPolicyFailure.SensitiveReadbackAlways => readbackPath,
            _ => silentFallbackPath
        };

        errors.Add(new PreflightIssue(
            Code: issueCode,
            Message: message,
            Path: resolvedPath,
            Id: id));
    }

    private static string Describe(
        EffectiveSensitiveDataPolicyFailure failure,
        string subject,
        SensitiveDataPolicy? authoredPolicy)
        => failure switch
        {
            EffectiveSensitiveDataPolicyFailure.UnknownPrivacyClass =>
                $"{subject} has an unrecognized privacy class.",
            EffectiveSensitiveDataPolicyFailure.BlankVoiceInputPolicy =>
                $"{subject} has a blank voiceInputPolicy.",
            EffectiveSensitiveDataPolicyFailure.UnsupportedVoiceInputPolicy =>
                $"{subject} has an unsupported voiceInputPolicy.",
            EffectiveSensitiveDataPolicyFailure.BlankReadbackPolicy =>
                $"{subject} has a blank readbackPolicy.",
            EffectiveSensitiveDataPolicyFailure.UnsupportedReadbackPolicy =>
                $"{subject} has an unsupported readbackPolicy.",
            EffectiveSensitiveDataPolicyFailure.BlankSilentInputFallback =>
                $"{subject} has a blank silentInputFallback.",
            EffectiveSensitiveDataPolicyFailure.UnsupportedSilentInputFallback =>
                $"{subject} has an unsupported silentInputFallback.",
            EffectiveSensitiveDataPolicyFailure.SecretVoiceInputNotForbidden =>
                $"{subject} is privacyClass=secret but voiceInputPolicy='{authoredPolicy?.VoiceInputPolicy}' is not allowed.",
            EffectiveSensitiveDataPolicyFailure.SecretReadbackNotNever =>
                $"{subject} is privacyClass=secret but readbackPolicy='{authoredPolicy?.ReadbackPolicy}' is not allowed.",
            EffectiveSensitiveDataPolicyFailure.SensitiveReadbackAlways =>
                $"{subject} is privacyClass=sensitive but readbackPolicy='always' violates opt-in requirement.",
            _ => $"{subject} has an invalid privacy policy."
        };
}
