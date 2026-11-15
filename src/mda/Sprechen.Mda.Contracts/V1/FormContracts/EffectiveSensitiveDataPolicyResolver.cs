namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Leitet Regeln für Spracheingabe und Rücklesen aus Datenschutzklasse und
/// modellierten Einstellungen ab. Ergänzt fehlende Werte und lehnt ungültige
/// Regeln sowie unzulässige Kombinationen für sensitive und secret ab.
/// </summary>
public sealed class EffectiveSensitiveDataPolicyResolver
{
    public bool TryResolve(
        PrivacyClass? privacyClass,
        SensitiveDataPolicy? authoredPolicy,
        out EffectiveSensitiveDataPolicy? policy,
        out EffectiveSensitiveDataPolicyFailure failure)
    {
        policy = null;

        if (privacyClass == PrivacyClass.Unknown)
        {
            failure = EffectiveSensitiveDataPolicyFailure.UnknownPrivacyClass;
            return false;
        }

        var privacyWire = privacyClass switch
        {
            PrivacyClass.Personal => PrivacyClasses.Personal,
            PrivacyClass.Sensitive => PrivacyClasses.Sensitive,
            PrivacyClass.Secret => PrivacyClasses.Secret,
            _ => PrivacyClasses.Public
        };

        if (!TryNormalizeMember(
                authoredPolicy?.VoiceInputPolicy,
                [
                    SensitiveDataPolicyDefaults.VoiceInputForbidden,
                    SensitiveDataPolicyDefaults.VoiceInputOptional,
                    SensitiveDataPolicyDefaults.VoiceInputRecommended
                ],
                DefaultVoiceInput(privacyWire),
                EffectiveSensitiveDataPolicyFailure.BlankVoiceInputPolicy,
                EffectiveSensitiveDataPolicyFailure.UnsupportedVoiceInputPolicy,
                out var voiceInput,
                out failure))
        {
            return false;
        }

        if (!TryNormalizeMember(
                authoredPolicy?.ReadbackPolicy,
                [
                    SensitiveDataPolicyDefaults.ReadbackNever,
                    SensitiveDataPolicyDefaults.ReadbackOptInRequired,
                    SensitiveDataPolicyDefaults.ReadbackAlways
                ],
                DefaultReadback(privacyWire),
                EffectiveSensitiveDataPolicyFailure.BlankReadbackPolicy,
                EffectiveSensitiveDataPolicyFailure.UnsupportedReadbackPolicy,
                out var readback,
                out failure))
        {
            return false;
        }

        if (!TryNormalizeMember(
                authoredPolicy?.SilentInputFallback,
                [
                    SensitiveDataPolicyDefaults.SilentInputFallbackKeyboard,
                    SensitiveDataPolicyDefaults.SilentInputFallbackBraille,
                    SensitiveDataPolicyDefaults.SilentInputFallbackAny
                ],
                SensitiveDataPolicyDefaults.SilentInputFallbackAny,
                EffectiveSensitiveDataPolicyFailure.BlankSilentInputFallback,
                EffectiveSensitiveDataPolicyFailure.UnsupportedSilentInputFallback,
                out var silentFallback,
                out failure))
        {
            return false;
        }

        if (privacyWire == PrivacyClasses.Secret
            && !string.Equals(voiceInput, SensitiveDataPolicyDefaults.VoiceInputForbidden, StringComparison.Ordinal))
        {
            failure = EffectiveSensitiveDataPolicyFailure.SecretVoiceInputNotForbidden;
            return false;
        }

        if (privacyWire == PrivacyClasses.Secret
            && !string.Equals(readback, SensitiveDataPolicyDefaults.ReadbackNever, StringComparison.Ordinal))
        {
            failure = EffectiveSensitiveDataPolicyFailure.SecretReadbackNotNever;
            return false;
        }

        if (privacyWire == PrivacyClasses.Sensitive
            && string.Equals(readback, SensitiveDataPolicyDefaults.ReadbackAlways, StringComparison.Ordinal))
        {
            failure = EffectiveSensitiveDataPolicyFailure.SensitiveReadbackAlways;
            return false;
        }

        policy = new EffectiveSensitiveDataPolicy(privacyWire, voiceInput, readback, silentFallback);
        failure = EffectiveSensitiveDataPolicyFailure.None;
        return true;
    }

    private static string DefaultVoiceInput(string privacyWire)
        => privacyWire switch
        {
            PrivacyClasses.Secret => SensitiveDataPolicyDefaults.VoiceInputForbidden,
            PrivacyClasses.Sensitive => SensitiveDataPolicyDefaults.VoiceInputOptional,
            _ => SensitiveDataPolicyDefaults.VoiceInputRecommended
        };

    private static string DefaultReadback(string privacyWire)
        => privacyWire switch
        {
            PrivacyClasses.Secret => SensitiveDataPolicyDefaults.ReadbackNever,
            PrivacyClasses.Sensitive => SensitiveDataPolicyDefaults.ReadbackOptInRequired,
            _ => SensitiveDataPolicyDefaults.ReadbackAlways
        };

    private static bool TryNormalizeMember(
        string? authored,
        string[] allowed,
        string absentDefault,
        EffectiveSensitiveDataPolicyFailure blankFailure,
        EffectiveSensitiveDataPolicyFailure unsupportedFailure,
        out string value,
        out EffectiveSensitiveDataPolicyFailure failure)
    {
        if (authored is null)
        {
            value = absentDefault;
            failure = EffectiveSensitiveDataPolicyFailure.None;
            return true;
        }

        if (string.IsNullOrWhiteSpace(authored))
        {
            value = string.Empty;
            failure = blankFailure;
            return false;
        }

        var trimmed = authored.Trim();
        if (!allowed.Contains(trimmed, StringComparer.Ordinal))
        {
            value = string.Empty;
            failure = unsupportedFailure;
            return false;
        }

        value = trimmed;
        failure = EffectiveSensitiveDataPolicyFailure.None;
        return true;
    }
}
